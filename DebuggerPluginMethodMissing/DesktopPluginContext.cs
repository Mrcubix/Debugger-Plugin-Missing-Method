using System.Reflection;

namespace DebuggerPluginMethodMissing
{
    public class DesktopPluginContext : PluginContext
    {
        public DesktopPluginContext(DirectoryInfo directory)
        {
            Directory = directory;
            FriendlyName = Directory.Name;

            foreach (var plugin in Directory.EnumerateFiles("*.dll"))
            {
                // Ignore a plugin library build artifact
                // Loading it seems to stop loading any further DLLs from the directory
                if (string.Equals(plugin.Name, "DebuggerPluginMethodMissing.Plugin.dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                LoadAssemblyFromFile(plugin);
            }
        }

        public DirectoryInfo Directory { get; }

        public string FriendlyName { get; }

        protected Assembly LoadAssemblyFromFile(FileInfo file)
        {
            try
            {
                return LoadFromAssemblyPath(file.FullName);
            }
            catch
            {
                Console.WriteLine($"Failed loading assembly '{file.Name}'");
                return null;
            }
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            if (Directory == null)
            {
                Console.WriteLine($"Independent plugin does not support loading native library '{unmanagedDllName}'");
                throw new NotSupportedException();
            }

            var runtimeFolder = new DirectoryInfo(Path.Join(Directory.FullName, "runtimes"));
            if (runtimeFolder.Exists)
            {
                var libraryFile = runtimeFolder.EnumerateFiles(ToDllName(unmanagedDllName), SearchOption.AllDirectories).FirstOrDefault();
                if (libraryFile != null)
                    return LoadUnmanagedDllFromPath(libraryFile.FullName);
            }
            return IntPtr.Zero;
        }

        private static string ToDllName(string dllName)
        {
            if (OperatingSystem.IsWindows())
                return $"{dllName}.dll";

            if (OperatingSystem.IsLinux())
                return $"lib{dllName}.so";

            if (OperatingSystem.IsMacOS())
                return $"lib{dllName}.dylib";

            return null;
        }
    }
}
