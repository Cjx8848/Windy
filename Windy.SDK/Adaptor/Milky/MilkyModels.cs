using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Windy.SDK.Adaptor.Milky
{
    /// <summary>
    /// Milky 登录账号信息。
    /// </summary>
    public sealed class MilkyLoginInfo
    {
        /// <summary>登录 QQ 号。</summary>
        [JsonProperty("uin")]
        public long Uin { get; set; }

        /// <summary>登录昵称。</summary>
        [JsonProperty("nickname")]
        public string Nickname { get; set; } = "";
    }

    /// <summary>
    /// Milky 协议端实现信息。
    /// </summary>
    public sealed class MilkyImplInfo
    {
        /// <summary>协议端名称。</summary>
        [JsonProperty("impl_name")]
        public string ImplName { get; set; } = "";

        /// <summary>协议端版本。</summary>
        [JsonProperty("impl_version")]
        public string ImplVersion { get; set; } = "";

        /// <summary>QQ 协议版本。</summary>
        [JsonProperty("qq_protocol_version")]
        public string QqProtocolVersion { get; set; } = "";

        /// <summary>QQ 协议平台。</summary>
        [JsonProperty("qq_protocol_type")]
        public string QqProtocolType { get; set; } = "";

        /// <summary>Milky 协议版本。</summary>
        [JsonProperty("milky_version")]
        public string MilkyVersion { get; set; } = "";
    }

    /// <summary>
    /// QQ 用户个人资料。
    /// </summary>
    public sealed class MilkyUserProfile
    {
        /// <summary>昵称。</summary>
        [JsonProperty("nickname")]
        public string Nickname { get; set; } = "";

        /// <summary>QID。</summary>
        [JsonProperty("qid")]
        public string Qid { get; set; } = "";

        /// <summary>年龄。</summary>
        [JsonProperty("age")]
        public int Age { get; set; }

        /// <summary>性别，可能为 male、female、unknown。</summary>
        [JsonProperty("sex")]
        public string Sex { get; set; } = "unknown";

        /// <summary>备注。</summary>
        [JsonProperty("remark")]
        public string Remark { get; set; } = "";

        /// <summary>个性签名。</summary>
        [JsonProperty("bio")]
        public string Bio { get; set; } = "";

        /// <summary>QQ 等级。</summary>
        [JsonProperty("level")]
        public int Level { get; set; }

        /// <summary>国家或地区。</summary>
        [JsonProperty("country")]
        public string Country { get; set; } = "";

        /// <summary>城市。</summary>
        [JsonProperty("city")]
        public string City { get; set; } = "";

        /// <summary>学校。</summary>
        [JsonProperty("school")]
        public string School { get; set; } = "";
    }

    /// <summary>
    /// Milky 好友分组实体。
    /// </summary>
    public sealed class MilkyFriendCategoryEntity
    {
        /// <summary>好友分组 ID。</summary>
        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        /// <summary>好友分组名称。</summary>
        [JsonProperty("category_name")]
        public string CategoryName { get; set; } = "";
    }

    /// <summary>
    /// Milky 好友实体。
    /// </summary>
    public sealed class MilkyFriendEntity
    {
        /// <summary>用户 QQ 号。</summary>
        [JsonProperty("user_id")]
        public long UserId { get; set; }

        /// <summary>用户昵称。</summary>
        [JsonProperty("nickname")]
        public string Nickname { get; set; } = "";

        /// <summary>用户性别，可能为 male、female、unknown。</summary>
        [JsonProperty("sex")]
        public string Sex { get; set; } = "unknown";

        /// <summary>用户 QID。</summary>
        [JsonProperty("qid")]
        public string Qid { get; set; } = "";

        /// <summary>好友备注。</summary>
        [JsonProperty("remark")]
        public string Remark { get; set; } = "";

        /// <summary>好友分组。</summary>
        [JsonProperty("category")]
        public MilkyFriendCategoryEntity Category { get; set; } = new();
    }

    /// <summary>
    /// Milky 群实体。
    /// </summary>
    public sealed class MilkyGroupEntity
    {
        /// <summary>群号。</summary>
        [JsonProperty("group_id")]
        public long GroupId { get; set; }

        /// <summary>群名称。</summary>
        [JsonProperty("group_name")]
        public string GroupName { get; set; } = "";

        /// <summary>群成员数量。</summary>
        [JsonProperty("member_count")]
        public int MemberCount { get; set; }

        /// <summary>群容量。</summary>
        [JsonProperty("max_member_count")]
        public int MaxMemberCount { get; set; }

        /// <summary>群备注。</summary>
        [JsonProperty("remark")]
        public string Remark { get; set; } = "";

        /// <summary>群创建时间，Unix 时间戳（秒）。</summary>
        [JsonProperty("created_time")]
        public long CreatedTime { get; set; }

        /// <summary>群简介。</summary>
        [JsonProperty("description")]
        public string Description { get; set; } = "";

        /// <summary>加群验证问题。</summary>
        [JsonProperty("question")]
        public string Question { get; set; } = "";

        /// <summary>群公告预览。</summary>
        [JsonProperty("announcement")]
        public string Announcement { get; set; } = "";
    }

    /// <summary>
    /// Milky 群成员实体。
    /// </summary>
    public sealed class MilkyGroupMemberEntity
    {
        /// <summary>用户 QQ 号。</summary>
        [JsonProperty("user_id")]
        public long UserId { get; set; }

        /// <summary>用户昵称。</summary>
        [JsonProperty("nickname")]
        public string Nickname { get; set; } = "";

        /// <summary>用户性别，可能为 male、female、unknown。</summary>
        [JsonProperty("sex")]
        public string Sex { get; set; } = "unknown";

        /// <summary>群号。</summary>
        [JsonProperty("group_id")]
        public long GroupId { get; set; }

        /// <summary>成员备注。</summary>
        [JsonProperty("card")]
        public string Card { get; set; } = "";

        /// <summary>专属头衔。</summary>
        [JsonProperty("title")]
        public string Title { get; set; } = "";

        /// <summary>群等级。</summary>
        [JsonProperty("level")]
        public int Level { get; set; }

        /// <summary>权限等级，可能为 owner、admin、member。</summary>
        [JsonProperty("role")]
        public string Role { get; set; } = "member";

        /// <summary>入群时间，Unix 时间戳（秒）。</summary>
        [JsonProperty("join_time")]
        public long JoinTime { get; set; }

        /// <summary>最后发言时间，Unix 时间戳（秒）。</summary>
        [JsonProperty("last_sent_time")]
        public long LastSentTime { get; set; }

        /// <summary>禁言结束时间，Unix 时间戳（秒）。</summary>
        [JsonProperty("shut_up_end_time")]
        public long? ShutUpEndTime { get; set; }
    }

    /// <summary>
    /// Milky 置顶会话列表。
    /// </summary>
    public sealed class MilkyPeerPins
    {
        /// <summary>置顶好友列表。</summary>
        [JsonProperty("friends")]
        public List<MilkyFriendEntity> Friends { get; set; } = new();

        /// <summary>置顶群列表。</summary>
        [JsonProperty("groups")]
        public List<MilkyGroupEntity> Groups { get; set; } = new();
    }

    /// <summary>
    /// Milky 群公告实体。
    /// </summary>
    public sealed class MilkyGroupAnnouncementEntity
    {
        /// <summary>群号。</summary>
        [JsonProperty("group_id")]
        public long GroupId { get; set; }

        /// <summary>公告 ID。</summary>
        [JsonProperty("announcement_id")]
        public string AnnouncementId { get; set; } = "";

        /// <summary>发送者 QQ 号。</summary>
        [JsonProperty("user_id")]
        public long UserId { get; set; }

        /// <summary>Unix 时间戳（秒）。</summary>
        [JsonProperty("time")]
        public long Time { get; set; }

        /// <summary>公告内容。</summary>
        [JsonProperty("content")]
        public string Content { get; set; } = "";

        /// <summary>公告图片 URL。</summary>
        [JsonProperty("image_url")]
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// Milky 接收消息段。
    /// </summary>
    public sealed class MilkyIncomingSegment
    {
        /// <summary>消息段类型。</summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "";

        /// <summary>消息段数据。</summary>
        [JsonProperty("data")]
        public JObject Data { get; set; } = new();
    }

    /// <summary>
    /// Milky 群精华消息。
    /// </summary>
    public sealed class MilkyGroupEssenceMessage
    {
        /// <summary>群号。</summary>
        [JsonProperty("group_id")]
        public long GroupId { get; set; }

        /// <summary>消息序列号。</summary>
        [JsonProperty("message_seq")]
        public long MessageSeq { get; set; }

        /// <summary>消息发送时间，Unix 时间戳（秒）。</summary>
        [JsonProperty("message_time")]
        public long MessageTime { get; set; }

        /// <summary>发送者 QQ 号。</summary>
        [JsonProperty("sender_id")]
        public long SenderId { get; set; }

        /// <summary>发送者名称。</summary>
        [JsonProperty("sender_name")]
        public string SenderName { get; set; } = "";

        /// <summary>设置精华的操作者 QQ 号。</summary>
        [JsonProperty("operator_id")]
        public long OperatorId { get; set; }

        /// <summary>设置精华的操作者名称。</summary>
        [JsonProperty("operator_name")]
        public string OperatorName { get; set; } = "";

        /// <summary>设置精华时间，Unix 时间戳（秒）。</summary>
        [JsonProperty("operation_time")]
        public long OperationTime { get; set; }

        /// <summary>消息段列表。</summary>
        [JsonProperty("segments")]
        public List<MilkyIncomingSegment> Segments { get; set; } = new();
    }

    /// <summary>
    /// Milky 群精华消息分页结果。
    /// </summary>
    public sealed class MilkyGroupEssenceMessagesResult
    {
        /// <summary>精华消息列表。</summary>
        [JsonProperty("messages")]
        public List<MilkyGroupEssenceMessage> Messages { get; set; } = new();

        /// <summary>是否已到最后一页。</summary>
        [JsonProperty("is_end")]
        public bool IsEnd { get; set; }
    }

    /// <summary>
    /// Milky 群通知实体。
    /// </summary>
    public sealed class MilkyGroupNotification
    {
        /// <summary>通知类型。</summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "";

        /// <summary>群号。</summary>
        [JsonProperty("group_id")]
        public long GroupId { get; set; }

        /// <summary>通知序列号。</summary>
        [JsonProperty("notification_seq")]
        public long NotificationSeq { get; set; }

        /// <summary>请求是否被过滤。</summary>
        [JsonProperty("is_filtered")]
        public bool IsFiltered { get; set; }

        /// <summary>发起者或邀请者 QQ 号。</summary>
        [JsonProperty("initiator_id")]
        public long? InitiatorId { get; set; }

        /// <summary>被操作用户 QQ 号。</summary>
        [JsonProperty("target_user_id")]
        public long? TargetUserId { get; set; }

        /// <summary>处理状态。</summary>
        [JsonProperty("state")]
        public string State { get; set; } = "";

        /// <summary>操作者 QQ 号。</summary>
        [JsonProperty("operator_id")]
        public long? OperatorId { get; set; }

        /// <summary>入群请求附加信息。</summary>
        [JsonProperty("comment")]
        public string Comment { get; set; } = "";

        /// <summary>是否设置为管理员。</summary>
        [JsonProperty("is_set")]
        public bool? IsSet { get; set; }
    }

    /// <summary>
    /// Milky 群通知分页结果。
    /// </summary>
    public sealed class MilkyGroupNotificationsResult
    {
        /// <summary>群通知列表。</summary>
        [JsonProperty("notifications")]
        public List<MilkyGroupNotification> Notifications { get; set; } = new();

        /// <summary>下一页起始通知序列号。</summary>
        [JsonProperty("next_notification_seq")]
        public long? NextNotificationSeq { get; set; }
    }
}
