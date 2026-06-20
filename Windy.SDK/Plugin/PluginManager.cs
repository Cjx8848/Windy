using System.Reflection;
using Windy.SDK.Adaptor;
using Windy.SDK.Command;
using Windy.SDK.Hooks;

namespace Windy.SDK.Plugin
{
    public sealed class PluginManager
    {
        private readonly Dictionary<string, Assembly> loadedAssemblies = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<WindyPlugin> plugins = new();

        public IReadOnlyList<WindyPlugin> Plugins => plugins;

        public void Load(string pluginsDirectory, IReadOnlyList<Adaptor.Adaptor> adaptors, CommandRegistry commands, HookRegistry hooks)
        {
            Directory.CreateDirectory(pluginsDirectory);
            LoadFromAssembly(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly(), adaptors, commands, hooks);

            foreach (string pluginPath in Directory.GetFiles(pluginsDirectory, "*.dll"))
            {
                if (loadedAssemblies.ContainsKey(pluginPath))
                {
                    continue;
                }

                try
                {
                    Assembly assembly = Assembly.Load(File.ReadAllBytes(pluginPath));
                    loadedAssemblies.Add(pluginPath, assembly);
                    LoadFromAssembly(assembly, adaptors, commands, hooks);
                }
                catch (BadImageFormatException)
                {
                }
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
                plugin.Initialize();
                commands.RegisterFromPlugin(plugin);
                plugins.Add(plugin);
                Message.Yellow($"[{plugin.Name}] Version:{plugin.Version}(by {plugin.Author}) 成功加载.");
            }
        }
    }
}
