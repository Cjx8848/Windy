using Windy.SDK.Events;

namespace Windy.SDK.Command
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class CommandAttribute : Attribute
    {
        public CommandAttribute(string name, string helpText = "", MessageScene scene = MessageScene.GroupAt, params string[] parameters)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("指令名称不能为空.", nameof(name));
            }

            Name = name;
            HelpText = helpText;
            Scene = scene;
            Parameters = parameters;
        }

        public string Name { get; }

        public string HelpText { get; }

        public MessageScene Scene { get; }

        public string[] Parameters { get; }
    }
}
