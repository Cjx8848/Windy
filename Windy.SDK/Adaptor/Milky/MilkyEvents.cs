using Newtonsoft.Json.Linq;
using Windy.SDK.Events;

namespace Windy.SDK.Adaptor.Milky
{
    public enum MilkyEventType
    {
        MessageReceive,
        MessageRecall,
        PeerPinChange,
        FriendRequest,
        GroupJoinRequest,
        GroupInvitedJoinRequest,
        GroupInvitation,
        FriendNudge,
        FriendFileUpload,
        GroupAdminChange,
        GroupEssenceMessageChange,
        GroupMemberIncrease,
        GroupMemberDecrease,
        GroupNameChange,
        GroupMessageReaction,
        GroupMute,
        GroupWholeMute,
        GroupNudge,
        GroupFileUpload,
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
                MilkyEventType.MessageRecall => "message_recall",
                MilkyEventType.PeerPinChange => "peer_pin_change",
                MilkyEventType.FriendRequest => "friend_request",
                MilkyEventType.GroupJoinRequest => "group_join_request",
                MilkyEventType.GroupInvitedJoinRequest => "group_invited_join_request",
                MilkyEventType.GroupInvitation => "group_invitation",
                MilkyEventType.FriendNudge => "friend_nudge",
                MilkyEventType.FriendFileUpload => "friend_file_upload",
                MilkyEventType.GroupAdminChange => "group_admin_change",
                MilkyEventType.GroupEssenceMessageChange => "group_essence_message_change",
                MilkyEventType.GroupMemberIncrease => "group_member_increase",
                MilkyEventType.GroupMemberDecrease => "group_member_decrease",
                MilkyEventType.GroupNameChange => "group_name_change",
                MilkyEventType.GroupMessageReaction => "group_message_reaction",
                MilkyEventType.GroupMute => "group_mute",
                MilkyEventType.GroupWholeMute => "group_whole_mute",
                MilkyEventType.GroupNudge => "group_nudge",
                MilkyEventType.GroupFileUpload => "group_file_upload",
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

        /// <summary>
        /// 忽略当前事件并阻止后续事件处理器继续处理。
        /// </summary>
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

        /// <summary>
        /// 同意当前好友请求。
        /// </summary>
        public Task AcceptAsync(CancellationToken cancellationToken = default)
        {
            return ((MilkyAdaptor)Adaptor).AcceptFriendRequestAsync(InitiatorUid, IsFiltered, cancellationToken);
        }

        /// <summary>
        /// 拒绝当前好友请求。
        /// </summary>
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

        /// <summary>
        /// 同意当前入群请求。
        /// </summary>
        public Task AcceptAsync(CancellationToken cancellationToken = default)
        {
            return ((MilkyAdaptor)Adaptor).AcceptGroupRequestAsync(GroupId, NotificationSeq, "join_request", IsFiltered, cancellationToken);
        }

        /// <summary>
        /// 拒绝当前入群请求。
        /// </summary>
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

        /// <summary>
        /// 同意当前群成员邀请他人入群请求。
        /// </summary>
        public Task AcceptAsync(CancellationToken cancellationToken = default)
        {
            return ((MilkyAdaptor)Adaptor).AcceptGroupRequestAsync(GroupId, NotificationSeq, "invited_join_request", false, cancellationToken);
        }

        /// <summary>
        /// 拒绝当前群成员邀请他人入群请求。
        /// </summary>
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

        /// <summary>
        /// 同意当前他人邀请自身入群事件。
        /// </summary>
        public Task AcceptAsync(CancellationToken cancellationToken = default)
        {
            return ((MilkyAdaptor)Adaptor).AcceptGroupInvitationAsync(GroupId, InvitationSeq, cancellationToken);
        }

        /// <summary>
        /// 拒绝当前他人邀请自身入群事件。
        /// </summary>
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

    public sealed class MilkyBotOfflineEventArgs : MilkyEventArgs
    {
        public MilkyBotOfflineEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.BotOffline)
        {
        }

        public string Reason => Data.Value<string>("reason") ?? "";
    }

    public sealed class MilkyMessageRecallEventArgs : MilkyEventArgs
    {
        public MilkyMessageRecallEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.MessageRecall)
        {
        }

        public string MessageScene => Data.Value<string>("message_scene") ?? "";

        public long PeerId => Data.Value<long?>("peer_id") ?? 0;

        public long MessageSeq => Data.Value<long?>("message_seq") ?? 0;

        public long SenderId => Data.Value<long?>("sender_id") ?? 0;

        public long OperatorId => Data.Value<long?>("operator_id") ?? 0;

        public string DisplaySuffix => Data.Value<string>("display_suffix") ?? "";
    }

    public sealed class MilkyPeerPinChangeEventArgs : MilkyEventArgs
    {
        public MilkyPeerPinChangeEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.PeerPinChange)
        {
        }

        public string MessageScene => Data.Value<string>("message_scene") ?? "";

        public long PeerId => Data.Value<long?>("peer_id") ?? 0;

        public bool IsPinned => Data.Value<bool?>("is_pinned") ?? false;
    }

    public sealed class MilkyFriendNudgeEventArgs : MilkyEventArgs
    {
        public MilkyFriendNudgeEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.FriendNudge)
        {
        }

        public long UserId => Data.Value<long?>("user_id") ?? 0;

        public bool IsSelfSend => Data.Value<bool?>("is_self_send") ?? false;

        public bool IsSelfReceive => Data.Value<bool?>("is_self_receive") ?? false;

        public string DisplayAction => Data.Value<string>("display_action") ?? "";

        public string DisplaySuffix => Data.Value<string>("display_suffix") ?? "";

        public string DisplayActionImageUrl => Data.Value<string>("display_action_img_url") ?? "";
    }

    public sealed class MilkyFileUploadEventArgs : MilkyEventArgs
    {
        public MilkyFileUploadEventArgs(AdaptorEventArgs source, MilkyEventType type) : base(source, type)
        {
        }

        public long? GroupId => Data.Value<long?>("group_id");

        public long UserId => Data.Value<long?>("user_id") ?? 0;

        public string FileId => Data.Value<string>("file_id") ?? "";

        public string FileName => Data.Value<string>("file_name") ?? "";

        public long FileSize => Data.Value<long?>("file_size") ?? 0;

        public string FileHash => Data.Value<string>("file_hash") ?? "";

        public bool IsSelf => Data.Value<bool?>("is_self") ?? false;
    }

    public sealed class MilkyGroupAdminChangeEventArgs : MilkyEventArgs
    {
        public MilkyGroupAdminChangeEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.GroupAdminChange)
        {
        }

        public long GroupId => Data.Value<long?>("group_id") ?? 0;

        public long UserId => Data.Value<long?>("user_id") ?? 0;

        public long OperatorId => Data.Value<long?>("operator_id") ?? 0;

        public bool IsSet => Data.Value<bool?>("is_set") ?? false;
    }

    public sealed class MilkyGroupEssenceMessageChangeEventArgs : MilkyEventArgs
    {
        public MilkyGroupEssenceMessageChangeEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.GroupEssenceMessageChange)
        {
        }

        public long GroupId => Data.Value<long?>("group_id") ?? 0;

        public long MessageSeq => Data.Value<long?>("message_seq") ?? 0;

        public long OperatorId => Data.Value<long?>("operator_id") ?? 0;

        public bool IsSet => Data.Value<bool?>("is_set") ?? false;
    }

    public sealed class MilkyGroupNameChangeEventArgs : MilkyEventArgs
    {
        public MilkyGroupNameChangeEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.GroupNameChange)
        {
        }

        public long GroupId => Data.Value<long?>("group_id") ?? 0;

        public string NewGroupName => Data.Value<string>("new_group_name") ?? "";

        public long OperatorId => Data.Value<long?>("operator_id") ?? 0;
    }

    public sealed class MilkyGroupMessageReactionEventArgs : MilkyEventArgs
    {
        public MilkyGroupMessageReactionEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.GroupMessageReaction)
        {
        }

        public long GroupId => Data.Value<long?>("group_id") ?? 0;

        public long UserId => Data.Value<long?>("user_id") ?? 0;

        public long MessageSeq => Data.Value<long?>("message_seq") ?? 0;

        public string FaceId => Data.Value<string>("face_id") ?? "";

        public string ReactionType => Data.Value<string>("reaction_type") ?? "face";

        public bool IsAdd => Data.Value<bool?>("is_add") ?? false;
    }

    public sealed class MilkyGroupMuteEventArgs : MilkyEventArgs
    {
        public MilkyGroupMuteEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.GroupMute)
        {
        }

        public long GroupId => Data.Value<long?>("group_id") ?? 0;

        public long UserId => Data.Value<long?>("user_id") ?? 0;

        public long OperatorId => Data.Value<long?>("operator_id") ?? 0;

        public int Duration => Data.Value<int?>("duration") ?? 0;
    }

    public sealed class MilkyGroupWholeMuteEventArgs : MilkyEventArgs
    {
        public MilkyGroupWholeMuteEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.GroupWholeMute)
        {
        }

        public long GroupId => Data.Value<long?>("group_id") ?? 0;

        public long OperatorId => Data.Value<long?>("operator_id") ?? 0;

        public bool IsMute => Data.Value<bool?>("is_mute") ?? false;
    }

    public sealed class MilkyGroupNudgeEventArgs : MilkyEventArgs
    {
        public MilkyGroupNudgeEventArgs(AdaptorEventArgs source) : base(source, MilkyEventType.GroupNudge)
        {
        }

        public long GroupId => Data.Value<long?>("group_id") ?? 0;

        public long SenderId => Data.Value<long?>("sender_id") ?? 0;

        public long ReceiverId => Data.Value<long?>("receiver_id") ?? 0;

        public string DisplayAction => Data.Value<string>("display_action") ?? "";

        public string DisplaySuffix => Data.Value<string>("display_suffix") ?? "";

        public string DisplayActionImageUrl => Data.Value<string>("display_action_img_url") ?? "";
    }

    public sealed partial class MilkyAdaptor
    {
        /// <summary>
        /// 机器人离线时触发。
        /// </summary>
        public event Func<MilkyBotOfflineEventArgs, Task> OnBotOffline
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.BotOffline.ToEventName(), args => value(new MilkyBotOfflineEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 消息被撤回时触发。
        /// </summary>
        public event Func<MilkyMessageRecallEventArgs, Task> OnMessageRecall
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.MessageRecall.ToEventName(), args => value(new MilkyMessageRecallEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 好友或群会话置顶状态变化时触发。
        /// </summary>
        public event Func<MilkyPeerPinChangeEventArgs, Task> OnPeerPinChange
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.PeerPinChange.ToEventName(), args => value(new MilkyPeerPinChangeEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 收到好友请求时触发。
        /// </summary>
        public event Func<MilkyFriendRequestEventArgs, Task> OnFriendRequest
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.FriendRequest.ToEventName(), args => value(new MilkyFriendRequestEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 收到用户入群请求时触发。
        /// </summary>
        public event Func<MilkyGroupJoinRequestEventArgs, Task> OnGroupJoinRequest
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupJoinRequest.ToEventName(), args => value(new MilkyGroupJoinRequestEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 收到群成员邀请他人入群请求时触发。
        /// </summary>
        public event Func<MilkyGroupInvitedJoinRequestEventArgs, Task> OnGroupInvitedJoinRequest
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupInvitedJoinRequest.ToEventName(), args => value(new MilkyGroupInvitedJoinRequestEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 收到他人邀请机器人入群时触发。
        /// </summary>
        public event Func<MilkyGroupInvitationEventArgs, Task> OnGroupInvitation
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupInvitation.ToEventName(), args => value(new MilkyGroupInvitationEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 收到好友戳一戳时触发。
        /// </summary>
        public event Func<MilkyFriendNudgeEventArgs, Task> OnFriendNudge
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.FriendNudge.ToEventName(), args => value(new MilkyFriendNudgeEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 收到好友文件上传时触发。
        /// </summary>
        public event Func<MilkyFileUploadEventArgs, Task> OnFriendFileUpload
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.FriendFileUpload.ToEventName(), args => value(new MilkyFileUploadEventArgs(args, MilkyEventType.FriendFileUpload)));
            remove { }
        }

        /// <summary>
        /// 群管理员变更时触发。
        /// </summary>
        public event Func<MilkyGroupAdminChangeEventArgs, Task> OnGroupAdminChange
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupAdminChange.ToEventName(), args => value(new MilkyGroupAdminChangeEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 群精华消息状态变更时触发。
        /// </summary>
        public event Func<MilkyGroupEssenceMessageChangeEventArgs, Task> OnGroupEssenceMessageChange
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupEssenceMessageChange.ToEventName(), args => value(new MilkyGroupEssenceMessageChangeEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 群成员增加时触发。
        /// </summary>
        public event Func<MilkyGroupMemberChangeEventArgs, Task> OnGroupMemberIncrease
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupMemberIncrease.ToEventName(), args => value(new MilkyGroupMemberChangeEventArgs(args, MilkyEventType.GroupMemberIncrease)));
            remove { }
        }

        /// <summary>
        /// 群成员减少时触发。
        /// </summary>
        public event Func<MilkyGroupMemberChangeEventArgs, Task> OnGroupMemberDecrease
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupMemberDecrease.ToEventName(), args => value(new MilkyGroupMemberChangeEventArgs(args, MilkyEventType.GroupMemberDecrease)));
            remove { }
        }

        /// <summary>
        /// 群名称变更时触发。
        /// </summary>
        public event Func<MilkyGroupNameChangeEventArgs, Task> OnGroupNameChange
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupNameChange.ToEventName(), args => value(new MilkyGroupNameChangeEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 群消息表情回应变化时触发。
        /// </summary>
        public event Func<MilkyGroupMessageReactionEventArgs, Task> OnGroupMessageReaction
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupMessageReaction.ToEventName(), args => value(new MilkyGroupMessageReactionEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 群成员禁言状态变化时触发。
        /// </summary>
        public event Func<MilkyGroupMuteEventArgs, Task> OnGroupMute
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupMute.ToEventName(), args => value(new MilkyGroupMuteEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 群全员禁言状态变化时触发。
        /// </summary>
        public event Func<MilkyGroupWholeMuteEventArgs, Task> OnGroupWholeMute
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupWholeMute.ToEventName(), args => value(new MilkyGroupWholeMuteEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 收到群戳一戳时触发。
        /// </summary>
        public event Func<MilkyGroupNudgeEventArgs, Task> OnGroupNudge
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupNudge.ToEventName(), args => value(new MilkyGroupNudgeEventArgs(args)));
            remove { }
        }

        /// <summary>
        /// 收到群文件上传时触发。
        /// </summary>
        public event Func<MilkyFileUploadEventArgs, Task> OnGroupFileUpload
        {
            add => CurrentPlugin.AddEventHandler(MilkyEventType.GroupFileUpload.ToEventName(), args => value(new MilkyFileUploadEventArgs(args, MilkyEventType.GroupFileUpload)));
            remove { }
        }
    }
}
