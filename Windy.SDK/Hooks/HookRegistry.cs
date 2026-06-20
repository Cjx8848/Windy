using Windy.SDK.Adaptor;
using Windy.SDK.Command;
using Windy.SDK.Events;
using Windy.SDK.Plugin;

namespace Windy.SDK.Hooks
{
    public sealed class HookRegistry
    {
        private readonly List<HookEntry<MessageEventArgs>> messageHooks = new();
        private readonly List<HookEntry<MessageEventArgs>> groupAtNoCommandHooks = new();
        private readonly List<HookEntry<AdaptorEventArgs>> eventHooks = new();
        private readonly List<HookEntry<CommandArgs>> proHandleHooks = new();

        public void RegisterMessage(WindyPlugin plugin, Func<MessageEventArgs, Task> handler, int priority = 0)
        {
            messageHooks.Add(new HookEntry<MessageEventArgs>(plugin.Name, plugin.RequiredAdaptor, null, priority, handler));
            Sort(messageHooks);
        }

        public void RegisterGroupAtNoCommand(WindyPlugin plugin, Func<MessageEventArgs, Task> handler, int priority = 0)
        {
            groupAtNoCommandHooks.Add(new HookEntry<MessageEventArgs>(plugin.Name, plugin.RequiredAdaptor, null, priority, handler));
            Sort(groupAtNoCommandHooks);
        }

        public void RegisterEvent(WindyPlugin plugin, string eventType, Func<AdaptorEventArgs, Task> handler, int priority = 0)
        {
            if (string.IsNullOrWhiteSpace(eventType))
            {
                throw new ArgumentException("事件类型不能为空.", nameof(eventType));
            }

            eventHooks.Add(new HookEntry<AdaptorEventArgs>(plugin.Name, plugin.RequiredAdaptor, eventType, priority, handler));
            Sort(eventHooks);
        }

        public void RegisterProHandle(WindyPlugin plugin, Func<CommandArgs, Task> handler, int priority = 0)
        {
            proHandleHooks.Add(new HookEntry<CommandArgs>(plugin.Name, plugin.RequiredAdaptor, null, priority, handler));
            Sort(proHandleHooks);
        }

        public async Task ExecuteMessageAsync(MessageEventArgs args, CancellationToken cancellationToken = default)
        {
            foreach (HookEntry<MessageEventArgs> hook in messageHooks.Where(hook => hook.AdaptorType == args.RawAdaptor.Type))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await InvokeSafely(hook.Handler, args, hook.PluginName, "Message");
                if (args.Handled)
                {
                    return;
                }
            }
        }

        public async Task ExecuteGroupAtNoCommandAsync(MessageEventArgs args, CancellationToken cancellationToken = default)
        {
            if (args.Scene != MessageScene.GroupAt)
            {
                return;
            }

            foreach (HookEntry<MessageEventArgs> hook in groupAtNoCommandHooks.Where(hook => hook.AdaptorType == args.RawAdaptor.Type))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await InvokeSafely(hook.Handler, args, hook.PluginName, "GroupAtNoCommand");
                if (args.Handled)
                {
                    return;
                }
            }
        }

        public async Task ExecuteEventAsync(AdaptorEventArgs args, CancellationToken cancellationToken = default)
        {
            foreach (HookEntry<AdaptorEventArgs> hook in eventHooks.Where(hook => hook.AdaptorType == args.Adaptor.Type && Matches(hook.EventType, args.Type)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await InvokeSafely(hook.Handler, args, hook.PluginName, "Event");
                if (args.Handled)
                {
                    return;
                }
            }
        }

        public async Task ExecuteProHandleAsync(CommandArgs args, CancellationToken cancellationToken = default)
        {
            foreach (HookEntry<CommandArgs> hook in proHandleHooks.Where(hook => hook.AdaptorType == args.Message.RawAdaptor.Type))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await InvokeSafely(hook.Handler, args, hook.PluginName, "ProHandle");
                if (args.Handled)
                {
                    args.Message.Handled = true;
                    return;
                }
            }
        }

        private static async Task InvokeSafely<T>(Func<T, Task> handler, T args, string pluginName, string hookType)
        {
            try
            {
                await handler(args);
            }
            catch (Exception ex)
            {
                Message.Red($"[Hook][{hookType}] {pluginName} 处理异常: {ex.Message}");
            }
        }

        public void Unregister(WindyPlugin plugin)
        {
            messageHooks.RemoveAll(hook => string.Equals(hook.PluginName, plugin.Name, StringComparison.OrdinalIgnoreCase));
            groupAtNoCommandHooks.RemoveAll(hook => string.Equals(hook.PluginName, plugin.Name, StringComparison.OrdinalIgnoreCase));
            eventHooks.RemoveAll(hook => string.Equals(hook.PluginName, plugin.Name, StringComparison.OrdinalIgnoreCase));
            proHandleHooks.RemoveAll(hook => string.Equals(hook.PluginName, plugin.Name, StringComparison.OrdinalIgnoreCase));
        }

        public void Clear()
        {
            messageHooks.Clear();
            groupAtNoCommandHooks.Clear();
            eventHooks.Clear();
            proHandleHooks.Clear();
        }

        private static bool Matches(string? expected, string actual)
        {
            return expected == "*" || string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
        }

        private static void Sort<T>(List<HookEntry<T>> hooks)
        {
            hooks.Sort((left, right) => right.Priority.CompareTo(left.Priority));
        }
    }

    internal sealed class HookEntry<T>
    {
        public HookEntry(string pluginName, AdaptorType adaptorType, string? eventType, int priority, Func<T, Task> handler)
        {
            PluginName = pluginName;
            AdaptorType = adaptorType;
            EventType = eventType;
            Priority = priority;
            Handler = handler;
        }

        public string PluginName { get; }

        public AdaptorType AdaptorType { get; }

        public string? EventType { get; }

        public int Priority { get; }

        public Func<T, Task> Handler { get; }
    }
}
