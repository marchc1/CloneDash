using System;
using System.Collections.Specialized;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AssetStudio
{
    public class Object
    {
        [JsonIgnore]
        public SerializedFile assetsFile;
        [JsonIgnore]
        public ObjectReader reader;
        public long m_PathID;
        [JsonIgnore]
        public UnityVersion version;
        [JsonIgnore]
        public BuildTarget platform;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ClassIDType type;
        [JsonIgnore]
        public SerializedType serializedType;
        public int classID;
        public uint byteSize;
        [JsonIgnore]
        public string Name;
        private static readonly JsonSerializerOptions jsonOptions;

        static Object()
        {
            jsonOptions = new JsonSerializerOptions
            {
                Converters = { new JsonConverterHelper.FloatConverter() },
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                WriteIndented = true,
            };
        }

        public Object() { }

        public Object(ObjectReader reader)
        {
            this.reader = reader;
            reader.Reset();
            assetsFile = reader.assetsFile;
            type = reader.type;
            m_PathID = reader.m_PathID;
            version = reader.version;
            platform = reader.platform;
            serializedType = reader.serializedType;
            classID = reader.classID;
            byteSize = reader.byteSize;

            if (platform == BuildTarget.NoTarget)
            {
                var m_ObjectHideFlags = reader.ReadUInt32();
            }
        }

        public string DumpObject()
        {
            string str = null;
            try
            {
                if (this is Mesh m_Mesh)
                {
                    m_Mesh.ProcessData();
                }

                str = JsonSerializer.Deserialize<JsonObject>(JsonSerializer.SerializeToUtf8Bytes(this, GetType(), jsonOptions))
                    .ToJsonString(jsonOptions).Replace("  ", "    ");
            }
            catch
            {
                //ignore
            }

            return str;
        }

        public string Dump(TypeTree m_Type = null)
        {
            m_Type = m_Type ?? serializedType?.m_Type;
            if (m_Type == null)
                return null;

            return TypeTreeHelper.ReadTypeString(m_Type, reader);
        }

        public OrderedDictionary ToType(TypeTree m_Type = null)
        {
            m_Type = m_Type ?? serializedType?.m_Type;
            if (m_Type == null)
                return null;

            return TypeTreeHelper.ReadType(m_Type, reader);
        }

        public JsonDocument ToJsonDoc(TypeTree m_Type = null)
        {
            var typeDict = ToType(m_Type);
            try
            {
                if (typeDict != null)
                {
                    return JsonSerializer.SerializeToDocument(typeDict, jsonOptions);
                }

                if (this is Mesh m_Mesh)
                {
                    m_Mesh.ProcessData();
                }

                return JsonSerializer.SerializeToDocument(this, GetType(), jsonOptions);
            }
            catch
            {
                //ignore
            }

            return null;
        }

        public byte[] GetRawData()
        {
            reader.Reset();
            return reader.ReadBytes((int)byteSize);
        }

		internal static Object Read(ObjectReader objectReader, bool loadViaTypeTree = false, ObjectInfo objectInfo = null) {
			Object obj = null;
			switch (objectReader.type) {
				case ClassIDType.Animation:
					obj = new Animation(objectReader);
					break;
				case ClassIDType.AnimationClip:
					obj = objectReader.serializedType?.m_Type != null && loadViaTypeTree
						? new AnimationClip(objectReader, TypeTreeHelper.ReadTypeByteArray(objectReader.serializedType.m_Type, objectReader), jsonOptions, objectInfo)
						: new AnimationClip(objectReader);
					break;
				case ClassIDType.Animator:
					obj = new Animator(objectReader);
					break;
				case ClassIDType.AnimatorController:
					obj = new AnimatorController(objectReader);
					break;
				case ClassIDType.AnimatorOverrideController:
					obj = new AnimatorOverrideController(objectReader);
					break;
				case ClassIDType.AssetBundle:
					obj = new AssetBundle(objectReader);
					break;
				case ClassIDType.AudioClip:
					obj = new AudioClip(objectReader);
					break;
				case ClassIDType.Avatar:
					obj = new Avatar(objectReader);
					break;
				case ClassIDType.BuildSettings:
					obj = new BuildSettings(objectReader);
					break;
				case ClassIDType.Font:
					obj = new Font(objectReader);
					break;
				case ClassIDType.GameObject:
					obj = new GameObject(objectReader);
					break;
				case ClassIDType.Material:
					obj = objectReader.serializedType?.m_Type != null && loadViaTypeTree
						? new Material(objectReader, TypeTreeHelper.ReadTypeByteArray(objectReader.serializedType.m_Type, objectReader), jsonOptions)
						: new Material(objectReader);
					break;
				case ClassIDType.Mesh:
					obj = new Mesh(objectReader);
					break;
				case ClassIDType.MeshFilter:
					obj = new MeshFilter(objectReader);
					break;
				case ClassIDType.MeshRenderer:
					obj = new MeshRenderer(objectReader);
					break;
				case ClassIDType.MonoBehaviour:
					obj = new MonoBehaviour(objectReader);
					break;
				case ClassIDType.MonoScript:
					obj = new MonoScript(objectReader);
					break;
				case ClassIDType.MovieTexture:
					obj = new MovieTexture(objectReader);
					break;
				case ClassIDType.ParticleSystem:
					obj = new ParticleSystem(objectReader);
					break;
				case ClassIDType.ParticleSystemRenderer:
					obj = new ParticleSystemRenderer(objectReader);
					break;
				case ClassIDType.PlayerSettings:
					obj = new PlayerSettings(objectReader);
					break;
				case ClassIDType.PreloadData:
					obj = new PreloadData(objectReader);
					break;
				case ClassIDType.RectTransform:
					obj = new RectTransform(objectReader);
					break;
				case ClassIDType.Shader:
					if (objectReader.version < 2021)
						obj = new Shader(objectReader);
					break;
				case ClassIDType.SkinnedMeshRenderer:
					obj = new SkinnedMeshRenderer(objectReader);
					break;
				case ClassIDType.Sprite:
					obj = new Sprite(objectReader);
					break;
				case ClassIDType.SpriteRenderer:
					obj = new SpriteRenderer(objectReader);
					break;
				case ClassIDType.SpriteAtlas:
					obj = new SpriteAtlas(objectReader);
					break;
				case ClassIDType.TextAsset:
					obj = new TextAsset(objectReader);
					break;
				case ClassIDType.Texture2D:
					obj = objectReader.serializedType?.m_Type != null && loadViaTypeTree
						? new Texture2D(objectReader, TypeTreeHelper.ReadTypeByteArray(objectReader.serializedType.m_Type, objectReader), jsonOptions)
						: new Texture2D(objectReader);
					break;
				case ClassIDType.Texture2DArray:
					obj = objectReader.serializedType?.m_Type != null && loadViaTypeTree
						? new Texture2DArray(objectReader, TypeTreeHelper.ReadTypeByteArray(objectReader.serializedType.m_Type, objectReader), jsonOptions)
						: new Texture2DArray(objectReader);
					break;
				case ClassIDType.Transform:
					obj = new Transform(objectReader);
					break;
				case ClassIDType.VideoClip:
					obj = new VideoClip(objectReader);
					break;
				case ClassIDType.ResourceManager:
					obj = new ResourceManager(objectReader);
					break;
				default:
					obj = new Object(objectReader);
					break;
			}

			return obj;
		}
	}
}
