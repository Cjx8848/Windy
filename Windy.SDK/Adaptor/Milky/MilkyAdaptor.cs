using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windy.SDK.Events;

namespace Windy.SDK.Adaptor.Milky
{
    public sealed class MilkyAdaptor : Adaptor
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

        public Task AcceptFriendRequestAsync(string initiatorUid, bool isFiltered = false, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("accept_friend_request", new JObject { ["initiator_uid"] = initiatorUid, ["is_filtered"] = isFiltered }, cancellationToken);
        }

        public Task RejectFriendRequestAsync(string initiatorUid, string reason = "", bool isFiltered = false, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("reject_friend_request", new JObject { ["initiator_uid"] = initiatorUid, ["reason"] = reason, ["is_filtered"] = isFiltered }, cancellationToken);
        }

        public Task AcceptGroupRequestAsync(long groupId, long notificationSeq, string notificationType, bool isFiltered = false, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("accept_group_request", new JObject { ["group_id"] = groupId, ["notification_seq"] = notificationSeq, ["notification_type"] = notificationType, ["is_filtered"] = isFiltered }, cancellationToken);
        }

        public Task RejectGroupRequestAsync(long groupId, long notificationSeq, string notificationType, string reason = "", bool isFiltered = false, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("reject_group_request", new JObject { ["group_id"] = groupId, ["notification_seq"] = notificationSeq, ["notification_type"] = notificationType, ["reason"] = reason, ["is_filtered"] = isFiltered }, cancellationToken);
        }

        public Task AcceptGroupInvitationAsync(long groupId, long invitationSeq, CancellationToken cancellationToken = default)
        {
            return CallApiAsync("accept_group_invitation", new JObject { ["group_id"] = groupId, ["invitation_seq"] = invitationSeq }, cancellationToken);
        }

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
                MentionSegment mention => Segment("mention", new JObject { ["user_id"] = ParseId(mention.UserId, nameof(mention.UserId)) }),
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
                MentionSegment mention => Segment("mention", new JObject { ["user_id"] = ParseId(mention.UserId, nameof(mention.UserId)) }),
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
