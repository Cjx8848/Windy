using Newtonsoft.Json.Linq;
using Windy.SDK.Events;
using Windy.SDK.Plugin;

namespace Windy.SDK.Adaptor.Milky
{
    public enum MilkyEventType
    {
        MessageReceive,
        FriendRequest,
        GroupJoinRequest,
        GroupInvitedJoinRequest,
        GroupInvitation,
        GroupMemberIncrease,
        GroupMemberDecrease,
        BotOffline,
        BotKicked,
        Unknown,
    }

    public static class MilkyEventTypeExtensions
    {
        public static string ToEventName(this MilkyEventType type)
        {
            return type switch
            {
                MilkyEventType.MessageReceive => "message_receive",
                MilkyEventType.FriendRequest => "friend_request",
                MilkyEventType.GroupJoinRequest => "group_join_request",
                MilkyEventType.GroupInvitedJoinRequest => "group_invited_join_request",
                MilkyEventType.GroupInvitation => "group_invitation",
                MilkyEventType.GroupMemberIncrease => "group_member_increase",
                MilkyEventType.GroupMemberDecrease => "group_member_decrease",
                MilkyEventType.BotOffline => "bot_offline",
                MilkyEventType.BotKicked => "bot_kicked",
                _ => "unknown",
            };
        }
    }

    public class MilkyEventArgs
    {
        protected MilkyEventArgs(AdaptorEventArgs source, MilkyEventType type)
        {
            Source = source;
            Type = type;
        }

        public AdaptorEventArgs Source { get; }

        public Adaptor Adaptor => Source.Adaptor;

        public MilkyEventType Type { get; }

        public JObject Raw => Source.Raw;

        public JObject Data => Source.Raw["data"] as JObject ?? new JObject();

        public long SelfId => Source.Raw.Value<long?>("self_id") ?? 0;

        public long Time => Source.Raw.Value<long?>("time") ?? 0;

        public bool Ignored { get; private set; }

        public bool Handled
        {
            get => Source.Handled;
            set => Source.Handled = value;
        }

        public void Ignore()
        {
            Ignored = true;
            Handled = true;
        }
    }

    public sealed class MilkyFriendRequestEventArgs : MilkyEventArgs
    {
        public MilkyFriendRequestEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.FriendRequest)
        {
        }

        public long InitiatorId => Data.Value<long?>("initiator_id") ?? 0;

        public string InitiatorUid => Data.Value<string>("initiator_uid") ?? "";

        public string Comment => Data.Value<string>("comment") ?? "";

        public string Via => Data.Value<string>("via") ?? "";

        public bool IsFiltered => Data.Value<bool?>("is_filtered") ?? false;

        public Task AcceptAsync(CancellationToken cancellationToken = default)
        {
            return ((MilkyAdaptor)Adaptor).AcceptFriendRequestAsync(InitiatorUid, IsFiltered, cancellationToken);
        }

        public Task RejectAsync(string reason = "", CancellationToken cancellationToken = default)
        {
            return ((MilkyAdaptor)Adaptor).RejectFriendRequestAsync(InitiatorUid, reason, IsFiltered, cancellationToken);
        }
    }

    public sealed class MilkyGroupJoinRequestEventArgs : MilkyEventArgs
    {
        public MilkyGroupJoinRequestEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.GroupJoinRequest)
        {
        }

        public long NotificationSeq => Data.Value<long?>("notification_seq") ?? 0;

        public long GroupId => Data.Value<long?>("group_id") ?? 0;

        public long InitiatorId => Data.Value<long?>("initiator_id") ?? 0;

        public string Comment => Data.Value<string>("comment") ?? "";

        public bool IsFiltered => Data.Value<bool?>("is_filtered") ?? false;

        public Task AcceptAsync(CancellationToken cancellationToken = default)
        {
            return ((MilkyAdaptor)Adaptor).AcceptGroupRequestAsync(GroupId, NotificationSeq, "join_request", IsFiltered, cancellationToken);
        }

        public Task RejectAsync(string reason = "", CancellationToken cancellationToken = default)
        {
            return ((MilkyAdaptor)Adaptor).RejectGroupRequestAsync(GroupId, NotificationSeq, "join_request", reason, IsFiltered, cancellationToken);
        }
    }

    public sealed class MilkyGroupInvitedJoinRequestEventArgs : MilkyEventArgs
    {
        public MilkyGroupInvitedJoinRequestEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.GroupInvitedJoinRequest)
        {
        }

        public long NotificationSeq => Data.Value<long?>("notification_seq") ?? 0;

        public long GroupId => Data.Value<long?>("group_id") ?? 0;

        public long InitiatorId => Data.Value<long?>("initiator_id") ?? 0;

        public long TargetUserId => Data.Value<long?>("target_user_id") ?? 0;

        public Task AcceptAsync(CancellationToken cancellationToken = default)
        {
            return ((MilkyAdaptor)Adaptor).AcceptGroupRequestAsync(GroupId, NotificationSeq, "invited_join_request", false, cancellationToken);
        }

        public Task RejectAsync(string reason = "", CancellationToken cancellationToken = default)
        {
            return ((MilkyAdaptor)Adaptor).RejectGroupRequestAsync(GroupId, NotificationSeq, "invited_join_request", reason, false, cancellationToken);
        }
    }

    public sealed class MilkyGroupInvitationEventArgs : MilkyEventArgs
    {
        public MilkyGroupInvitationEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.GroupInvitation)
        {
        }

        public long InvitationSeq => Data.Value<long?>("invitation_seq") ?? 0;

        public long GroupId => Data.Value<long?>("group_id") ?? 0;

        public long SourceGroupId => Data.Value<long?>("source_group_id") ?? 0;

        public long InitiatorId => Data.Value<long?>("initiator_id") ?? 0;

        public Task AcceptAsync(CancellationToken cancellationToken = default)
        {
            return ((MilkyAdaptor)Adaptor).AcceptGroupInvitationAsync(GroupId, InvitationSeq, cancellationToken);
        }

        public Task RejectAsync(CancellationToken cancellationToken = default)
        {
            return ((MilkyAdaptor)Adaptor).RejectGroupInvitationAsync(GroupId, InvitationSeq, cancellationToken);
        }
    }

    public sealed class MilkyGroupMemberChangeEventArgs : MilkyEventArgs
    {
        public MilkyGroupMemberChangeEventArgs(AdaptorEventArgs source, MilkyEventType type) : base(source, type)
        {
        }

        public long UserId => Data.Value<long?>("user_id") ?? 0;

        public long GroupId => Data.Value<long?>("group_id") ?? 0;

        public long? OperatorId => Data.Value<long?>("operator_id");

        public long? InvitorId => Data.Value<long?>("invitor_id");
    }

    public static class MilkyHookExtensions
    {
        public static void RegisterFriendRequestHook(this WindyPlugin plugin, Func<MilkyFriendRequestEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(MilkyEventType.FriendRequest.ToEventName(), args => handler(new MilkyFriendRequestEventArgs(args)), priority);
        }

        public static void RegisterGroupJoinRequestHook(this WindyPlugin plugin, Func<MilkyGroupJoinRequestEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(MilkyEventType.GroupJoinRequest.ToEventName(), args => handler(new MilkyGroupJoinRequestEventArgs(args)), priority);
        }

        public static void RegisterGroupInvitedJoinRequestHook(this WindyPlugin plugin, Func<MilkyGroupInvitedJoinRequestEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(MilkyEventType.GroupInvitedJoinRequest.ToEventName(), args => handler(new MilkyGroupInvitedJoinRequestEventArgs(args)), priority);
        }

        public static void RegisterGroupInvitationHook(this WindyPlugin plugin, Func<MilkyGroupInvitationEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(MilkyEventType.GroupInvitation.ToEventName(), args => handler(new MilkyGroupInvitationEventArgs(args)), priority);
        }

        public static void RegisterGroupMemberIncreaseHook(this WindyPlugin plugin, Func<MilkyGroupMemberChangeEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(MilkyEventType.GroupMemberIncrease.ToEventName(), args => handler(new MilkyGroupMemberChangeEventArgs(args, MilkyEventType.GroupMemberIncrease)), priority);
        }

        public static void RegisterGroupMemberDecreaseHook(this WindyPlugin plugin, Func<MilkyGroupMemberChangeEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(MilkyEventType.GroupMemberDecrease.ToEventName(), args => handler(new MilkyGroupMemberChangeEventArgs(args, MilkyEventType.GroupMemberDecrease)), priority);
        }
    }
}
