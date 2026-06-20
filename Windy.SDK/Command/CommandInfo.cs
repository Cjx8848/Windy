using System.Reflection;
using Windy.SDK.Events;
using Windy.SDK.Plugin;

namespace Windy.SDK.Command
{
    public sealed class CommandInfo
    {
        internal CommandInfo(CommandAttribute attribute, MethodInfo method, object? target, WindyPlugin plugin)
        {
            Name = attribute.Name;
            HelpText = attribute.HelpText;
            Parameters = attribute.Parameters;
            Scene = attribute.Scene;
            Method = method;
            Target = target;
            Plugin = plugin;
        }

        public string Name { get; }

        public string HelpText { get; }

        public string[] Parameters { get; }

        public MessageScene Scene { get; }

        public WindyPlugin Plugin { get; }

        public string Usage => Parameters.Length == 0 ? Name : $"{Name} {string.Join(" ", Parameters)}";

        internal MethodInfo Method { get; }

        internal object? Target { get; }
    }
}
