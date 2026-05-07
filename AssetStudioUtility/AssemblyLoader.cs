using Mono.Cecil;
using System.Collections.Generic;
using System.IO;

namespace AssetStudio
{
    public class AssemblyLoader
    {
        public bool Loaded;
        private Dictionary<string, ModuleDefinition> moduleDic = new Dictionary<string, ModuleDefinition>();

        public void Load(string path)
        {
            var files = Directory.GetFiles(path, "*.dll");
            var resolver = new MyAssemblyResolver();
            var readerParameters = new ReaderParameters();
            readerParameters.AssemblyResolver = resolver;
            foreach (var file in files)
            {
                try
                {
                    var assembly = AssemblyDefinition.ReadAssembly(file, readerParameters);
                    resolver.Register(assembly);
                    moduleDic.Add(assembly.MainModule.Name, assembly.MainModule);
                }
                catch
                {
                    // ignored
                }
            }
            Loaded = true;
        }

        public TypeDefinition GetTypeDefinition(string assemblyName, string fullName)
        {
            if (!moduleDic.TryGetValue(assemblyName, out var module) && !assemblyName.EndsWith(".dll"))
            {
                assemblyName += ".dll";
                moduleDic.TryGetValue(assemblyName, out module);
            }
            if (module == null) 
                return null;

            var typeDef = module.GetType(fullName);
            if (typeDef == null && assemblyName == "UnityEngine.dll")
            {
                foreach (var pair in moduleDic)
                {
                    typeDef = pair.Value.GetType(fullName);
                    if (typeDef != null)
                    {
                        break;
                    }
                }
            }
            return typeDef;
        }

        public void Clear()
        {
            foreach (var pair in moduleDic)
            {
                pair.Value.Dispose();
            }
            moduleDic.Clear();
            Loaded = false;
        }
    }
}
