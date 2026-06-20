using System.Reflection;
using System.Text;
using Windy.SDK.Events;
using Windy.SDK.Plugin;

namespace Windy.SDK.Command
{
    public sealed class CommandRegistry
    {
        private readonly Dictionary<string, CommandInfo> commands = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<CommandInfo> All => commands.Values;

        public void RegisterFromPlugin(WindyPlugin plugin)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            foreach (MethodInfo method in plugin.GetType().GetMethods(flags))
            {
                foreach (CommandAttribute attribute in method.GetCustomAttributes<CommandAttribute>())
                {
                    Register(attribute, method, method.IsStatic ? null : plugin, plugin);
                }
            }
        }

        public async Task<bool> ExecuteAsync(MessageEventArgs message, CancellationToken cancellationToken = default)
        {
            string[] args = ParseCommandLine(message.Content.Trim());
            if (args.Length == 0)
            {
                return false;
            }

            string commandName = args[0].TrimStart('/', '!', '！');
            string key = GetKey(message.RawAdaptor.Type, message.Scene, commandName);
            if (!commands.TryGetValue(key, out CommandInfo? command))
            {
                return false;
            }

            try
            {
                CommandArgs commandArgs = new(command.Name, args[1..], message);
                object? result = Invoke(command, commandArgs);
                if (result is Task task)
                {
                    await task.WaitAsync(cancellationToken);
                }

                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                Message.Red($"指令 {command.Name} 执行失败: {ex.InnerException.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Message.Red($"指令 {command.Name} 执行失败: {ex.Message}");
                return false;
            }
        }

        public static string[] ParseCommandLine(string line)
        {
            List<string> args = new();
            StringBuilder builder = new();
            bool inQuotes = false;

            foreach (char current in line)
            {
                if (current == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(current) && !inQuotes)
                {
                    AddArg(args, builder);
                    continue;
                }

                builder.Append(current);
            }

            AddArg(args, builder);
            return args.ToArray();
        }

        private void Register(CommandAttribute attribute, MethodInfo method, object? target, WindyPlugin plugin)
        {
            ValidateMethod(method);
            string key = GetKey(plugin.RequiredAdaptor, attribute.Scene, attribute.Name);
            if (commands.ContainsKey(key))
            {
                throw new InvalidOperationException($"指令 '{attribute.Name}' 已经在适配器 '{plugin.RequiredAdaptor}' 注册过了.");
            }

            commands.Add(key, new CommandInfo(attribute, method, target, plugin));
        }

        private static string GetKey(Adaptor.AdaptorType adaptorType, MessageScene scene, string commandName)
        {
            return $"{adaptorType}:{scene}:{commandName}";
        }

        private static void ValidateMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            bool validParameters = parameters.Length == 0 || parameters.Length == 1 && parameters[0].ParameterType == typeof(CommandArgs);
            if (!validParameters)
            {
                throw new InvalidOperationException($"指令方法 '{method.DeclaringType?.FullName}.{method.Name}' 的参数出错.");
            }

            if (method.ReturnType != typeof(void) && !typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                throw new InvalidOperationException($"指令方法 '{method.DeclaringType?.FullName}.{method.Name}' 返回内容出错.");
            }
        }

        private static object? Invoke(CommandInfo command, CommandArgs args)
        {
            object?[]? parameters = command.Method.GetParameters().Length == 0 ? null : [args];
            return command.Method.Invoke(command.Target, parameters);
        }

        private static void AddArg(List<string> args, StringBuilder builder)
        {
            if (builder.Length == 0)
            {
                return;
            }

            args.Add(builder.ToString());
            builder.Clear();
        }
    }
}
