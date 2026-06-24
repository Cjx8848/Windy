using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windy.SDK.Events;

namespace Windy.SDK.Adaptor.QQOfficial
{
    public sealed partial class QQOfficialAdaptor : Adaptor
    {
        private readonly HttpClient httpClient = new();
        private readonly SemaphoreSlim reconnectLock = new(1, 1);
        private ClientWebSocket? webSocket;
        private CancellationTokenSource? cancellationTokenSource;
        private string accessToken = "";
        private string sessionId = "";
        private int sequenceNumber;
        private Task? receiveTask;
        private Task? heartbeatTask;

        public QQOfficialAdaptor(AdaptorConfig config) : base("qq-official", AdaptorType.QQOfficial, config)
        {
            AppId = config.Get("AppId");
            ClientSecret = config.Get("ClientSecret");
            Sandbox = config.GetBool("Sandbox");
            Intents = int.TryParse(config.Get("Intents"), out int intents) ? intents : (1 << 25) | (1 << 26) | (1 << 27) | (1 << 30);
        }

        public string AppId { get; }

        public string ClientSecret { get; }

        public bool Sandbox { get; }

        public int Intents { get; }

        public override AdaptorCapabilities Capabilities => AdaptorCapabilities.Text |
            AdaptorCapabilities.Image |
            AdaptorCapabilities.Markdown |
            AdaptorCapabilities.Button |
            AdaptorCapabilities.DirectMessage |
            AdaptorCapabilities.GroupMessage;

        private string ApiBase => Sandbox ? "https://sandbox.api.sgroup.qq.com" : "https://api.sgroup.qq.com";

        private string Gateway => Sandbox ? "wss://sandbox.api.sgroup.qq.com/websocket" : "wss://api.sgroup.qq.com/websocket";

        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(AppId) || string.IsNullOrWhiteSpace(ClientSecret))
            {
                Message.Yellow("QQ官方适配器缺少 AppId 或 ClientSecret，已跳过连接.");
                IsActive = false;
                return;
            }

            try
            {
                await RefreshAccessTokenAsync(cancellationToken);
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(new Uri(Gateway), cancellationTokenSource.Token);
                IsActive = true;
                await SendIdentifyAsync(cancellationTokenSource.Token);
                receiveTask = ReceiveLoopAsync(webSocket, cancellationTokenSource.Token);
                Message.Blue("QQ官方适配器已启动.");
            }
            catch (Exception ex)
            {
                IsActive = false;
                Message.Red($"QQ官方适配器连接失败，已进入 inactive 状态: {ex.Message}");
                cancellationTokenSource ??= CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _ = ReconnectAsync(cancellationTokenSource.Token);
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
            if (!EnsureActive("发送 QQ 官方消息"))
            {
                return;
            }

            options ??= new SendOptions { Passive = false };
            string route = target.Type switch
            {
                SendTargetType.User => $"/v2/users/{target.Id}/messages",
                SendTargetType.Group => $"/v2/groups/{target.Id}/messages",
                _ => throw new NotSupportedException("QQ 官方适配器当前仅支持用户和群目标."),
            };

            JObject payload = await BuildMessagePayloadAsync(target, content, options, cancellationToken);
            await PostJsonAsync(route, payload, cancellationToken);
        }

        public Task SendMarkdownAsync(SendTarget target, string markdown, ButtonKeyboard? keyboard = null, SendOptions? options = null, CancellationToken cancellationToken = default)
        {
            MessageContent content = new MessageContent().AddMarkdown(markdown);
            if (keyboard != null)
            {
                content.AddButton(keyboard);
            }

            return SendMessage(target, content, options, cancellationToken);
        }

        private async Task<JObject> BuildMessagePayloadAsync(SendTarget target, MessageContent content, SendOptions options, CancellationToken cancellationToken)
        {
            JObject payload = new()
            {
                ["msg_seq"] = options.Sequence,
            };

            if (!string.IsNullOrWhiteSpace(options.MessageId))
            {
                payload["msg_id"] = options.MessageId;
            }

            if (!string.IsNullOrWhiteSpace(options.EventId))
            {
                payload["event_id"] = options.EventId;
            }

            TextSegment? text = content.Segments.OfType<TextSegment>().FirstOrDefault();
            MarkdownSegment? markdown = content.Segments.OfType<MarkdownSegment>().FirstOrDefault();
            MarkdownTemplateSegment? markdownTemplate = content.Segments.OfType<MarkdownTemplateSegment>().FirstOrDefault();
            ButtonSegment? button = content.Segments.OfType<ButtonSegment>().FirstOrDefault();
            MessageSegment? image = content.Segments.FirstOrDefault(segment => segment is ImageSegment or ImageUrlSegment);

            if (markdown != null || markdownTemplate != null)
            {
                payload["msg_type"] = 2;
                var md = markdown != null ? markdown.Markdown : "";
                var textContent = content.PlainText;
                if (!string.IsNullOrEmpty(textContent))
                    md = md + textContent;
                payload["markdown"] = markdown != null
                    ? new JObject { ["content"] = md }
                    : BuildMarkdownTemplate(markdownTemplate!);
            }
            else if (image != null)
            {
                payload["msg_type"] = 7;
                payload["media"] = new JObject { ["file_info"] = await UploadMediaAsync(target, image, options, cancellationToken) };
                if (text != null)
                {
                    payload["content"] = text.Text;
                }
            }
            else
            {
                payload["msg_type"] = 2;
                payload["markdown"] = new JObject { ["content"] = content.PlainText };
            }

            if (button != null)
            {
                payload["keyboard"] = BuildKeyboard(button.Keyboard);
            }

            return payload;
        }

        private async Task<string> UploadMediaAsync(SendTarget target, MessageSegment segment, SendOptions options, CancellationToken cancellationToken)
        {
            string route = target.Type switch
            {
                SendTargetType.User => $"/v2/users/{target.Id}/files",
                SendTargetType.Group => $"/v2/groups/{target.Id}/files",
                _ => throw new NotSupportedException("QQ官方适配器当前仅支持用户和群富媒体上传."),
            };

            JObject payload = new()
            {
                ["file_type"] = 1,
                ["srv_send_msg"] = false,
                ["msg_seq"] = options.Sequence,
            };

            if (!string.IsNullOrWhiteSpace(options.EventId))
            {
                payload["event_id"] = options.EventId;
            }

            switch (segment)
            {
                case ImageSegment image:
                    payload["file_data"] = Convert.ToBase64String(image.Data);
                    break;
                case ImageUrlSegment imageUrl:
                    payload["url"] = imageUrl.Url;
                    break;
            }

            JObject response = await PostJsonAsync(route, payload, cancellationToken);
            return response.Value<string>("file_info")
                ?? throw new InvalidOperationException($"QQ官方富媒体上传失败: {response}");
        }

        private async Task RefreshAccessTokenAsync(CancellationToken cancellationToken)
        {
            JObject request = new()
            {
                ["appId"] = AppId,
                ["clientSecret"] = ClientSecret,
            };
            using StringContent content = new(request.ToString(Formatting.None), Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await httpClient.PostAsync("https://bots.qq.com/app/getAppAccessToken", content, cancellationToken);
            string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            response.EnsureSuccessStatusCode();
            accessToken = JObject.Parse(responseText).Value<string>("access_token") ?? "";
        }

        private async Task<JObject> PostJsonAsync(string route, JObject payload, CancellationToken cancellationToken)
        {
            await RefreshAccessTokenAsync(cancellationToken);
            using HttpRequestMessage request = new(HttpMethod.Post, ApiBase + route);
            request.Headers.Authorization = new AuthenticationHeaderValue("QQBot", accessToken);
            request.Headers.Add("X-Union-Appid", AppId);
            request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"QQ官方 API 请求失败: {(int)response.StatusCode} {responseText}");
            }

            //Message.Blue($"QQ官方 API 响应: {responseText}");
            //返回主动消息失败的话应该是在这里,后续可以处理一下 TODO
            return string.IsNullOrWhiteSpace(responseText) ? new JObject() : JObject.Parse(responseText);
        }

        private async Task SendIdentifyAsync(CancellationToken cancellationToken)
        {
            JObject identify = new()
            {
                ["op"] = 2,
                ["d"] = new JObject
                {
                    ["token"] = "QQBot " + accessToken,
                    ["intents"] = Intents,
                    ["shard"] = new JArray { 0, 1 },
                    ["properties"] = new JObject
                    {
                        ["$os"] = Environment.OSVersion.Platform.ToString(),
                        ["$browser"] = "Windy",
                        ["$device"] = "Windy",
                    },
                },
            };

            await SendWebSocketAsync(identify, cancellationToken);
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
                        await ProcessGatewayMessage(JObject.Parse(text), socket, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (WebSocketException ex)
                {
                    Message.Yellow($"QQ官方网关连接异常: {ex.Message}");
                    if (!cancellationToken.IsCancellationRequested && ReferenceEquals(socket, webSocket))
                    {
                        await ReconnectAsync(cancellationToken);
                    }

                    return;
                }
            }
        }

        private async Task ProcessGatewayMessage(JObject message, ClientWebSocket socket, CancellationToken cancellationToken)
        {
            int op = message.Value<int>("op");
            if (message["s"] != null)
            {
                sequenceNumber = message.Value<int>("s");
            }

            switch (op)
            {
                case 10:
                    int interval = message["d"]?.Value<int>("heartbeat_interval") ?? 45000;
                    heartbeatTask = HeartbeatLoopAsync(socket, interval, cancellationToken);
                    break;
                case 0:
                    ProcessEvent(message);
                    break;
                case 7:
                    Message.Yellow("QQ官方网关要求重连.");
                    await ReconnectAsync(cancellationToken);
                    break;
                case 9:
                    Message.Red("QQ官方网关会话无效.");
                    break;
            }
        }

        private async Task ReconnectAsync(CancellationToken cancellationToken)
        {
            if (!await reconnectLock.WaitAsync(0, cancellationToken))
            {
                return;
            }

            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                ClientWebSocket? oldSocket = webSocket;
                IsActive = false;
                webSocket = null;
                if (oldSocket?.State == WebSocketState.Open || oldSocket?.State == WebSocketState.CloseReceived)
                {
                    try
                    {
                        await oldSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "reconnect", cancellationToken);
                    }
                    catch (WebSocketException)
                    {
                    }
                }

                oldSocket?.Dispose();

                await RefreshAccessTokenAsync(cancellationToken);
                ClientWebSocket newSocket = new();
                await newSocket.ConnectAsync(new Uri(Gateway), cancellationToken);
                webSocket = newSocket;
                IsActive = true;

                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    await SendResumeAsync(cancellationToken);
                    Message.Green($"QQ官方网关已重连并尝试恢复会话: {sessionId}, seq: {sequenceNumber}");
                }
                else
                {
                    await SendIdentifyAsync(cancellationToken);
                    Message.Green("QQ官方网关已重连并重新鉴权.");
                }

                receiveTask = ReceiveLoopAsync(newSocket, cancellationToken);
            }
            finally
            {
                reconnectLock.Release();
            }
        }

        private async Task SendResumeAsync(CancellationToken cancellationToken)
        {
            JObject resume = new()
            {
                ["op"] = 6,
                ["d"] = new JObject
                {
                    ["token"] = "QQBot " + accessToken,
                    ["session_id"] = sessionId,
                    ["seq"] = sequenceNumber,
                },
            };

            await SendWebSocketAsync(resume, cancellationToken);
        }

        private void ProcessEvent(JObject envelope)
        {
            string eventType = envelope.Value<string>("t") ?? "";
            JObject data = envelope["d"] as JObject ?? new JObject();
            //Message.Blue($"QQ官方 API Event: {envelope.ToString()}");
            //调试用,如果有需要可以删除注释
            switch (eventType)
            {
                case "READY":
                    sessionId = data.Value<string>("session_id") ?? "";
                    Message.Yellow($"QQ官方 Ready Event: {sessionId}");
                    PublishEvent(new Events.AdaptorEventArgs(this, eventType, envelope));
                    break;
                case "RESUMED":
                    Message.Green("QQ官方 Resumed Event.");
                    PublishEvent(new Events.AdaptorEventArgs(this, eventType, envelope));
                    break;
                case "GROUP_AT_MESSAGE_CREATE":
                    PublishMessage(CreateGroupMessage(data, MessageScene.GroupAt, envelope));
                    break;
                case "GROUP_MESSAGE_CREATE":
                    PublishMessage(CreateGroupMessage(data, IsMentioningBot(data) ? MessageScene.GroupAt : MessageScene.Group, envelope));
                    break;
                case "C2C_MESSAGE_CREATE":
                    PublishMessage(CreatePrivateMessage(data, envelope));
                    break;
                case "GROUP_ADD_ROBOT":
                case "GROUP_DEL_ROBOT":
                case "GROUP_MEMBER_ADD":
                case "GROUP_MEMBER_REMOVE":
                case "GROUP_MSG_RECEIVE":
                case "GROUP_MSG_REJECT":
                case "SUBSCRIBE_MESSAGE_STATUS":
                    PublishEvent(new Events.AdaptorEventArgs(this, eventType, envelope));
                    break;
                default:
                    PublishEvent(new Events.AdaptorEventArgs(this, eventType, envelope));
                    break;
            }
        }

        private MessageEventArgs CreateGroupMessage(JObject data, MessageScene scene, JObject envelope)
        {
            string groupId = data.Value<string>("group_openid") ?? "";
            string author = data["author"]?.Value<string>("member_openid") ?? data.Value<string>("member_openid") ?? "";
            string authorName = data["author"]?.Value<string>("username") ?? "";
            string memberRole = data["author"]?.Value<string>("member_role") ?? "";
            MessageEventArgs args = new(this, scene, CleanMessageContent(data), data.Value<string>("id") ?? "", author, SendTarget.Group(groupId), authorName)
            {
                GroupId = groupId,
                EventId = envelope.Value<string>("id"),
                Raw = envelope,
                Role = ParseGroupMemberRole(memberRole),
            };
            Message.Yellow($"[QQOfficial][{args.GroupId}]{args.AuthorName}({args.Role}):{args.Content}");
            AddAttachments(args, data);
            return args;
        }

        private static GroupMemberRole ParseGroupMemberRole(string role)
        {
            return role.ToLowerInvariant() switch
            {
                "owner" => GroupMemberRole.Owner,
                "admin" => GroupMemberRole.Admin,
                _ => GroupMemberRole.Member,
            };
        }

        private MessageEventArgs CreatePrivateMessage(JObject data, JObject envelope)
        {
            string author = data["author"]?.Value<string>("user_openid") ?? data.Value<string>("openid") ?? "";
            string authorName = data["author"]?.Value<string>("username") ?? "";
            MessageEventArgs args = new(this, MessageScene.Private, CleanMessageContent(data), data.Value<string>("id") ?? "", author, SendTarget.User(author), authorName)
            {
                EventId = envelope.Value<string>("id"),
                Raw = envelope,
            };
            Message.Yellow($"[QQOfficial][{args.AuthorId}]{args.AuthorName}:{args.Content}");
            AddAttachments(args, data);
            return args;
        }

        private static string CleanMessageContent(JObject data)
        {
            string content = data.Value<string>("content") ?? "";
            if (data["mentions"] is not JArray mentions)
            {
                return content.TrimStart();
            }

            foreach (JObject mention in mentions.OfType<JObject>())
            {
                if (mention.Value<bool?>("is_you") != true)
                {
                    continue;
                }

                RemoveMention(ref content, mention.Value<string>("id"));
                RemoveMention(ref content, mention.Value<string>("member_openid"));
            }

            return content.TrimStart();
        }

        private static bool IsMentioningBot(JObject data)
        {
            return data["mentions"] is JArray mentions && mentions.OfType<JObject>().Any(mention => mention.Value<bool?>("is_you") == true);
        }

        private static void RemoveMention(ref string content, string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            content = content.Replace($"<@{id}>", "", StringComparison.Ordinal);
        }

        private static void AddAttachments(MessageEventArgs args, JObject data)
        {
            if (data["attachments"] is not JArray attachments)
            {
                return;
            }

            foreach (JToken attachment in attachments)
            {
                args.Attachments.Add(new MessageAttachment
                {
                    ContentType = attachment.Value<string>("content_type") ?? "",
                    FileName = attachment.Value<string>("filename") ?? "",
                    Url = attachment.Value<string>("url") ?? "",
                });
            }
        }

        private async Task HeartbeatLoopAsync(ClientWebSocket socket, int interval, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && ReferenceEquals(socket, webSocket) && socket.State == WebSocketState.Open)
            {
                try
                {
                    await SendWebSocketAsync(socket, new JObject { ["op"] = 1, ["d"] = sequenceNumber }, cancellationToken);
                    await Task.Delay(interval, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (WebSocketException ex)
                {
                    Message.Yellow($"QQ官方网关心跳异常: {ex.Message}");
                    return;
                }
            }
        }

        private async Task SendWebSocketAsync(JObject payload, CancellationToken cancellationToken)
        {
            if (webSocket == null)
            {
                return;
            }

            await SendWebSocketAsync(webSocket, payload, cancellationToken);
        }

        private static async Task SendWebSocketAsync(ClientWebSocket socket, JObject payload, CancellationToken cancellationToken)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(payload.ToString(Formatting.None));
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
        }

        private static JObject BuildMarkdownTemplate(MarkdownTemplateSegment segment)
        {
            return new JObject
            {
                ["custom_template_id"] = segment.TemplateId,
                ["params"] = new JArray(segment.Parameters.Select(item => new JObject
                {
                    ["key"] = item.Key,
                    ["values"] = new JArray(item.Value),
                })),
            };
        }

        private static JObject BuildKeyboard(ButtonKeyboard keyboard)
        {
            if (!string.IsNullOrWhiteSpace(keyboard.Id))
            {
                return new JObject { ["id"] = keyboard.Id };
            }

            return new JObject
            {
                ["content"] = new JObject
                {
                    ["rows"] = new JArray(keyboard.Rows.Select(row => new JObject
                    {
                        ["buttons"] = new JArray(row.Buttons.Select(button => new JObject
                        {
                            ["id"] = button.Id,
                            ["render_data"] = new JObject { ["label"] = button.RenderLabel, ["style"] = 1 },
                            ["action"] = new JObject
                            {
                                ["type"] = button.ActionType,
                                ["permission"] = new JObject { ["type"] = button.PermissionType },
                                ["data"] = button.ActionData,
                                ["enter"] = true,
                            },
                        })),
                    })),
                },
            };
        }
    }
}
