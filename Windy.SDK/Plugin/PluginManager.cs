using System.Reflection;
using System.Runtime.Loader;
using Windy.SDK.Adaptor;
using Windy.SDK.Command;
using Windy.SDK.Hooks;

namespace Windy.SDK.Plugin
{
    public sealed class PluginManager
    {
        private readonly Dictionary<string, Assembly> loadedAssemblies = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> dependencyDirectories = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, byte[]> assemblyCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<WindyPlugin> plugins = new();
        private bool resolverRegistered;

        public IReadOnlyList<WindyPlugin> Plugins => plugins;

        public void Load(string pluginsDirectory, IReadOnlyList<Adaptor.Adaptor> adaptors, CommandRegistry commands, HookRegistry hooks)
        {
            Directory.CreateDirectory(pluginsDirectory);
            var fullPluginsDir = Path.GetFullPath(pluginsDirectory);
            dependencyDirectories.Add(fullPluginsDir);

            PreCacheAssemblies(fullPluginsDir);

            if (!resolverRegistered)
            {
                AssemblyLoadContext.Default.Resolving += ResolveAssembly;
                resolverRegistered = true;
            }

            LoadFromAssembly(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly(), adaptors, commands, hooks);

            foreach (string pluginPath in Directory.GetFiles(pluginsDirectory, "*.dll"))
            {
                var fullPluginPath = Path.GetFullPath(pluginPath);
                if (loadedAssemblies.ContainsKey(fullPluginPath))
                {
                    continue;
                }

                try
                {
                    if (assemblyCache.TryGetValue(fullPluginPath, out var raw))
                    {
                        using var ms = new MemoryStream(raw, false);
                        Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                        loadedAssemblies.Add(fullPluginPath, assembly);
                        LoadFromAssembly(assembly, adaptors, commands, hooks);
                    }
                }
                catch (BadImageFormatException)
                {
                }
            }
        }

        private void PreCacheAssemblies(string directory)
        {
            try
            {
                foreach (string dllPath in Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    var fullPath = Path.GetFullPath(dllPath);
                    if (!assemblyCache.ContainsKey(fullPath))
                    {
                        try
                        {
                            assemblyCache[fullPath] = File.ReadAllBytes(fullPath);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public void DisposeAll(HookRegistry hooks)
        {
            foreach (WindyPlugin plugin in plugins)
            {
                plugin.Dispose();
                hooks.Unregister(plugin);
            }

            plugins.Clear();
        }

        private Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName.Name)) return null;

            foreach (var directory in dependencyDirectories.Append(AppContext.BaseDirectory))
            {
                var path = Path.Combine(directory, assemblyName.Name + ".dll");
                var fullPath = Path.GetFullPath(path);

                if (assemblyCache.TryGetValue(fullPath, out var raw))
                {
                    using var ms = new MemoryStream(raw, false);
                    return context.LoadFromStream(ms);
                }

                try
                {
                    if (File.Exists(fullPath))
                    {
                        var bytes = File.ReadAllBytes(fullPath);
                        assemblyCache[fullPath] = bytes;
                        using var ms = new MemoryStream(bytes, false);
                        return context.LoadFromStream(ms);
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private void LoadFromAssembly(Assembly assembly, IReadOnlyList<Adaptor.Adaptor> adaptors, CommandRegistry commands, HookRegistry hooks)
        {
            foreach (Type type in assembly.GetExportedTypes())
            {
                if (!type.IsSubclassOf(typeof(WindyPlugin)) || type.IsAbstract)
                {
                    continue;
                }

                WindyPlugin plugin = (Activator.CreateInstance(type) as WindyPlugin)
                    ?? throw new InvalidOperationException($"无法创建插件 '{type.FullName}'.");
                Adaptor.Adaptor? adaptor = adaptors.FirstOrDefault(item => item.Type == plugin.RequiredAdaptor);
                if (adaptor == null)
                {
                    Message.Yellow($"[{plugin.Name}] 需要适配器 {plugin.RequiredAdaptor},已跳过加载.");
                    continue;
                }

                plugin.ProInitialize(new PluginContext(adaptor, commands, hooks));
                adaptor.BindPlugin(plugin);
                try
                {
                    plugin.Initialize();
                }
                finally
                {
                    adaptor.UnbindPlugin(plugin);
                }
                commands.RegisterFromPlugin(plugin);
                plugins.Add(plugin);
                Message.Yellow($"[{plugin.Name}] Version:{plugin.Version}(by {plugin.Author}) 成功加载.");
            }
        }
    }
}
