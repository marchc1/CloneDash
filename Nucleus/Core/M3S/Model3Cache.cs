using Raylib_cs;

namespace Nucleus.Core
{
	/// <summary>
	/// Cached model data loaded from GLTF files. This stores all associated Meshes, Materials, Skeletons/Bones, and Animations.
	/// <br></br>
	/// The Model3System stores a dictionary cache (file -> cacheobj) of all loaded model cache objects, and builds Model3's out of them
	/// </summary>
	public class Model3Cache()
	{
		public int Vertices { get; internal set; }
		public int Triangles { get; internal set; }
		public List<Mesh> Meshes { get; set; } = [];
		public List<Material3Cache> Materials { get; set; }
		public List<Model3AnimationCache> Animations { get; set; } = [];
		public Bone3Cache[] AllBones { get; set; } = [];
		public Bone3Cache[] RootBones { get; set; } = [];

		public List<(Mesh mesh, int material)> MeshMaterialPairs { get; private set; } = [];
		public Dictionary<int, int> JointIDToNewID { get; internal set; }
		public Dictionary<int, int> NodeIDToJointID { get; internal set; }

		~Model3Cache() {
			unsafe {
				Logs.Debug("Attempting to unload a Model3Cache. Problems may arise.");
				foreach (var mesh in Meshes) {
					var VaoId = mesh.VaoId;
					var VboId = mesh.VboId;
					var Vertices = mesh.Vertices;
					var Normals = mesh.Normals;
					var Colors = mesh.Colors;
					var Tangents = mesh.Tangents;
					var TexCoords2 = mesh.TexCoords2;
					var Indices = mesh.Indices;
					var AnimVertices = mesh.AnimVertices;
					var AnimNormals = mesh.AnimNormals;
					var BoneWeights = mesh.BoneWeights;
					var BoneIds = mesh.BoneIds;
					MainThread.RunASAP(() => {
						unsafe {
							// UnloadMesh doesn't work here. My guess; we do everything from scratch and that results in
							// the RL_FREE calls failing somewhere. So I'm just going to manually free what I know Nucleus
							// does for Model3Cache's and call it a day.

							Rlgl.UnloadVertexArray(VaoId);
							if (VboId != null)
								for (uint vboID = 0; vboID < 7; vboID++) { // MAX_MESH_VERTEX_BUFFERS == 7 == the value used here
									Rlgl.UnloadVertexBuffer(VboId[vboID]);
								}
							Raylib.MemFree(VboId);
							Raylib.MemFree(Vertices);
							Raylib.MemFree(Normals);
							Raylib.MemFree(Colors);
							Raylib.MemFree(Tangents);
							Raylib.MemFree(TexCoords2);
							Raylib.MemFree(Indices);
							Raylib.MemFree(AnimVertices);
							Raylib.MemFree(AnimNormals);
							Raylib.MemFree(BoneWeights);
							Raylib.MemFree(BoneIds);
						}
					});
				}

				MainThread.RunASAP(() => {
					for (int i = 0; i < Materials.Count; i++) {
						var material = Materials[i];
						Raylib.UnloadTexture(material.Material.Maps[0].Texture);
					}
				});
			}
		}
	}
}
