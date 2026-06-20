using Windy.SDK.Adaptor;
using Windy.SDK.Events;
using Windy.SDK.Plugin;

namespace Windy.SDK.Hooks
{
    public sealed class HookRegistry
    {
        private readonly List<HookEntry<MessageEventArgs>> messageHooks = new();
        private readonly List<HookEntry<AdaptorEventArgs>> eventHooks = new();

        public void RegisterMessage(WindyPlugin plugin, Func<MessageEventArgs, Task> handler, int priority = 0)
        {
            messageHooks.Add(new HookEntry<MessageEventArgs>(plugin.Name, plugin.RequiredAdaptor, null, priority, handler));
            Sort(messageHooks);
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

        public async Task ExecuteMessageAsync(MessageEventArgs args, CancellationToken cancellationToken = default)
        {
            foreach (HookEntry<MessageEventArgs> hook in messageHooks.Where(hook => hook.AdaptorType == args.RawAdaptor.Type))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await hook.Handler(args);
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
                await hook.Handler(args);
                if (args.Handled)
                {
                    return;
                }
            }
        }

        public void Unregister(WindyPlugin plugin)
        {
            messageHooks.RemoveAll(hook => string.Equals(hook.PluginName, plugin.Name, StringComparison.OrdinalIgnoreCase));
            eventHooks.RemoveAll(hook => string.Equals(hook.PluginName, plugin.Name, StringComparison.OrdinalIgnoreCase));
        }

        public void Clear()
        {
            messageHooks.Clear();
            eventHooks.Clear();
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
