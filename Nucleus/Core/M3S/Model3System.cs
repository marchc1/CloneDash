using System.Numerics;
using Raylib_cs;
using glTFLoader;
using Image = Raylib_cs.Image;
using static Nucleus.Core.GLTFHelpers;
using Nucleus.Types;
using Nucleus.Util;

namespace Nucleus.Core
{
	/// <summary>
	/// Attempt #3 of the Nucleus model system.
	/// <br></br>
	/// <br></br>
	/// Supports:
	/// <br></br>    - GLB file importing with its embedded textures
	/// <br></br>    - Cached and semi-instanced models (model data is reused per entity)
	/// <br></br>    - Hierarchial skeleton support
	/// <br></br>    - GPU-powered mesh skinning
	/// <br></br>    - Animation importing &amp; animation channels (WIP)
	/// <br></br>    - Dynamically-poseable bones
	/// <br></br>
	/// <br></br>This system is not meant to be interfaced with directly, use ModelEntity's in the engine unless absolutely necessary
	/// </summary>
	public static class Model3System
	{
		internal static Dictionary<string, Model3Cache> Cache { get; } = new();

		public static unsafe Model3 Load(string modelname, bool nocache = false) {
			var modelAbsPath = Filesystem.Resolve(modelname, "models");

			Model3 model = new Model3();
			if (!Cache.ContainsKey(modelAbsPath) || nocache) {
				Model3Cache cache = new();
				Cache[modelAbsPath] = cache;

				var schema = Interface.LoadModel(modelAbsPath);
				byte[] buffer = schema.LoadBinaryBuffer(0, modelAbsPath);

				List<Material3Cache> materialPairs = [];

				Logs.Debug($"GLTF/Import: #Meshes     = {schema.Meshes.Length}");
				Logs.Debug($"GLTF/Import: #Materials  = {schema.Materials.Length}");
				Logs.Debug($"GLTF/Import: #Images     = {schema.Images.Length}");
				Logs.Debug($"GLTF/Import: #Texture    = {schema.Textures.Length}");
				Logs.Debug($"GLTF/Import: #Skins      = {schema.Skins.Length}");

				var mi = 0;
				foreach (var material in schema.Materials) {
					var pbrMetallicRoughness = material.PbrMetallicRoughness;
					var texInfo = pbrMetallicRoughness.BaseColorTexture;
					var imageInfo = schema.Images[schema.Textures[texInfo.Index].Source ?? -1];

					var bufferView = schema.BufferViews[imageInfo.BufferView.Value];
					byte[] data = new byte[bufferView.ByteLength];
					Array.Copy(buffer, bufferView.ByteOffset, data, 0, bufferView.ByteLength);

					Image i = Raylib.LoadImageFromMemory(GLTFHelpers.MIMEToRaylib(imageInfo.MimeType ?? glTFLoader.Schema.Image.MimeTypeEnum.image_png), data);
					Texture2D t = Raylib.LoadTextureFromImage(i);

					Raylib.UnloadImage(i);

					Material m = new Material();
					m.Maps = Raylib.New<MaterialMap>(1); // C++ memory allocator needed here
					m.Maps[0] = new MaterialMap() {
						Color = Color.WHITE,
						Texture = t,
						Value = 1.0f
					};

					m.Shader = Raylib.LoadShader(Filesystem.Resolve("model3.vshader", "shaders"), Filesystem.Resolve("model3.fshader", "shaders"));

					materialPairs.Add(new() {
						Material = m
					});
				}

				var len = 0;
				foreach (var meshData in schema.Meshes)
					foreach (var primitiveData in meshData.Primitives)
						len++;


				var index = 0;
				foreach (var meshData in schema.Meshes) {
					foreach (var primitiveData in meshData.Primitives) {
						Mesh mesh = new Mesh();

						var indices = new AccessorReader(schema, buffer, primitiveData.Indices.Value).ReadUShortArray();
						var vertices = new List<float>();
						var normals = new List<float>();
						var texcoords = new List<float>();
						var joints = new List<float>();
						var weights = new List<float>();

						foreach (var attributeData in primitiveData.Attributes) {
							switch (attributeData.Key) {
								case "POSITION": vertices.AddRange(new AccessorReader(schema, buffer, attributeData.Value).ReadFloatArray()); break;
								case "NORMAL": normals.AddRange(new AccessorReader(schema, buffer, attributeData.Value).ReadFloatArray()); break;
								case "TEXCOORD_0": texcoords.AddRange(new AccessorReader(schema, buffer, attributeData.Value).ReadFloatArray()); break;
								case "JOINTS_0":
									var jointBytes = new AccessorReader(schema, buffer, attributeData.Value).ReadUByteArray();
									foreach (var us in jointBytes) {
										joints.Add(us);
									}
									break;
								case "WEIGHTS_0": weights.AddRange(new AccessorReader(schema, buffer, attributeData.Value).ReadFloatArray()); break;
							}
						}

						// Confirm if the mesh is valid or not
						// Only supports triangle meshes so its pretty easy to just work with multiple of 3's
						if (indices.Count % 3 != 0)
							throw new Exception("Indices.Count is not a multiple of 3; is the input corrupt?");

						int triangles = indices.Count / 3;
						int v = vertices.Count / 3;

						cache.Vertices += v;
						cache.Triangles += triangles;

						if (vertices.Count != normals.Count) throw new Exception("Mismatch between the amount of vertices and normals; is the model file corrupt?");
						if (v * 2 != texcoords.Count) throw new Exception("Mismatch between the amount of vertices and texture coordinates; is the model file corrupt?");
						if (v * 4 != joints.Count) throw new Exception("Mismatch between the amount of vertices and joint attachment IDs; is the model file corrupt?");
						if (v * 4 != weights.Count) throw new Exception("Mismatch between the amount of vertices and joint weights; is the model file corrupt?");

						// Convert the List's to arrays, then to pointers for the mesh data

						byte[] cfinal = new byte[vertices.Count * 4];
						for (int i = 0; i < cfinal.Length; i++) {
							cfinal[i] = 255;
						}


						unsafe {
							float* jPtr = Raylib.New<float>(joints.Count);
							for (int i = 0; i < joints.Count; i++) {
								jPtr[i] = joints[i];
							}

							mesh.Indices = indices.ToUnmanagedPointer();
							mesh.TriangleCount = triangles;
							mesh.VertexCount = v;
							mesh.Vertices = vertices.ToUnmanagedPointer();
							mesh.TexCoords = texcoords.ToUnmanagedPointer();
							mesh.Normals = normals.ToUnmanagedPointer();
							mesh.Colors = cfinal.ToUnmanagedPointer();
							mesh.BoneWeights = weights.ToUnmanagedPointer();

							// a lot of this stuff is copied from Raylib's UploadMesh function but remade in C#
							// I decided to make sure everything works to almost 100% copy how it does things
							// the only changes I really neded were changing the order of inputs uploaded to the GPU,
							// so i could do mesh skinning in a GPU shader which Raylib does not support

							var dynamic = false;

							// Set up VAO, VBO
							mesh.VaoId = 0;
							var VboId = Raylib.New<uint>(7);

							VboId[0] = 0;
							VboId[1] = 0;
							VboId[2] = 0;
							VboId[3] = 0;
							VboId[4] = 0;
							VboId[5] = 0;
							VboId[6] = 0;

							// Load the vertex array into the mesh
							mesh.VaoId = Rlgl.LoadVertexArray();
							Rlgl.EnableVertexArray(mesh.VaoId);

							// Vertex position coordinates
							void* verticesAdr = (mesh.AnimVertices != null) ? mesh.AnimVertices : mesh.Vertices;
							VboId[0] = Rlgl.LoadVertexBuffer(verticesAdr, mesh.VertexCount * 3 * sizeof(float), dynamic);
							Rlgl.SetVertexAttribute(0, 3, Rlgl.FLOAT, 0, 0, null); //null?
							Rlgl.EnableVertexAttribute(0);

							// Vertex UV coordinates
							VboId[1] = Rlgl.LoadVertexBuffer(mesh.TexCoords, mesh.VertexCount * 2 * sizeof(float), dynamic);
							Rlgl.SetVertexAttribute(1, 2, Rlgl.FLOAT, 0, 0, null);
							Rlgl.EnableVertexAttribute(1);

							// Vertex normals
							VboId[2] = Rlgl.LoadVertexBuffer(mesh.Normals, mesh.VertexCount * 3 * sizeof(float), dynamic);
							Rlgl.SetVertexAttribute(2, 3, Rlgl.FLOAT, 0, 0, null);
							Rlgl.EnableVertexAttribute(2);

							// Vertex colors; these are just plain white since the fragment shader does all of this
							VboId[3] = Rlgl.LoadVertexBuffer(mesh.Colors, mesh.VertexCount * 4 * sizeof(byte), dynamic);
							Rlgl.SetVertexAttribute(3, 4, Rlgl.UNSIGNED_BYTE, 1, 0, null);
							Rlgl.EnableVertexAttribute(3);

							// Joint indices
							VboId[4] = Rlgl.LoadVertexBuffer(joints.ToUnmanagedPointer(), joints.Count * sizeof(int), dynamic);
							Rlgl.EnableVertexAttribute(4);
							Rlgl.SetVertexAttribute(4, 4, Rlgl.FLOAT, false, 0, null);

							// Bone weights
							VboId[5] = Rlgl.LoadVertexBuffer(mesh.BoneWeights, mesh.VertexCount * 4 * sizeof(float), dynamic);
							Rlgl.SetVertexAttribute(5, 4, Rlgl.FLOAT, 0, 0, null);
							Rlgl.EnableVertexAttribute(5);

							// Indices for triangles
							VboId[6] = Rlgl.LoadVertexBufferElement(mesh.Indices, mesh.TriangleCount * 3 * sizeof(ushort), dynamic);

							mesh.VboId = VboId;

							if (mesh.VaoId > 0)
								Logs.Debug($"Mesh #[{mesh.VaoId}] uploaded to GPU.");

							Rlgl.DisableVertexArray();
						}
						cache.Meshes.Add(mesh);
						cache.MeshMaterialPairs.Add((mesh, primitiveData.Material ?? 0));

						index += 1;
					}
				}
				cache.Materials = materialPairs;

				// Figure out the armature structure
				// Throw errors if more than one skin; currently only supports single-armatures for models

				if (schema.Skins.Length > 1)
					throw new Exception("Multi-skeletal models arent supported");
				else if (schema.Skins.Length == 1) {
					var skin = schema.Skins[0];
					var ibmID = skin.InverseBindMatrices;

					if (ibmID.HasValue) {
						List<Matrix4x4> ibm = new AccessorReader(schema, buffer, ibmID ?? -1).ReadMatrix4FArray();

						Bone3Cache[] allJoints = new Bone3Cache[skin.Joints.Length];
						Bone3Cache[] rootJoints = new Bone3Cache[skin.Joints.Length];
						for (int i = 0; i < skin.Joints.Length; i++) rootJoints[i] = null;

						Dictionary<int, int> ParentAssociation = new();
						Dictionary<int, int> JointIDToNewID = new();
						Dictionary<int, int> NodeIDToJointID = new();
						var newIndexBasedID = 0;

						foreach (var jointID in skin.Joints) {
							var jointData = schema.Nodes[jointID];
							Bone3Cache joint = new();
							joint.ID = newIndexBasedID;
							NodeIDToJointID[jointID] = newIndexBasedID;
							JointIDToNewID[joint.ID] = newIndexBasedID;
							joint.Name = jointData.Name;
							joint.InverseBindMatrix = ibm[newIndexBasedID];

							if (jointData.Children != null)
								foreach (var childID in jointData.Children)
									ParentAssociation[childID] = newIndexBasedID;

							if (ParentAssociation.ContainsKey(jointID)) {
								joint.Parent = ParentAssociation[jointID];
								allJoints[joint.Parent].Children.Add(joint);
							}
							else {
								rootJoints[newIndexBasedID] = joint;
							}


							joint.BindPose = TransformVQV.FromTQS(jointData.Translation, jointData.Rotation, jointData.Scale);

							allJoints[newIndexBasedID] = joint;
							newIndexBasedID++;
						}

						cache.JointIDToNewID = JointIDToNewID;
						cache.NodeIDToJointID = NodeIDToJointID;
						cache.AllBones = allJoints;

						var rootBones = new List<Bone3Cache>();
						for (int i = 0; i < rootJoints.Length; i++) {
							if (rootJoints[i] != null)
								rootBones.Add(rootJoints[i]);
						}
						cache.RootBones = rootBones.ToArray();
					}
					else {
						throw new Exception("No inverse bind matrix?");
					}
				}

				if (schema.Animations.Length > 0) {
					foreach (var animation in schema.Animations) {
						Model3AnimationCache animCache = new();
						animCache.Name = animation.Name;
						Logs.Debug($"Loading animation '{animCache.Name}'...");
						foreach (var channel in animation.Channels) {
							IAnimationChannelData channelObj = null;

							// Setup path and the way IAnimationChannelData stores its value data
							switch (channel.Target.Path) {
								case glTFLoader.Schema.AnimationChannelTarget.PathEnum.translation:
									channelObj = new AnimationChannelData<Vector3>();
									channelObj.Path = AnimationTargetPath.Position;
									break;
								case glTFLoader.Schema.AnimationChannelTarget.PathEnum.rotation:
									channelObj = new AnimationChannelData<Quaternion>();
									channelObj.Path = AnimationTargetPath.Rotation;
									break;
								case glTFLoader.Schema.AnimationChannelTarget.PathEnum.scale:
									channelObj = new AnimationChannelData<Vector3>();
									channelObj.Path = AnimationTargetPath.Scale;
									break;
								case glTFLoader.Schema.AnimationChannelTarget.PathEnum.weights:
									Logs.Warn("The GLTF loader is currently unable to load weight animation data; some animations may not work as expected");
									break;
							}

							if (channelObj == null) // Weights arent supported and it would be null here 
								continue;

							channelObj.Target = cache.NodeIDToJointID[channel.Target.Node.Value];

							var sampler = animation.Samplers[channel.Sampler];
							switch (sampler.Interpolation) {
								case glTFLoader.Schema.AnimationSampler.InterpolationEnum.STEP:
									channelObj.Interpolation = AnimationInterpolation.Step; break;
								case glTFLoader.Schema.AnimationSampler.InterpolationEnum.LINEAR:
									channelObj.Interpolation = AnimationInterpolation.Linear; break;
								case glTFLoader.Schema.AnimationSampler.InterpolationEnum.CUBICSPLINE:
									Logs.Warn("Cubic-spline keyframe interpolation is currently not supported by the model runtime");
									channelObj.Interpolation = AnimationInterpolation.Bezier; break;
							}

							var inputs = new AccessorReader(schema, buffer, sampler.Input).ReadFloatArray();
							var outputs = new AccessorReader(schema, buffer, sampler.Output).ReadFloatArray();

							switch (channelObj) {
								case AnimationChannelData<Vector3> vec3:
									if (inputs.Count * 3 != outputs.Count)
										throw new Exception($"Mismatch: {inputs.Count} inputs, {outputs.Count} outputs, expected inputs * 3 == outputs");

									for (int i = 0; i < inputs.Count; i++) {
										if (inputs[i] > animCache.AnimationLength)
											animCache.AnimationLength = inputs[i];

										vec3.Keyframes.Add(new(inputs[i], new Vector3(outputs[i * 3], outputs[(i * 3) + 1], outputs[(i * 3) + 2])));
									}
									break;

								case AnimationChannelData<Quaternion> quat4:
									if (inputs.Count * 4 != outputs.Count)
										throw new Exception($"Mismatch: {inputs.Count} inputs, {outputs.Count} outputs, expected inputs * 4 == outputs");

									for (int i = 0; i < inputs.Count; i++) {
										if (inputs[i] > animCache.AnimationLength)
											animCache.AnimationLength = inputs[i];

										quat4.Keyframes.Add(new(inputs[i], new Quaternion(outputs[(i * 4) + 0], outputs[(i * 4) + 1], outputs[(i * 4) + 2], outputs[(i * 4) + 3])));
									}
									break;
							}

							animCache.Channels.Add(channelObj);
						}
						animCache.Build();
						cache.Animations.Add(animCache);
					}
				}
			}

			model.Model = Cache[modelAbsPath];

			foreach (var boneCacheStore in model.Model.RootBones) {
				model.CreateBoneFromCache(boneCacheStore);
			}

			model.Build();
			return model;
		}
	}
}
