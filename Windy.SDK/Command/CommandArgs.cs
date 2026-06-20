using Windy.SDK.Adaptor;
using Windy.SDK.Events;

namespace Windy.SDK.Command
{
    public sealed class CommandArgs
    {
        public CommandArgs(string commandName, string[] parameters, MessageEventArgs message)
        {
            CommandName = commandName;
            Parameters = parameters;
            Message = message;
        }

        public string CommandName { get; }

        public string[] Parameters { get; }

        public MessageEventArgs Message { get; }

        public AdaptorMessageApi Adaptor => Message.Adaptor;

        public int Count => Parameters.Length;

        public bool Handled { get; set; }

        public string? Get(int index)
        {
            return index < 0 || index >= Parameters.Length ? null : Parameters[index];
        }

        public string GetOrDefault(int index, string defaultValue = "")
        {
            return Get(index) ?? defaultValue;
        }

        public bool Has(int index)
        {
            return index >= 0 && index < Parameters.Length;
        }

        public bool Require(int count)
        {
            return Parameters.Length >= count;
        }

        public bool TryGetLong(int index, out long value)
        {
            value = 0;
            return Has(index) && long.TryParse(Parameters[index], out value);
        }

        public bool TryGetInt(int index, out int value)
        {
            value = 0;
            return Has(index) && int.TryParse(Parameters[index], out value);
        }

        public ContextMessageContent CreateMessageContent() => new(Message);
    }
}
