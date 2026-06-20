using Windy.SDK.Adaptor;
using Windy.SDK.Command;
using Windy.SDK.Events;
using Windy.SDK.Hooks;

namespace Windy.SDK.Plugin
{
    public abstract class WindyPlugin : IDisposable
    {
        public abstract string Name { get; }

        public abstract string Version { get; }

        public abstract string Author { get; }

        public abstract string Description { get; }

        public abstract AdaptorType RequiredAdaptor { get; }

        public Adaptor.Adaptor Adaptor { get; protected set; } = null!;

        public HookRegistry Hooks { get; private set; } = null!;

        public abstract void Initialize();

        public void ProInitialize(PluginContext context)
        {
            Adaptor = context.Adaptor;
            Hooks = context.Hooks;
        }

        public void RegisterMessageHook(Func<MessageEventArgs, Task> handler, int priority = 0)
        {
            Hooks.RegisterMessage(this, handler, priority);
        }

        public void RegisterGroupAtNoCommandHook(Func<MessageEventArgs, Task> handler, int priority = 0)
        {
            Hooks.RegisterGroupAtNoCommand(this, handler, priority);
        }

        public void RegisterEventHook(string eventType, Func<AdaptorEventArgs, Task> handler, int priority = 0)
        {
            Hooks.RegisterEvent(this, eventType, handler, priority);
        }

        public void RegisterProHandleHook(Func<CommandArgs, Task> handler, int priority = 0)
        {
            Hooks.RegisterProHandle(this, handler, priority);
        }

        public virtual void Dispose()
        {
        }
    }

    public sealed class PluginContext
    {
        public PluginContext(Adaptor.Adaptor adaptor, Command.CommandRegistry commands, HookRegistry hooks)
        {
            Adaptor = adaptor;
            Commands = commands;
            Hooks = hooks;
        }

        public Adaptor.Adaptor Adaptor { get; }

        public Command.CommandRegistry Commands { get; }

        public HookRegistry Hooks { get; }
    }
}
