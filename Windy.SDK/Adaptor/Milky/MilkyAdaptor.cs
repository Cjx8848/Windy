using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windy.SDK.Events;

namespace Windy.SDK.Adaptor.Milky
{
    public sealed partial class MilkyAdaptor : Adaptor
    {
        private readonly HttpClient httpClient = new();
        private ClientWebSocket? webSocket;
        private CancellationTokenSource? cancellationTokenSource;
        private Task? receiveTask;

        public MilkyAdaptor(AdaptorConfig config) : base("milky", AdaptorType.Milky, config)
        {
            MilkyEndpoint endpoint = MilkyEndpoint.FromConfig(config);
            EventUrl = endpoint.EventUrl;
            ApiBaseUrl = endpoint.ApiBaseUrl;
            AccessToken = config.Get("AccessToken");
            ReconnectInterval = int.TryParse(config.Get("ReconnectInterval"), out int interval) ? interval : 5000;
            DropSelfMessage = config.GetBool("DropSelfMessage", true);
        }

        public string EventUrl { get; }

        public string ApiBaseUrl { get; }

        public string AccessToken { get; }

        public int ReconnectInterval { get; }

        public bool DropSelfMessage { get; }

        public string? SelfId { get; private set; }

        public override AdaptorCapabilities Capabilities => AdaptorCapabilities.Text |
            AdaptorCapabilities.Image |
            AdaptorCapabilities.File |
            AdaptorCapabilities.DirectMessage |
            AdaptorCapabilities.GroupMessage;

        /// <summary>
        /// 启动 Milky WebSocket 事件连接。
        /// </summary>
        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (!string.IsNullOrWhiteSpace(AccessToken))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            }

            try
            {
                await ConnectWebSocketAsync(cancellationTokenSource.Token);
                Message.Blue($"Milky 适配器已启动: {EventUrl}");
            }
            catch (Exception ex)
            {
                IsActive = false;
                Message.Red($"Milky 适配器连接失败，已进入 inactive 状态并等待重连: {ex.Message}");
                _ = ReconnectLoopAsync(cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// 停止 Milky WebSocket 事件连接。
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationTokenSource?.Cancel();
            if (webSocket?.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "stop", cancellationToken);
            }

            webSocket?.Dispose();
            webSocket = null;
            IsActive = false;
        }

        /// <summary>
        /// 发送 Milky 群消息或私聊消息。
        /// </summary>
        public override async Task SendMessage(SendTarget target, MessageContent content, SendOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (!EnsureActive("发送 Milky 消息"))
            {
                return;
            }

            if (target.Type is not SendTargetType.Group and not SendTargetType.User)
            {
                throw new NotSupportedException("Milky 当前仅支持群消息和私聊消息.");
            }

            long id = ParseId(target.Id, "发送目标 ID");
            List<JObject> messageSegments = new();
            foreach (MessageSegment segment in content.Segments)
            {
                if (segment is FileSegment file)
                {
                    await UploadFileAsync(target, id, file, cancellationToken);
                    continue;
                }

                JObject? milkySegment = await ConvertSegmentAsync(segment, cancellationToken);
                if (milkySegment != null)
                {
                    messageSegments.Add(milkySegment);
                }
            }

            if (messageSegments.Count == 0)
            {
                return;
            }

            string action = target.Type == SendTargetType.Group ? "send_group_message" : "send_private_message";
            JObject payload = target.Type == SendTargetType.Group
                ? new JObject { ["group_id"] = id, ["message"] = new JArray(messageSegments) }
                : new JObject { ["user_id"] = id, ["message"] = new JArray(messageSegments) };

            await CallApiAsync(action, payload, cancellationToken);
        }

        /// <summary>
        /// 上传群文件。
        /// </summary>
        public Task UploadGroupFileAsync(string groupId, string fileUri, string fileName, string parentFolderId = "/", CancellationToken cancellationToken = default)
        {
            if (!EnsureActive("上传 Milky 群文件"))
            {
                return Task.CompletedTask;
            }

            return CallApiAsync("upload_group_file", new JObject
            {
                ["group_id"] = ParseId(groupId, nameof(groupId)),
                ["file_uri"] = fileUri,
                ["file_name"] = fileName,
                ["parent_folder_id"] = parentFolderId,
            }, cancellationToken);
        }

        /// <summary>
        /// 上传私聊文件。
        /// </summary>
        public Task UploadPrivateFileAsync(string userId, string fileUri, string fileName, CancellationToken cancellationToken = default)
        {
            if (!EnsureActive("上传 Milky 私聊文件"))
            {
                return Task.CompletedTask;
            }

            return CallApiAsync("upload_private_file", new JObject
            {
                ["user_id"] = ParseId(userId, nameof(userId)),
                ["file_uri"] = fileUri,
                ["file_name"] = fileName,
            }, cancellationToken);
        }

        /// <summary>
        /// 获取当前登录账号信息。
        /// </summary>
        public Task<MilkyLoginInfo> GetLoginInfoAsync(CancellationToken cancellationToken = default)
        {
            return CallDataApiAsync<MilkyLoginInfo>("get_login_info", new JObject(), cancellationToken);
        }

        /// <summary>
        /// 获取 Milky 协议端实现信息。
        /// </summary>
        public Task<MilkyImplInfo> GetImplInfoAsync(CancellationToken cancellationToken = default)
        {
            return CallDataApiAsync<MilkyImplInfo>("get_impl_info", new JObject(), cancellationToken);
        }

        /// <summary>
        /// 获取指定用户个人资料。
        /// </summary>
        public Task<MilkyUserProfile> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default)
        {
            return CallDataApiAsync<MilkyUserProfile>("get_user_profile", new JObject { ["user_id"] = ParseId(userId, nameof(userId)) }, cancellationToken);
        }

        /// <summary>
        /// 获取好友列表。
        /// </summary>
        public Task<IReadOnlyList<MilkyFriendEntity>> GetFriendListAsync(bool noCache = false, CancellationToken cancellationToken = default)
        {
            return CallDataListApiAsync<MilkyFriendEntity>("get_friend_list", new JObject { ["no_cache"] = noCache }, "friends", cancellationToken);
        }

        /// <summary>
        /// 获取指定好友信息。
        /// </summary>
        public Task<MilkyFriendEntity> GetFriendInfoAsync(string userId, bool noCache = false, CancellationToken cancellationToken = default)
        {
            return CallDataApiAsync<MilkyFriendEntity>("get_friend_info", new JObject { ["user_id"] = ParseId(userId, nameof(userId)), ["no_cache"] = noCache }, cancellationToken, "friend");
        }

        /// <summary>
        /// 获取群列表。
        /// </summary>
        public Task<IReadOnlyList<MilkyGroupEntity>> GetGroupListAsync(bool noCache = false, CancellationToken cancellationToken = default)
        {
            return CallDataListApiAsync<MilkyGroupEntity>("get_group_list", new JObject { ["no_cache"] = noCache }, "groups", cancellationToken);
        }

        /// <summary>
        /// 获取指定群信息。
        /// </summary>
        public Task<MilkyGroupEntity> GetGroupInfoAsync(string groupId, bool noCache = false, CancellationToken cancellationToken = default)
        {
            return CallDataApiAsync<MilkyGroupEntity>("get_group_info", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["no_cache"] = noCache }, cancellationToken, "group");
        }

        /// <summary>
        /// 获取群成员列表。
        /// </summary>
        public Task<IReadOnlyList<MilkyGroupMemberEntity>> GetGroupMemberListAsync(string groupId, bool noCache = false, CancellationToken cancellationToken = default)
        {
            return CallDataListApiAsync<MilkyGroupMemberEntity>("get_group_member_list", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["no_cache"] = noCache }, "members", cancellationToken);
        }

        /// <summary>
        /// 获取指定群成员信息。
        /// </summary>
        public Task<MilkyGroupMemberEntity> GetGroupMemberInfoAsync(string groupId, string userId, bool noCache = false, CancellationToken cancellationToken = default)
        {
            return CallDataApiAsync<MilkyGroupMemberEntity>("get_group_member_info", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["user_id"] = ParseId(userId, nameof(userId)), ["no_cache"] = noCache }, cancellationToken, "member");
        }

        /// <summary>
        /// 获取置顶好友和置顶群列表。
        /// </summary>
        public Task<MilkyPeerPins> GetPeerPinsAsync(CancellationToken cancellationToken = default)
        {
            return CallDataApiAsync<MilkyPeerPins>("get_peer_pins", new JObject(), cancellationToken);
        }

        /// <summary>
        /// 设置好友、群或临时会话置顶状态。
        /// </summary>
        public Task SetPeerPinAsync(string messageScene, string peerId, bool isPinned = true, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("set_peer_pin", new JObject { ["message_scene"] = messageScene, ["peer_id"] = ParseId(peerId, nameof(peerId)), ["is_pinned"] = isPinned }, cancellationToken);
        }

        /// <summary>
        /// 设置当前 QQ 账号头像。
        /// </summary>
        public Task SetAvatarAsync(string uri, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("set_avatar", new JObject { ["uri"] = uri }, cancellationToken);
        }

        /// <summary>
        /// 设置当前 QQ 账号昵称。
        /// </summary>
        public Task SetNicknameAsync(string newNickname, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("set_nickname", new JObject { ["new_nickname"] = newNickname }, cancellationToken);
        }

        /// <summary>
        /// 设置当前 QQ 账号个性签名。
        /// </summary>
        public Task SetBioAsync(string newBio, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("set_bio", new JObject { ["new_bio"] = newBio }, cancellationToken);
        }

        /// <summary>
        /// 获取自定义表情 URL 列表。
        /// </summary>
        public Task<IReadOnlyList<string>> GetCustomFaceUrlListAsync(CancellationToken cancellationToken = default)
        {
            return CallDataListApiAsync<string>("get_custom_face_url_list", new JObject(), "urls", cancellationToken);
        }

        /// <summary>
        /// 获取指定域名的 Cookies 字符串。
        /// </summary>
        public Task<string> GetCookiesAsync(string domain, CancellationToken cancellationToken = default)
        {
            return CallDataStringApiAsync("get_cookies", new JObject { ["domain"] = domain }, "cookies", cancellationToken);
        }

        /// <summary>
        /// 获取 CSRF Token。
        /// </summary>
        public Task<string> GetCsrfTokenAsync(CancellationToken cancellationToken = default)
        {
            return CallDataStringApiAsync("get_csrf_token", new JObject(), "csrf_token", cancellationToken);
        }

        /// <summary>
        /// 设置群名称。
        /// </summary>
        public Task SetGroupNameAsync(string groupId, string newGroupName, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("set_group_name", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["new_group_name"] = newGroupName }, cancellationToken);
        }

        /// <summary>
        /// 设置群头像。
        /// </summary>
        public Task SetGroupAvatarAsync(string groupId, string imageUri, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("set_group_avatar", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["image_uri"] = imageUri }, cancellationToken);
        }

        /// <summary>
        /// 设置群成员名片。
        /// </summary>
        public Task SetGroupMemberCardAsync(string groupId, string userId, string card, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("set_group_member_card", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["user_id"] = ParseId(userId, nameof(userId)), ["card"] = card }, cancellationToken);
        }

        /// <summary>
        /// 设置群成员专属头衔。
        /// </summary>
        public Task SetGroupMemberSpecialTitleAsync(string groupId, string userId, string specialTitle, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("set_group_member_special_title", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["user_id"] = ParseId(userId, nameof(userId)), ["special_title"] = specialTitle }, cancellationToken);
        }

        /// <summary>
        /// 设置或取消群管理员。
        /// </summary>
        public Task SetGroupMemberAdminAsync(string groupId, string userId, bool isSet = true, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("set_group_member_admin", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["user_id"] = ParseId(userId, nameof(userId)), ["is_set"] = isSet }, cancellationToken);
        }

        /// <summary>
        /// 设置或取消群成员禁言。
        /// </summary>
        public Task SetGroupMemberMuteAsync(string groupId, string userId, int duration = 0, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("set_group_member_mute", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["user_id"] = ParseId(userId, nameof(userId)), ["duration"] = duration }, cancellationToken);
        }

        /// <summary>
        /// 设置或取消群全员禁言。
        /// </summary>
        public Task SetGroupWholeMuteAsync(string groupId, bool isMute = true, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("set_group_whole_mute", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["is_mute"] = isMute }, cancellationToken);
        }

        /// <summary>
        /// 踢出群成员。
        /// </summary>
        public Task KickGroupMemberAsync(string groupId, string userId, bool rejectAddRequest = false, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("kick_group_member", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["user_id"] = ParseId(userId, nameof(userId)), ["reject_add_request"] = rejectAddRequest }, cancellationToken);
        }

        /// <summary>
        /// 获取群公告列表。
        /// </summary>
        public Task<IReadOnlyList<MilkyGroupAnnouncementEntity>> GetGroupAnnouncementsAsync(string groupId, CancellationToken cancellationToken = default)
        {
            return CallDataListApiAsync<MilkyGroupAnnouncementEntity>("get_group_announcements", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)) }, "announcements", cancellationToken);
        }

        /// <summary>
        /// 发送群公告。
        /// </summary>
        public Task SendGroupAnnouncementAsync(string groupId, string content, string? imageUri = null, CancellationToken cancellationToken = default)
        {
            JObject payload = new() { ["group_id"] = ParseId(groupId, nameof(groupId)), ["content"] = content, ["image_uri"] = imageUri };
            payload.RemoveNullValues();
            return CallApiAsync("send_group_announcement", payload, cancellationToken);
        }

        /// <summary>
        /// 删除群公告。
        /// </summary>
        public Task DeleteGroupAnnouncementAsync(string groupId, string announcementId, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("delete_group_announcement", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["announcement_id"] = announcementId }, cancellationToken);
        }

        /// <summary>
        /// 获取群精华消息列表。
        /// </summary>
        public Task<MilkyGroupEssenceMessagesResult> GetGroupEssenceMessagesAsync(string groupId, int pageIndex = 0, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            return CallDataApiAsync<MilkyGroupEssenceMessagesResult>("get_group_essence_messages", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["page_index"] = pageIndex, ["page_size"] = pageSize }, cancellationToken);
        }

        /// <summary>
        /// 设置或取消群精华消息。
        /// </summary>
        public Task SetGroupEssenceMessageAsync(string groupId, string messageSeq, bool isSet = true, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("set_group_essence_message", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["message_seq"] = ParseId(messageSeq, nameof(messageSeq)), ["is_set"] = isSet }, cancellationToken);
        }

        /// <summary>
        /// 退出群聊。
        /// </summary>
        public Task QuitGroupAsync(string groupId, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("quit_group", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)) }, cancellationToken);
        }

        /// <summary>
        /// 发送或取消群消息表情回应。
        /// </summary>
        public Task SendGroupMessageReactionAsync(string groupId, string messageSeq, string reaction, string reactionType = "face", bool isAdd = true, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("send_group_message_reaction", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["message_seq"] = ParseId(messageSeq, nameof(messageSeq)), ["reaction"] = reaction, ["reaction_type"] = reactionType, ["is_add"] = isAdd }, cancellationToken);
        }

        /// <summary>
        /// 发送群戳一戳。
        /// </summary>
        public Task SendGroupNudgeAsync(string groupId, string userId, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("send_group_nudge", new JObject { ["group_id"] = ParseId(groupId, nameof(groupId)), ["user_id"] = ParseId(userId, nameof(userId)) }, cancellationToken);
        }

        /// <summary>
        /// 获取群通知列表。
        /// </summary>
        public Task<MilkyGroupNotificationsResult> GetGroupNotificationsAsync(long? startNotificationSeq = null, bool isFiltered = false, int limit = 20, CancellationToken cancellationToken = default)
        {
            JObject payload = new() { ["start_notification_seq"] = startNotificationSeq, ["is_filtered"] = isFiltered, ["limit"] = limit };
            payload.RemoveNullValues();
            return CallDataApiAsync<MilkyGroupNotificationsResult>("get_group_notifications", payload, cancellationToken);
        }

        private async Task ConnectWebSocketAsync(CancellationToken cancellationToken)
        {
            webSocket?.Dispose();
            webSocket = new ClientWebSocket();
            if (!string.IsNullOrWhiteSpace(AccessToken))
            {
                webSocket.Options.SetRequestHeader("Authorization", "Bearer " + AccessToken);
            }

            await webSocket.ConnectAsync(new Uri(EventUrl), cancellationToken);
            IsActive = true;
            receiveTask = ReceiveLoopAsync(webSocket, cancellationToken);
        }

        /// <summary>
        /// 同意好友请求。
        /// </summary>
        public Task AcceptFriendRequestAsync(string initiatorUid, bool isFiltered = false, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("accept_friend_request", new JObject { ["initiator_uid"] = initiatorUid, ["is_filtered"] = isFiltered }, cancellationToken);
        }

        /// <summary>
        /// 拒绝好友请求。
        /// </summary>
        public Task RejectFriendRequestAsync(string initiatorUid, string reason = "", bool isFiltered = false, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("reject_friend_request", new JObject { ["initiator_uid"] = initiatorUid, ["reason"] = reason, ["is_filtered"] = isFiltered }, cancellationToken);
        }

        /// <summary>
        /// 同意入群请求或成员邀请他人入群请求。
        /// </summary>
        public Task AcceptGroupRequestAsync(long groupId, long notificationSeq, string notificationType, bool isFiltered = false, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("accept_group_request", new JObject { ["group_id"] = groupId, ["notification_seq"] = notificationSeq, ["notification_type"] = notificationType, ["is_filtered"] = isFiltered }, cancellationToken);
        }

        /// <summary>
        /// 拒绝入群请求或成员邀请他人入群请求。
        /// </summary>
        public Task RejectGroupRequestAsync(long groupId, long notificationSeq, string notificationType, string reason = "", bool isFiltered = false, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("reject_group_request", new JObject { ["group_id"] = groupId, ["notification_seq"] = notificationSeq, ["notification_type"] = notificationType, ["reason"] = reason, ["is_filtered"] = isFiltered }, cancellationToken);
        }

        /// <summary>
        /// 同意他人邀请自身入群。
        /// </summary>
        public Task AcceptGroupInvitationAsync(long groupId, long invitationSeq, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("accept_group_invitation", new JObject { ["group_id"] = groupId, ["invitation_seq"] = invitationSeq }, cancellationToken);
        }

        /// <summary>
        /// 拒绝他人邀请自身入群。
        /// </summary>
        public Task RejectGroupInvitationAsync(long groupId, long invitationSeq, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("reject_group_invitation", new JObject { ["group_id"] = groupId, ["invitation_seq"] = invitationSeq }, cancellationToken);
        }

        private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1024 * 64];
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                try
                {
                    using MemoryStream stream = new();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, cancellationToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            return;
                        }

                        stream.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    string text = Encoding.UTF8.GetString(stream.ToArray());
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        ProcessEvent(JObject.Parse(text));
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Message.Yellow($"Milky WebSocket 连接异常: {ex.Message}");
                    break;
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                IsActive = false;
                await ReconnectLoopAsync(cancellationToken);
            }
        }

        private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Message.Yellow($"Milky 将在 {ReconnectInterval}ms 后重连.");
                    await Task.Delay(ReconnectInterval, cancellationToken);
                    await ConnectWebSocketAsync(cancellationToken);
                    Message.Green("Milky WebSocket 重连成功.");
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Message.Red($"Milky WebSocket 重连失败: {ex.Message}");
                }
            }
        }

        private void ProcessEvent(JObject envelope)
        {
            string eventType = envelope.Value<string>("event_type") ?? "";
            if (envelope["self_id"] != null)
            {
                SelfId = envelope.Value<string>("self_id");
            }

            if (eventType == "bot_offline")
            {
                IsActive = false;
            }

            if (eventType == "group_member_decrease" && envelope["data"] is JObject decreaseData)
            {
                string userId = decreaseData.Value<string>("user_id") ?? "";
                if (!string.IsNullOrWhiteSpace(SelfId) && userId == SelfId)
                {
                    JObject kickedEvent = new(envelope);
                    kickedEvent["event_type"] = "bot_kicked";
                    PublishEvent(new AdaptorEventArgs(this, "bot_kicked", kickedEvent));
                    return;
                }
            }

            if (eventType == "message_receive" && envelope["data"] is JObject data)
            {
                MessageEventArgs? message = CreateMessageEvent(data, envelope);
                if (message != null)
                {
                    PublishMessage(message);
                }

                return;
            }

            PublishEvent(new AdaptorEventArgs(this, eventType, envelope));
        }

        private MessageEventArgs? CreateMessageEvent(JObject data, JObject envelope)
        {
            string scene = data.Value<string>("message_scene") ?? "";
            string peerId = data.Value<string>("peer_id") ?? "";
            string senderId = data.Value<string>("sender_id") ?? "";
            if (DropSelfMessage && !string.IsNullOrWhiteSpace(SelfId) && senderId == SelfId)
            {
                return null;
            }

            MessageScene messageScene = scene == "group" ? MessageScene.Group : MessageScene.Private;
            SendTarget replyTarget = messageScene == MessageScene.Group ? SendTarget.Group(peerId) : SendTarget.User(senderId);
            string authorName = ExtractAuthorName(data, messageScene);
            MessageEventArgs args = new(this, messageScene, ExtractPlainText(data), data.Value<string>("message_seq") ?? "", senderId, replyTarget, authorName)
            {
                GroupId = messageScene == MessageScene.Group ? peerId : null,
                Raw = envelope,
            };

            if (messageScene == MessageScene.Group)
            {
                args.Role = ParseRole(data["group_member"]?.Value<string>("role") ?? "");
            }

            AddAttachments(args, data);
            Message.Yellow(messageScene == MessageScene.Group
                ? $"[Milky][{args.GroupId}]{args.AuthorName}({args.Role}):{args.Content}"
                : $"[Milky][{args.AuthorId}]{args.AuthorName}:{args.Content}");
            return args;
        }

        private Task<JObject?> ConvertSegmentAsync(MessageSegment segment, CancellationToken cancellationToken)
        {
            return Task.FromResult(segment switch
            {
                TextSegment text => Segment("text", new JObject { ["text"] = text.Text }),
                MentionSegment mention => ConvertMentionSegment(mention),
                MentionAllSegment => Segment("mention_all", new JObject()),
                ImageUrlSegment image => Segment("image", new JObject { ["uri"] = image.Url }),
                ImageSegment image => Segment("image", new JObject { ["uri"] = ToBase64Uri(image.Data) }),
                AudioSegment audio => Segment("record", new JObject { ["uri"] = audio.Uri }),
                VideoSegment video => Segment("video", new JObject { ["uri"] = video.Uri, ["thumb_uri"] = video.ThumbnailUri }),
                MarkdownSegment markdown => Segment("text", new JObject { ["text"] = markdown.Markdown }),
                MarkdownTemplateSegment markdown => Segment("text", new JObject { ["text"] = $"Milky 不支持 Markdown 模板消息: {markdown.TemplateId}" }),
                ButtonSegment => SkipUnsupported("Milky 不支持 QQ 官方按钮消息."),
                FaceSegment face => Segment("face", new JObject { ["face_id"] = face.FaceId, ["is_large"] = face.IsLarge }),
                ReplySegment reply => Segment("reply", new JObject { ["message_seq"] = ParseId(reply.MessageSeq, nameof(reply.MessageSeq)) }),
                ForwardSegment forward => ConvertForwardSegment(forward),
                LightAppSegment lightApp => Segment("light_app", new JObject { ["json_payload"] = lightApp.JsonPayload }),
                _ => SkipUnsupported($"Milky 不支持消息段: {segment.GetType().Name}"),
            });
        }

        private async Task UploadFileAsync(SendTarget target, long id, FileSegment file, CancellationToken cancellationToken)
        {
            string action = target.Type == SendTargetType.Group ? "upload_group_file" : "upload_private_file";
            JObject payload = target.Type == SendTargetType.Group
                ? new JObject { ["group_id"] = id, ["file_uri"] = file.Uri, ["file_name"] = file.FileName, ["parent_folder_id"] = file.ParentFolderId }
                : new JObject { ["user_id"] = id, ["file_uri"] = file.Uri, ["file_name"] = file.FileName };
            await CallApiAsync(action, payload, cancellationToken);
        }

        private async Task<JObject> CallApiAsync(string action, JObject payload, CancellationToken cancellationToken)
        {
            try
            {
                using StringContent content = new(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await httpClient.PostAsync($"{ApiBaseUrl}/{action}", content, cancellationToken);
                string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    Message.Red($"Milky API [{action}] 请求失败: {(int)response.StatusCode} {responseText}");
                    return new JObject();
                }

                JObject result = string.IsNullOrWhiteSpace(responseText) ? new JObject() : JObject.Parse(responseText);
                int retCode = result.Value<int?>("retcode") ?? result.Value<int?>("retCode") ?? 0;
                string status = result.Value<string>("status") ?? "ok";
                if (retCode != 0 || status.Equals("failed", StringComparison.OrdinalIgnoreCase))
                {
                    Message.Red($"Milky API [{action}] 返回失败: {responseText}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Message.Red($"Milky API [{action}] 调用异常: {ex.Message}");
                return new JObject();
            }
        }

        private async Task<T> CallDataApiAsync<T>(string action, JObject payload, CancellationToken cancellationToken, string? propertyName = null) where T : new()
        {
            JObject result = await CallApiAsync(action, payload, cancellationToken);
            JToken? data = result["data"];
            JToken? token = string.IsNullOrWhiteSpace(propertyName) ? data : data?[propertyName];
            return token == null || token.Type == JTokenType.Null ? new T() : token.ToObject<T>() ?? new T();
        }

        private async Task<IReadOnlyList<T>> CallDataListApiAsync<T>(string action, JObject payload, string propertyName, CancellationToken cancellationToken)
        {
            JObject result = await CallApiAsync(action, payload, cancellationToken);
            JToken? token = result["data"]?[propertyName];
            return token == null || token.Type == JTokenType.Null ? [] : token.ToObject<List<T>>() ?? [];
        }

        private async Task<string> CallDataStringApiAsync(string action, JObject payload, string propertyName, CancellationToken cancellationToken)
        {
            JObject result = await CallApiAsync(action, payload, cancellationToken);
            return result["data"]?.Value<string>(propertyName) ?? "";
        }

        private static JObject? ConvertForwardSegment(ForwardSegment forward)
        {
            if (forward.Messages.Count == 0)
            {
                return null;
            }

            JArray messages = new(forward.Messages.Select(node =>
            {
                JArray segs = new();
                foreach (MessageSegment seg in node.Message.Segments)
                {
                    JObject? converted = ConvertSegmentStatic(seg);
                    if (converted != null)
                    {
                        segs.Add(converted);
                    }
                }

                return (JToken)new JObject
                {
                    ["user_id"] = ParseId(node.UserId, nameof(node.UserId)),
                    ["sender_name"] = node.SenderName,
                    ["segments"] = segs,
                };
            }));

            JObject data = new()
            {
                ["messages"] = messages,
            };

            if (!string.IsNullOrWhiteSpace(forward.Title))
            {
                data["title"] = forward.Title;
            }

            if (!string.IsNullOrWhiteSpace(forward.Summary))
            {
                data["summary"] = forward.Summary;
            }

            return Segment("forward", data);
        }

        private static JObject? ConvertSegmentStatic(MessageSegment segment)
        {
            return segment switch
            {
                TextSegment text => Segment("text", new JObject { ["text"] = text.Text }),
                MentionSegment mention => ConvertMentionSegment(mention),
                MentionAllSegment => Segment("mention_all", new JObject()),
                ImageUrlSegment image => Segment("image", new JObject { ["uri"] = image.Url }),
                ImageSegment image => Segment("image", new JObject { ["uri"] = ToBase64Uri(image.Data) }),
                FaceSegment face => Segment("face", new JObject { ["face_id"] = face.FaceId, ["is_large"] = face.IsLarge }),
                _ => null,
            };
        }

        private static JObject Segment(string type, JObject data)
        {
            data.RemoveNullValues();
            return new JObject { ["type"] = type, ["data"] = data };
        }

        private static JObject? ConvertMentionSegment(MentionSegment mention)
        {
            if (long.TryParse(mention.UserId, out long qq))
                return Segment("mention", new JObject { ["user_id"] = qq });

            return Segment("text", new JObject { ["text"] = $"<@{mention.UserId}>" });
        }

        private static JObject? SkipUnsupported(string message)
        {
            Message.Yellow(message);
            return null;
        }

        private static string ExtractPlainText(JObject data)
        {
            if (data["segments"] is not JArray segments)
            {
                return "";
            }

            StringBuilder builder = new();
            foreach (JObject segment in segments.OfType<JObject>())
            {
                string type = segment.Value<string>("type") ?? "";
                JObject segmentData = segment["data"] as JObject ?? new JObject();
                builder.Append(type switch
                {
                    "text" => segmentData.Value<string>("text") ?? "",
                    "mention" => $"<@{segmentData.Value<string>("user_id") ?? ""}>",
                    "mention_all" => "@全体成员",
                    "face" => $"[表情:{segmentData.Value<string>("face_id") ?? ""}]",
                    "image" => "[图片]",
                    "record" => "[语音]",
                    "video" => "[视频]",
                    "file" => $"[文件:{segmentData.Value<string>("file_name") ?? ""}]",
                    "reply" => "[回复消息]",
                    "forward" => "[转发消息]",
                    "light_app" => $"[小程序:{segmentData.Value<string>("app_name") ?? ""}]",
                    "market_face" => "[商城表情]",
                    "xml" => "[XML消息]",
                    _ => $"[{type}]",
                });
            }

            return builder.ToString();
        }

        private static void AddAttachments(MessageEventArgs args, JObject data)
        {
            if (data["segments"] is not JArray segments)
            {
                return;
            }

            foreach (JObject segment in segments.OfType<JObject>())
            {
                string type = segment.Value<string>("type") ?? "";
                JObject segmentData = segment["data"] as JObject ?? new JObject();
                if (type is not ("image" or "record" or "video" or "file"))
                {
                    continue;
                }

                args.Attachments.Add(new MessageAttachment
                {
                    ContentType = type,
                    FileName = segmentData.Value<string>("file_name") ?? segmentData.Value<string>("resource_id") ?? "",
                    Url = segmentData.Value<string>("temp_url") ?? segmentData.Value<string>("url") ?? "",
                });
            }
        }

        private static string ExtractAuthorName(JObject data, MessageScene scene)
        {
            string senderId = data.Value<string>("sender_id") ?? "";
            if (scene == MessageScene.Group)
            {
                JObject member = data["group_member"] as JObject ?? new JObject();
                return FirstNotEmpty(
                    member.Value<string>("card"),
                    member.Value<string>("nickname"),
                    member.Value<string>("title"),
                    senderId);
            }

            JObject friend = data["friend"] as JObject ?? new JObject();
            return FirstNotEmpty(
                friend.Value<string>("remark"),
                friend.Value<string>("nickname"),
                friend.Value<string>("qid"),
                senderId);
        }

        private static string FirstNotEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
        }

        private static GroupMemberRole ParseRole(string role)
        {
            return role.ToLowerInvariant() switch
            {
                "owner" => GroupMemberRole.Owner,
                "admin" => GroupMemberRole.Admin,
                _ => GroupMemberRole.Member,
            };
        }

        private static long ParseId(string value, string name)
        {
            return long.TryParse(value, out long id) ? id : throw new ArgumentException($"{name} 必须是数字 ID: {value}");
        }

        private static string ToBase64Uri(byte[] data)
        {
            return "base64://" + Convert.ToBase64String(data);
        }
    }

    internal sealed class MilkyEndpoint
    {
        private MilkyEndpoint(string eventUrl, string apiBaseUrl)
        {
            EventUrl = eventUrl;
            ApiBaseUrl = apiBaseUrl;
        }

        public string EventUrl { get; }

        public string ApiBaseUrl { get; }

        public static MilkyEndpoint FromConfig(AdaptorConfig config)
        {
            string endpoint = config.Get("Endpoint");
            if (!string.IsNullOrWhiteSpace(endpoint) && Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? endpointUri))
            {
                return FromEndpoint(endpointUri);
            }

            string host = config.Get("Host", "127.0.0.1");
            int port = int.TryParse(config.Get("Port"), out int parsedPort) ? parsedPort : 3000;
            string prefix = config.Get("Prefix").Trim('/');
            bool useTls = config.GetBool("UseTls");
            string httpScheme = useTls ? "https" : "http";
            string wsScheme = useTls ? "wss" : "ws";
            string prefixPath = string.IsNullOrWhiteSpace(prefix) ? "" : $"/{prefix}";
            return new MilkyEndpoint($"{wsScheme}://{host}:{port}{prefixPath}/event", $"{httpScheme}://{host}:{port}{prefixPath}/api");
        }

        private static MilkyEndpoint FromEndpoint(Uri endpoint)
        {
            string wsScheme = endpoint.Scheme is "https" or "wss" ? "wss" : "ws";
            string httpScheme = endpoint.Scheme is "https" or "wss" ? "https" : "http";
            string path = endpoint.AbsolutePath.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(path))
            {
                path = "/event";
            }
            else if (!path.EndsWith("/event", StringComparison.OrdinalIgnoreCase))
            {
                path += "/event";
            }

            string prefix = path[..^"/event".Length];
            string authority = endpoint.IsDefaultPort ? endpoint.Host : endpoint.Authority;
            return new MilkyEndpoint($"{wsScheme}://{authority}{path}", $"{httpScheme}://{authority}{prefix}/api");
        }
    }

    internal static class JObjectExtensions
    {
        public static void RemoveNullValues(this JObject value)
        {
            foreach (JProperty property in value.Properties().Where(property => property.Value.Type == JTokenType.Null).ToArray())
            {
                property.Remove();
            }
        }
    }
}
