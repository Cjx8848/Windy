using Newtonsoft.Json.Linq;
using Windy.SDK.Events;
using Windy.SDK.Plugin;

namespace Windy.SDK.Adaptor.QQOfficial
{
    public enum QQOfficialEventType
    {
        Ready,
        Resumed,
        GroupAtMessageCreate,
        GroupMessageCreate,
        C2CMessageCreate,
        GroupAddRobot,
        GroupDelRobot,
        GroupMemberAdd,
        GroupMemberRemove,
        GroupMsgReceive,
        GroupMsgReject,
        FriendAdd,
        FriendDel,
        C2CMsgReceive,
        C2CMsgReject,
        SubscribeMessageStatus,
        InteractionCreate,
        MessageAuditPass,
        MessageAuditReject,
        AtMessageCreate,
        MessageCreate,
        DirectMessageCreate,
        Unknown,
    }

    public static class QQOfficialEventTypeExtensions
    {
        public static string ToEventName(this QQOfficialEventType type)
        {
            return type switch
            {
                QQOfficialEventType.Ready => "READY",
                QQOfficialEventType.Resumed => "RESUMED",
                QQOfficialEventType.GroupAtMessageCreate => "GROUP_AT_MESSAGE_CREATE",
                QQOfficialEventType.GroupMessageCreate => "GROUP_MESSAGE_CREATE",
                QQOfficialEventType.C2CMessageCreate => "C2C_MESSAGE_CREATE",
                QQOfficialEventType.GroupAddRobot => "GROUP_ADD_ROBOT",
                QQOfficialEventType.GroupDelRobot => "GROUP_DEL_ROBOT",
                QQOfficialEventType.GroupMemberAdd => "GROUP_MEMBER_ADD",
                QQOfficialEventType.GroupMemberRemove => "GROUP_MEMBER_REMOVE",
                QQOfficialEventType.GroupMsgReceive => "GROUP_MSG_RECEIVE",
                QQOfficialEventType.GroupMsgReject => "GROUP_MSG_REJECT",
                QQOfficialEventType.FriendAdd => "FRIEND_ADD",
                QQOfficialEventType.FriendDel => "FRIEND_DEL",
                QQOfficialEventType.C2CMsgReceive => "C2C_MSG_RECEIVE",
                QQOfficialEventType.C2CMsgReject => "C2C_MSG_REJECT",
                QQOfficialEventType.SubscribeMessageStatus => "SUBSCRIBE_MESSAGE_STATUS",
                QQOfficialEventType.InteractionCreate => "INTERACTION_CREATE",
                QQOfficialEventType.MessageAuditPass => "MESSAGE_AUDIT_PASS",
                QQOfficialEventType.MessageAuditReject => "MESSAGE_AUDIT_REJECT",
                QQOfficialEventType.AtMessageCreate => "AT_MESSAGE_CREATE",
                QQOfficialEventType.MessageCreate => "MESSAGE_CREATE",
                QQOfficialEventType.DirectMessageCreate => "DIRECT_MESSAGE_CREATE",
                _ => "UNKNOWN",
            };
        }

        public static QQOfficialEventType ToQQOfficialEventType(this string eventName)
        {
            return eventName switch
            {
                "READY" => QQOfficialEventType.Ready,
                "RESUMED" => QQOfficialEventType.Resumed,
                "GROUP_AT_MESSAGE_CREATE" => QQOfficialEventType.GroupAtMessageCreate,
                "GROUP_MESSAGE_CREATE" => QQOfficialEventType.GroupMessageCreate,
                "C2C_MESSAGE_CREATE" => QQOfficialEventType.C2CMessageCreate,
                "GROUP_ADD_ROBOT" => QQOfficialEventType.GroupAddRobot,
                "GROUP_DEL_ROBOT" => QQOfficialEventType.GroupDelRobot,
                "GROUP_MEMBER_ADD" => QQOfficialEventType.GroupMemberAdd,
                "GROUP_MEMBER_REMOVE" => QQOfficialEventType.GroupMemberRemove,
                "GROUP_MSG_RECEIVE" => QQOfficialEventType.GroupMsgReceive,
                "GROUP_MSG_REJECT" => QQOfficialEventType.GroupMsgReject,
                "FRIEND_ADD" => QQOfficialEventType.FriendAdd,
                "FRIEND_DEL" => QQOfficialEventType.FriendDel,
                "C2C_MSG_RECEIVE" => QQOfficialEventType.C2CMsgReceive,
                "C2C_MSG_REJECT" => QQOfficialEventType.C2CMsgReject,
                "SUBSCRIBE_MESSAGE_STATUS" => QQOfficialEventType.SubscribeMessageStatus,
                "INTERACTION_CREATE" => QQOfficialEventType.InteractionCreate,
                "MESSAGE_AUDIT_PASS" => QQOfficialEventType.MessageAuditPass,
                "MESSAGE_AUDIT_REJECT" => QQOfficialEventType.MessageAuditReject,
                "AT_MESSAGE_CREATE" => QQOfficialEventType.AtMessageCreate,
                "MESSAGE_CREATE" => QQOfficialEventType.MessageCreate,
                "DIRECT_MESSAGE_CREATE" => QQOfficialEventType.DirectMessageCreate,
                _ => QQOfficialEventType.Unknown,
            };
        }
    }

    public class QQOfficialEventArgs
    {
        protected QQOfficialEventArgs(AdaptorEventArgs source, QQOfficialEventType type)
        {
            Source = source;
            Type = type;
        }

        public AdaptorEventArgs Source { get; }

        public Adaptor Adaptor => Source.Adaptor;

        public QQOfficialEventType Type { get; }

        public string EventId => Source.Raw.Value<string>("id") ?? "";

        public JObject Raw => Source.Raw;

        public JObject Data => Source.Raw["d"] as JObject ?? new JObject();

        public bool Handled
        {
            get => Source.Handled;
            set => Source.Handled = value;
        }
    }

    public sealed class QQOfficialReadyEventArgs : QQOfficialEventArgs
    {
        public QQOfficialReadyEventArgs(AdaptorEventArgs source) : base(source, QQOfficialEventType.Ready)
        {
        }

        public string SessionId => Data.Value<string>("session_id") ?? "";
    }

    public sealed class QQOfficialSimpleEventArgs : QQOfficialEventArgs
    {
        public QQOfficialSimpleEventArgs(AdaptorEventArgs source, QQOfficialEventType type) : base(source, type)
        {
        }
    }

    public sealed class QQOfficialGroupOperationEventArgs : QQOfficialEventArgs
    {
        public QQOfficialGroupOperationEventArgs(AdaptorEventArgs source, QQOfficialEventType type) : base(source, type)
        {
        }

        public string GroupOpenId => Data.Value<string>("group_openid") ?? "";

        public string OperatorMemberOpenId => Data.Value<string>("op_member_openid") ?? Data.Value<string>("operator_openid") ?? "";

        public long Timestamp => Data.Value<long?>("timestamp") ?? 0;
    }

    public sealed class QQOfficialGroupMemberEventArgs : QQOfficialEventArgs
    {
        public QQOfficialGroupMemberEventArgs(AdaptorEventArgs source, QQOfficialEventType type) : base(source, type)
        {
        }

        public string GroupOpenId => Data.Value<string>("group_openid") ?? "";

        public string OperatorMemberOpenId => Data.Value<string>("op_member_openid") ?? Data.Value<string>("operator_openid") ?? "";

        public string MemberOpenId => Data.Value<string>("member_openid") ?? Data.Value<string>("user_openid") ?? "";

        public long Timestamp => Data.Value<long?>("timestamp") ?? 0;
    }

    public sealed class QQOfficialUserOperationEventArgs : QQOfficialEventArgs
    {
        public QQOfficialUserOperationEventArgs(AdaptorEventArgs source, QQOfficialEventType type) : base(source, type)
        {
        }

        public string OpenId => Data.Value<string>("openid") ?? Data.Value<string>("user_openid") ?? "";

        public long Timestamp => Data.Value<long?>("timestamp") ?? 0;
    }

    public sealed class QQOfficialSubscribeMessageStatusEventArgs : QQOfficialEventArgs
    {
        public QQOfficialSubscribeMessageStatusEventArgs(AdaptorEventArgs source) : base(source, QQOfficialEventType.SubscribeMessageStatus)
        {
        }

        public string OpenId => Data.Value<string>("openid") ?? Data.Value<string>("user_openid") ?? "";

        public string Status => Data.Value<string>("status") ?? "";

        public long Timestamp => Data.Value<long?>("timestamp") ?? 0;
    }

    public sealed class QQOfficialInteractionEventArgs : QQOfficialEventArgs
    {
        public QQOfficialInteractionEventArgs(AdaptorEventArgs source) : base(source, QQOfficialEventType.InteractionCreate)
        {
        }

        public string Id => Data.Value<string>("id") ?? "";
    }

    public sealed class QQOfficialMessageAuditEventArgs : QQOfficialEventArgs
    {
        public QQOfficialMessageAuditEventArgs(AdaptorEventArgs source, QQOfficialEventType type) : base(source, type)
        {
        }

        public string AuditId => Data.Value<string>("audit_id") ?? "";

        public string MessageId => Data.Value<string>("message_id") ?? "";
    }

    public static class QQOfficialHookExtensions
    {
        public static void RegisterQQOfficialEventHook(this WindyPlugin plugin, QQOfficialEventType type, Func<QQOfficialEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(type.ToEventName(), args => handler(new QQOfficialSimpleEventArgs(args, type)), priority);
        }

        public static void RegisterQQOfficialGroupAtNoCommandHook(this WindyPlugin plugin, Func<MessageEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterGroupAtNoCommandHook(handler, priority);
        }

        public static void RegisterGroupAddRobotHook(this WindyPlugin plugin, Func<QQOfficialGroupOperationEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.GroupAddRobot.ToEventName(), args => handler(new QQOfficialGroupOperationEventArgs(args, QQOfficialEventType.GroupAddRobot)), priority);
        }

        public static void RegisterGroupDelRobotHook(this WindyPlugin plugin, Func<QQOfficialGroupOperationEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.GroupDelRobot.ToEventName(), args => handler(new QQOfficialGroupOperationEventArgs(args, QQOfficialEventType.GroupDelRobot)), priority);
        }

        public static void RegisterGroupMemberAddHook(this WindyPlugin plugin, Func<QQOfficialGroupMemberEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.GroupMemberAdd.ToEventName(), args => handler(new QQOfficialGroupMemberEventArgs(args, QQOfficialEventType.GroupMemberAdd)), priority);
        }

        public static void RegisterGroupMemberRemoveHook(this WindyPlugin plugin, Func<QQOfficialGroupMemberEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.GroupMemberRemove.ToEventName(), args => handler(new QQOfficialGroupMemberEventArgs(args, QQOfficialEventType.GroupMemberRemove)), priority);
        }

        public static void RegisterGroupMsgReceiveHook(this WindyPlugin plugin, Func<QQOfficialGroupOperationEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.GroupMsgReceive.ToEventName(), args => handler(new QQOfficialGroupOperationEventArgs(args, QQOfficialEventType.GroupMsgReceive)), priority);
        }

        public static void RegisterGroupMsgRejectHook(this WindyPlugin plugin, Func<QQOfficialGroupOperationEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.GroupMsgReject.ToEventName(), args => handler(new QQOfficialGroupOperationEventArgs(args, QQOfficialEventType.GroupMsgReject)), priority);
        }

        public static void RegisterReadyHook(this WindyPlugin plugin, Func<QQOfficialReadyEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.Ready.ToEventName(), args => handler(new QQOfficialReadyEventArgs(args)), priority);
        }

        public static void RegisterResumedHook(this WindyPlugin plugin, Func<QQOfficialSimpleEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.Resumed.ToEventName(), args => handler(new QQOfficialSimpleEventArgs(args, QQOfficialEventType.Resumed)), priority);
        }

        public static void RegisterFriendAddHook(this WindyPlugin plugin, Func<QQOfficialUserOperationEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.FriendAdd.ToEventName(), args => handler(new QQOfficialUserOperationEventArgs(args, QQOfficialEventType.FriendAdd)), priority);
        }

        public static void RegisterFriendDelHook(this WindyPlugin plugin, Func<QQOfficialUserOperationEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.FriendDel.ToEventName(), args => handler(new QQOfficialUserOperationEventArgs(args, QQOfficialEventType.FriendDel)), priority);
        }

        public static void RegisterC2CMsgReceiveHook(this WindyPlugin plugin, Func<QQOfficialUserOperationEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.C2CMsgReceive.ToEventName(), args => handler(new QQOfficialUserOperationEventArgs(args, QQOfficialEventType.C2CMsgReceive)), priority);
        }

        public static void RegisterC2CMsgRejectHook(this WindyPlugin plugin, Func<QQOfficialUserOperationEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.C2CMsgReject.ToEventName(), args => handler(new QQOfficialUserOperationEventArgs(args, QQOfficialEventType.C2CMsgReject)), priority);
        }

        public static void RegisterSubscribeMessageStatusHook(this WindyPlugin plugin, Func<QQOfficialSubscribeMessageStatusEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.SubscribeMessageStatus.ToEventName(), args => handler(new QQOfficialSubscribeMessageStatusEventArgs(args)), priority);
        }

        public static void RegisterInteractionCreateHook(this WindyPlugin plugin, Func<QQOfficialInteractionEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.InteractionCreate.ToEventName(), args => handler(new QQOfficialInteractionEventArgs(args)), priority);
        }

        public static void RegisterMessageAuditPassHook(this WindyPlugin plugin, Func<QQOfficialMessageAuditEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.MessageAuditPass.ToEventName(), args => handler(new QQOfficialMessageAuditEventArgs(args, QQOfficialEventType.MessageAuditPass)), priority);
        }

        public static void RegisterMessageAuditRejectHook(this WindyPlugin plugin, Func<QQOfficialMessageAuditEventArgs, Task> handler, int priority = 0)
        {
            plugin.RegisterEventHook(QQOfficialEventType.MessageAuditReject.ToEventName(), args => handler(new QQOfficialMessageAuditEventArgs(args, QQOfficialEventType.MessageAuditReject)), priority);
        }
    }
}
