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
        public List<Mesh> Meshes { get; set; } = [];
        public unsafe Mesh** MeshPointers { get; set; }
        public List<Material3Cache> Materials { get; set; }
        public List<Model3AnimationCache> Animations { get; set; } = [];
        public Bone3Cache[] AllBones { get; set; } = [];
        public Bone3Cache[] RootBones { get; set; } = [];
        
        public List<(Mesh mesh, int material)> MeshMaterialPairs { get; private set; } = [];
        public Dictionary<int, int> JointIDToNewID { get; internal set; }
        public Dictionary<int, int> NodeIDToJointID { get; internal set; }
    }
}
