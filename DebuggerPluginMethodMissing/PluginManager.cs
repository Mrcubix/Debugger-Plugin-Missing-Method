using System.Collections.Concurrent;
using System.Reflection;
using DebuggerPluginMethodMissing.Plugin;

namespace DebuggerPluginMethodMissing
{
    public class PluginManager
    {
        protected ConcurrentBag<TypeInfo> pluginTypes;
        protected readonly Type[] libTypes;

        public PluginManager()
        {
            var assemblies = new[]
            {
                Assembly.Load("DebuggerPluginMethodMissing.Plugin")
            };

            libTypes = (from type in typeof(ITool).Assembly.GetExportedTypes()
                        where type.IsAbstract || type.IsInterface
                        select type).ToArray();

            var internalTypes = from asm in assemblies
                                from type in asm.DefinedTypes
                                where type.IsPublic && !(type.IsInterface || type.IsAbstract)
                                where IsPluginType(type)
                                select type;

            pluginTypes = new ConcurrentBag<TypeInfo>(internalTypes);
        }

        public IReadOnlyCollection<TypeInfo> PluginTypes => pluginTypes;
        protected List<DesktopPluginContext> Plugins { get; } = new List<DesktopPluginContext>();
        public DirectoryInfo PluginDirectory { get; set; } = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "Plugins"));

        public void Load()
        {
            foreach (var dir in PluginDirectory.GetDirectories())
                LoadPlugin(dir);
        }

        protected void LoadPlugin(DirectoryInfo directory)
        {
            // "Plugins" are directories that contain managed and unmanaged dll
            // These dlls are loaded into a PluginContext per directory
            directory.Refresh();

            if (Plugins.Any(p => p.Directory.Name == directory.Name))
            {
                Console.WriteLine($"Attempted to load the plugin {directory.Name} when it is already loaded.");
                return;
            }

            Console.WriteLine($"Loading plugin '{directory.Name}'");

            try
            {
                var context = new DesktopPluginContext(directory);

                // Populate PluginTypes so desktop implementations can access them
                ImportTypes(context);
                Plugins.Add(context);
            }
            catch
            {
                Console.WriteLine($"Failed to completely load plugin '{directory.Name}'. Some problems may occur later. Please double check if this plugin is installed correctly or has any update.");
            }
        }

        public virtual T ConstructObject<T>(string name, object[] args = null) where T : class
        {
            args ??= new object[0];

            if (!string.IsNullOrWhiteSpace(name))
            {
                try
                {
                    if (PluginTypes.FirstOrDefault(t => t.FullName == name) is TypeInfo type)
                    {
                        var matchingConstructors = from ctor in type.GetConstructors()
                                                   let parameters = ctor.GetParameters()
                                                   where parameters.Length == args.Length
                                                   where IsValidParameterFor(args, parameters)
                                                   select ctor;

                        if (matchingConstructors.FirstOrDefault() is ConstructorInfo constructor)
                            return (T)constructor.Invoke(args) ?? null;
                        else
                            Console.WriteLine($"No constructor found for '{name}'");
                    }
                }
                catch (TargetInvocationException e) when (e.Message == "Exception has been thrown by the target of an invocation.")
                {
                    Console.WriteLine("Object construction has thrown an error");
                    Console.WriteLine(e.InnerException);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to construct object '{name}'");
                    Console.WriteLine(e);
                }
            }

            return null;
        }

        protected void ImportTypes(PluginContext context)
        {
            var types = from asm in context.Assemblies
                        where IsLoadable(asm)
                        from type in asm.GetExportedTypes()
                        where IsPluginType(type)
                        select type;

            types.AsParallel().ForAll(type =>
            {
                try
                {
                    var pluginTypeInfo = type.GetTypeInfo();
                    if (!pluginTypes.Contains(pluginTypeInfo))
                        pluginTypes.Add(pluginTypeInfo);
                }
                catch
                {
                    Console.WriteLine($"Plugin '{type.FullName}' incompatible");
                }
            });
        }

        public virtual IReadOnlyCollection<TypeInfo> GetChildTypes<T>()
        {
            var children = from type in PluginTypes
                           where typeof(T).IsAssignableFrom(type)
                           select type;

            return children.ToArray();
        }

        protected virtual bool IsValidParameterFor(object[] args, ParameterInfo[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var arg = args[i];
                if (!parameter.ParameterType.IsAssignableFrom(arg.GetType()))
                    return false;
            }
            return true;
        }

        protected virtual bool IsPluginType(Type type)
        {
            return !type.IsAbstract && !type.IsInterface &&
                libTypes.Any(t => t.IsAssignableFrom(type) ||
                    type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == t));
        }

        protected virtual bool IsLoadable(Assembly asm)
        {
            try
            {
                _ = asm.DefinedTypes;
                return true;
            }
            catch (Exception ex)
            {
                var asmName = asm.GetName();
                var hResultHex = ex.HResult.ToString("X");
                Console.WriteLine($"Plugin '{asmName.Name}, Version={asmName.Version}' can't be loaded and is likely out of date. (HResult: 0x{hResultHex})");
                return false;
            }
        }
    }
}
