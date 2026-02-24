using System;
using System.Collections.Specialized;

namespace AssetStudio
{
    public class Object
    {
        public SerializedFile assetsFile;
        public ObjectReader reader;
        public long m_PathID;
        public int[] version;
        protected BuildType buildType;
        public BuildTarget platform;
        public ClassIDType type;
        public SerializedType serializedType;
        public uint byteSize;

        public Object(ObjectReader reader)
        {
            this.reader = reader;
            reader.Reset();
            assetsFile = reader.assetsFile;
            type = reader.type;
            m_PathID = reader.m_PathID;
            version = reader.version;
            buildType = reader.buildType;
            platform = reader.platform;
            serializedType = reader.serializedType;
            byteSize = reader.byteSize;

            if (platform == BuildTarget.NoTarget)
            {
                var m_ObjectHideFlags = reader.ReadUInt32();
            }
        }

        public string Dump()
        {
            if (serializedType?.m_Type != null)
            {
                return TypeTreeHelper.ReadTypeString(serializedType.m_Type, reader);
            }
            return null;
        }

        public string Dump(TypeTree m_Type)
        {
            if (m_Type != null)
            {
                return TypeTreeHelper.ReadTypeString(m_Type, reader);
            }
            return null;
        }

        public OrderedDictionary ToType()
        {
            if (serializedType?.m_Type != null)
            {
                return TypeTreeHelper.ReadType(serializedType.m_Type, reader);
            }
            return null;
        }

        public OrderedDictionary ToType(TypeTree m_Type)
        {
            if (m_Type != null)
            {
                return TypeTreeHelper.ReadType(m_Type, reader);
            }
            return null;
        }

        public byte[] GetRawData()
        {
            reader.Reset();
            return reader.ReadBytes((int)byteSize);
        }

		internal static Object Read(ObjectReader objectReader) {
			switch (objectReader.type) {
				case ClassIDType.Animation:
					return new Animation(objectReader);
					
				case ClassIDType.AnimationClip:
					return new AnimationClip(objectReader);
					
				case ClassIDType.Animator:
					return new Animator(objectReader);
					
				case ClassIDType.AnimatorController:
					return new AnimatorController(objectReader);
					
				case ClassIDType.AnimatorOverrideController:
					return new AnimatorOverrideController(objectReader);
					
				case ClassIDType.AssetBundle:
					return new AssetBundle(objectReader);
					
				case ClassIDType.AudioClip:
					return new AudioClip(objectReader);
					
				case ClassIDType.Avatar:
					return new Avatar(objectReader);
					
				case ClassIDType.Font:
					return new Font(objectReader);
					
				case ClassIDType.GameObject:
					return new GameObject(objectReader);
					
				case ClassIDType.Material:
					return new Material(objectReader);
					
				case ClassIDType.Mesh:
					return new Mesh(objectReader);
					
				case ClassIDType.MeshFilter:
					return new MeshFilter(objectReader);
					
				case ClassIDType.MeshRenderer:
					return new MeshRenderer(objectReader);
					
				case ClassIDType.MonoBehaviour:
					return new MonoBehaviour(objectReader);
					
				case ClassIDType.MonoScript:
					return new MonoScript(objectReader);
					
				case ClassIDType.MovieTexture:
					return new MovieTexture(objectReader);
					
				case ClassIDType.ParticleSystem:
					return new ParticleSystem(objectReader);

				case ClassIDType.ParticleSystemRenderer:
					return new ParticleSystemRenderer(objectReader);

				case ClassIDType.PlayerSettings:
					return new PlayerSettings(objectReader);
					
				case ClassIDType.RectTransform:
					return new RectTransform(objectReader);
					
				case ClassIDType.Shader:
					return new Shader(objectReader);
					
				case ClassIDType.SkinnedMeshRenderer:
					return new SkinnedMeshRenderer(objectReader);

				case ClassIDType.Sprite:
					return new Sprite(objectReader);

				case ClassIDType.SpriteRenderer:
					return new SpriteRenderer(objectReader);

				case ClassIDType.SpriteAtlas:
					return new SpriteAtlas(objectReader);
					
				case ClassIDType.TextAsset:
					return new TextAsset(objectReader);
					
				case ClassIDType.Texture2D:
					return new Texture2D(objectReader);
					
				case ClassIDType.Transform:
					return new Transform(objectReader);
					
				case ClassIDType.VideoClip:
					return new VideoClip(objectReader);
					
				case ClassIDType.ResourceManager:
					return new ResourceManager(objectReader);
					
				default:
					return new Object(objectReader);
			}
		}
	}
}
