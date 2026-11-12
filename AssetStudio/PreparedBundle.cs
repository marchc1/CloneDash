using System.Collections.Generic;

namespace AssetStudio
{
    /// <summary>
    /// A bundle that was read and decompressed by <see cref="AssetsManager.PrepareBundleFile"/> but not added to its manager yet.
    /// </summary>
    public sealed class PreparedBundle
    {
        internal readonly List<SerializedFile> AssetsFiles = new List<SerializedFile>();
        internal readonly List<(string Name, FileReader Reader, bool Replace)> ResourceReaders = new List<(string Name, FileReader Reader, bool Replace)>();
    }
}
