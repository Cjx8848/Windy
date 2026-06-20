namespace Windy.SDK
{
    public sealed class WindyConfig
    {
        public string OwnerOpenID { get; set; } = "";
        public bool Debug = false;
        public List<AdaptorConfig> Adaptors { get; set; } =
        [
            new AdaptorConfig
            {
                Name = "qq-official",
                Enabled = false,
                Settings = new Dictionary<string, string>
                {
                    ["AppId"] = "",
                    ["ClientSecret"] = "",
                    ["Sandbox"] = "false",
                },
            },
            new AdaptorConfig
            {
                Name = "milky",
                Enabled = false,
                Settings = new Dictionary<string, string>
                {
                    ["Endpoint"] = "ws://127.0.0.1:3001",
                    ["AccessToken"] = "",
                },
            },
        ];

        public PluginConfig Plugins { get; set; } = new();
    }

    public sealed class PluginConfig
    {
        public string Directory { get; set; } = "Plugins";
    }

    public sealed class AdaptorConfig
    {
        public string Name { get; set; } = "";

        public bool Enabled { get; set; }

        public Dictionary<string, string> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public string Get(string key, string defaultValue = "")
        {
            return Settings.TryGetValue(key, out string? value) ? value : defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return Settings.TryGetValue(key, out string? value) && bool.TryParse(value, out bool result)
                ? result
                : defaultValue;
        }
    }
}
