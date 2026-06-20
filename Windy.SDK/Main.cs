namespace Windy.SDK
{
    public static class WindyRuntime
    {
        private static readonly List<Adaptor.Adaptor> Adaptors = new();

        public static WindyConfig Config { get; private set; } = new();

        public static IReadOnlyList<Adaptor.Adaptor> LoadedAdaptors => Adaptors;

        public static Plugin.PluginManager Plugins { get; } = new();

        public static Command.CommandRegistry Commands { get; } = new();

        public static Hooks.HookRegistry Hooks { get; } = new();

        public static async Task InitializeAsync(WindyConfig config, CancellationToken cancellationToken = default)
        {
            Config = config;
            Adaptors.Clear();

            foreach (AdaptorConfig adaptorConfig in config.Adaptors.Where(adaptor => adaptor.Enabled))
            {
                Adaptor.Adaptor adaptor = CreateAdaptor(adaptorConfig);
                adaptor.MessageReceived += async (_, args) =>
                {
                    await Hooks.ExecuteMessageAsync(args, cancellationToken);
                    if (!args.Handled)
                    {
                        await Commands.ExecuteAsync(args, cancellationToken);
                    }
                };
                adaptor.EventReceived += async (_, args) => await Hooks.ExecuteEventAsync(args, cancellationToken);
                Adaptors.Add(adaptor);
            }

            Plugins.Load(Path.Combine(AppContext.BaseDirectory, config.Plugins.Directory), Adaptors, Commands, Hooks);

            foreach (Adaptor.Adaptor adaptor in Adaptors)
            {
                await adaptor.StartAsync(cancellationToken);
            }
        }

        public static async Task StopAsync(CancellationToken cancellationToken = default)
        {
            Plugins.DisposeAll(Hooks);
            Hooks.Clear();
            foreach (Adaptor.Adaptor adaptor in Adaptors)
            {
                await adaptor.StopAsync(cancellationToken);
            }

            Adaptors.Clear();
        }

        public static T GetAdaptor<T>(string name) where T : Adaptor.Adaptor
        {
            Adaptor.Adaptor? adaptor = Adaptors.FirstOrDefault(item =>
                string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            return adaptor as T ?? throw new InvalidOperationException($"适配器 '{name}' 未加载或类型不匹配.");
        }

        public static T GetAdaptor<T>(Adaptor.AdaptorType type) where T : Adaptor.Adaptor
        {
            Adaptor.Adaptor? adaptor = Adaptors.FirstOrDefault(item => item.Type == type);
            return adaptor as T ?? throw new InvalidOperationException($"适配器 '{type}' 未加载或类型不匹配.");
        }

        private static Adaptor.Adaptor CreateAdaptor(AdaptorConfig config)
        {
            return config.Name.ToLowerInvariant() switch
            {
                "qq-official" or "qqofficial" or "qq" => new Adaptor.QQOfficial.QQOfficialAdaptor(config),
                "milky" => new Adaptor.Milky.MilkyAdaptor(config),
                _ => throw new InvalidOperationException($"未知适配器: {config.Name}"),
            };
        }

    }
}
