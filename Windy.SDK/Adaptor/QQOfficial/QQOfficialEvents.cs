using Newtonsoft.Json.Linq;
using Windy.SDK.Adaptor;
using Windy.SDK.Events;

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

        public Task SendMessage(SendTarget target, MessageContent content, CancellationToken cancellationToken = default)
        {
            SendOptions options = new()
            {
                EventId = EventId,
                Passive = true,
            };
            return Adaptor.SendMessage(target, content, options, cancellationToken);
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

        public Task SendToGroup(MessageContent content, CancellationToken cancellationToken = default)
        {
            return SendMessage(SendTarget.Group(GroupOpenId), content, cancellationToken);
        }
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

        public Task SendToGroup(MessageContent content, CancellationToken cancellationToken = default)
        {
            return SendMessage(SendTarget.Group(GroupOpenId), content, cancellationToken);
        }
    }

    public sealed class QQOfficialUserOperationEventArgs : QQOfficialEventArgs
    {
        public QQOfficialUserOperationEventArgs(AdaptorEventArgs source, QQOfficialEventType type) : base(source, type)
        {
        }

        public string OpenId => Data.Value<string>("openid") ?? Data.Value<string>("user_openid") ?? "";

        public long Timestamp => Data.Value<long?>("timestamp") ?? 0;

        public Task SendToUser(MessageContent content, CancellationToken cancellationToken = default)
        {
            return SendMessage(SendTarget.User(OpenId), content, cancellationToken);
        }
    }

    public sealed class QQOfficialSubscribeMessageStatusEventArgs : QQOfficialEventArgs
    {
        public QQOfficialSubscribeMessageStatusEventArgs(AdaptorEventArgs source) : base(source, QQOfficialEventType.SubscribeMessageStatus)
        {
        }

        public string OpenId => Data.Value<string>("openid") ?? Data.Value<string>("user_openid") ?? "";

        public string Status => Data.Value<string>("status") ?? "";

        public long Timestamp => Data.Value<long?>("timestamp") ?? 0;

        public Task SendToUser(MessageContent content, CancellationToken cancellationToken = default)
        {
            return SendMessage(SendTarget.User(OpenId), content, cancellationToken);
        }
    }

    public sealed class QQOfficialInteractionEventArgs : QQOfficialEventArgs
    {
        public QQOfficialInteractionEventArgs(AdaptorEventArgs source) : base(source, QQOfficialEventType.InteractionCreate)
        {
        }

        public string Id => Data.Value<string>("id") ?? "";
    }

    public sealed class QQOfficialC2CMessageEventArgs : QQOfficialEventArgs
    {
        public QQOfficialC2CMessageEventArgs(AdaptorEventArgs source) : base(source, QQOfficialEventType.C2CMessageCreate)
        {
        }

        public string AuthorId => (Data["author"] as JObject)?.Value<string>("user_openid")
            ?? Data.Value<string>("openid") ?? "";

        public string AuthorName => (Data["author"] as JObject)?.Value<string>("username") ?? "";

        public string Content => Data.Value<string>("content") ?? "";

        public string MessageId => Data.Value<string>("id") ?? "";

        public long Timestamp => Data.Value<long?>("timestamp") ?? 0;

        public Task SendToUser(MessageContent content, CancellationToken cancellationToken = default)
        {
            return SendMessage(SendTarget.User(AuthorId), content, cancellationToken);
        }

        public Task SendMessage(MessageContent content, CancellationToken cancellationToken = default)
        {
            SendOptions options = new()
            {
                MessageId = MessageId,
                Passive = true,
            };
            return Adaptor.SendMessage(SendTarget.User(AuthorId), content, options, cancellationToken);
        }

        public Task SendMessage(string text, CancellationToken cancellationToken = default)
        {
            return SendMessage(new MessageContent().AddText(text), cancellationToken);
        }
    }

    public sealed class QQOfficialMessageAuditEventArgs : QQOfficialEventArgs
    {
        public QQOfficialMessageAuditEventArgs(AdaptorEventArgs source, QQOfficialEventType type) : base(source, type)
        {
        }

        public string AuditId => Data.Value<string>("audit_id") ?? "";

        public string MessageId => Data.Value<string>("message_id") ?? "";
    }

    public sealed partial class QQOfficialAdaptor
    {
        /// <summary>
        /// QQ 官方网关 READY 事件。
        /// </summary>
        public event Func<QQOfficialReadyEventArgs, Task> OnReady
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.Ready.ToEventName(), args => value(new QQOfficialReadyEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// QQ 官方网关 RESUMED 事件。
        /// </summary>
        public event Func<QQOfficialSimpleEventArgs, Task> OnResumed
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.Resumed.ToEventName(), args => value(new QQOfficialSimpleEventArgs(args, QQOfficialEventType.Resumed)));
            remove { }
        }

        /// <summary>
        /// 机器人被添加到群时触发。
        /// </summary>
        public event Func<QQOfficialGroupOperationEventArgs, Task> OnGroupAddRobot
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.GroupAddRobot.ToEventName(), args => value(new QQOfficialGroupOperationEventArgs(args, QQOfficialEventType.GroupAddRobot)));
            remove { }
        }

        /// <summary>
        /// 机器人被移出群时触发。
        /// </summary>
        public event Func<QQOfficialGroupOperationEventArgs, Task> OnGroupDelRobot
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.GroupDelRobot.ToEventName(), args => value(new QQOfficialGroupOperationEventArgs(args, QQOfficialEventType.GroupDelRobot)));
            remove { }
        }

        /// <summary>
        /// QQ 官方群成员增加时触发。
        /// </summary>
        public event Func<QQOfficialGroupMemberEventArgs, Task> OnGroupMemberAdd
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.GroupMemberAdd.ToEventName(), args => value(new QQOfficialGroupMemberEventArgs(args, QQOfficialEventType.GroupMemberAdd)));
            remove { }
        }

        /// <summary>
        /// QQ 官方群成员减少时触发。
        /// </summary>
        public event Func<QQOfficialGroupMemberEventArgs, Task> OnGroupMemberRemove
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.GroupMemberRemove.ToEventName(), args => value(new QQOfficialGroupMemberEventArgs(args, QQOfficialEventType.GroupMemberRemove)));
            remove { }
        }

        /// <summary>
        /// 群消息接收权限打开时触发。
        /// </summary>
        public event Func<QQOfficialGroupOperationEventArgs, Task> OnGroupMsgReceive
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.GroupMsgReceive.ToEventName(), args => value(new QQOfficialGroupOperationEventArgs(args, QQOfficialEventType.GroupMsgReceive)));
            remove { }
        }

        /// <summary>
        /// 群消息接收权限关闭时触发。
        /// </summary>
        public event Func<QQOfficialGroupOperationEventArgs, Task> OnGroupMsgReject
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.GroupMsgReject.ToEventName(), args => value(new QQOfficialGroupOperationEventArgs(args, QQOfficialEventType.GroupMsgReject)));
            remove { }
        }

        /// <summary>
        /// 用户添加机器人为好友时触发。
        /// </summary>
        public event Func<QQOfficialUserOperationEventArgs, Task> OnFriendAdd
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.FriendAdd.ToEventName(), args => value(new QQOfficialUserOperationEventArgs(args, QQOfficialEventType.FriendAdd)));
            remove { }
        }

        /// <summary>
        /// 用户删除机器人好友关系时触发。
        /// </summary>
        public event Func<QQOfficialUserOperationEventArgs, Task> OnFriendDel
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.FriendDel.ToEventName(), args => value(new QQOfficialUserOperationEventArgs(args, QQOfficialEventType.FriendDel)));
            remove { }
        }

        /// <summary>
        /// C2C 消息接收权限打开时触发。
        /// </summary>
        public event Func<QQOfficialUserOperationEventArgs, Task> OnC2CMsgReceive
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.C2CMsgReceive.ToEventName(), args => value(new QQOfficialUserOperationEventArgs(args, QQOfficialEventType.C2CMsgReceive)));
            remove { }
        }

        /// <summary>
        /// C2C 消息接收权限关闭时触发。
        /// </summary>
        public event Func<QQOfficialUserOperationEventArgs, Task> OnC2CMsgReject
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.C2CMsgReject.ToEventName(), args => value(new QQOfficialUserOperationEventArgs(args, QQOfficialEventType.C2CMsgReject)));
            remove { }
        }

        /// <summary>
        /// 订阅消息状态变化时触发。
        /// </summary>
        public event Func<QQOfficialSubscribeMessageStatusEventArgs, Task> OnSubscribeMessageStatus
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.SubscribeMessageStatus.ToEventName(), args => value(new QQOfficialSubscribeMessageStatusEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 交互事件创建时触发。
        /// </summary>
        public event Func<QQOfficialInteractionEventArgs, Task> OnInteractionCreate
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.InteractionCreate.ToEventName(), args => value(new QQOfficialInteractionEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 消息审核通过时触发。
        /// </summary>
        public event Func<QQOfficialMessageAuditEventArgs, Task> OnMessageAuditPass
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.MessageAuditPass.ToEventName(), args => value(new QQOfficialMessageAuditEventArgs(args, QQOfficialEventType.MessageAuditPass)));
            remove { }
        }

        /// <summary>
        /// 消息审核拒绝时触发。
        /// </summary>
        public event Func<QQOfficialMessageAuditEventArgs, Task> OnMessageAuditReject
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.MessageAuditReject.ToEventName(), args => value(new QQOfficialMessageAuditEventArgs(args, QQOfficialEventType.MessageAuditReject)));
            remove { }
        }

        /// <summary>
        /// 收到 C2C 私聊消息时触发。
        /// </summary>
        public event Func<QQOfficialC2CMessageEventArgs, Task> OnC2CMessage
        {
            add => CurrentPlugin.AddEventHandler(QQOfficialEventType.C2CMessageCreate.ToEventName(), args => value(new QQOfficialC2CMessageEventArgs(args)));
            remove { }
        }
    }

}
