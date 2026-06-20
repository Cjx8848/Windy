# Windy C# QQ Bot SDK

Windy 是一个正在重做中的 QQ Bot SDK 与启动端项目。当前重点支持 QQ 官方 Bot 适配器，并提供插件、命令、消息内容、Hook 事件管线等基础能力。

当前状态：QQ 官方 Bot 适配器可用；Milky 适配器已预留结构，协议接入尚未完成。

## 项目结构

```text
Windy/
  Windy/                 启动端控制台程序
  Windy.SDK/             SDK 主体
  ExamplePlugin/         示例插件
```

主要目录：

```text
Windy.SDK/Adaptor/       适配器抽象与具体适配器
Windy.SDK/Command/       命令系统
Windy.SDK/Events/        通用消息与事件参数
Windy.SDK/Hooks/         Hook 管线
Windy.SDK/Plugin/        插件基类与加载器
Windy.SDK/Utils/         JsonTool 等工具
```

## 快速运行

构建主体端：

```powershell
dotnet build "Windy.slnx"
```

构建示例插件：

```powershell
dotnet build "ExamplePlugin\ExamplePlugin.csproj"
```

复制插件到主体端输出目录：

```powershell
Copy-Item -LiteralPath "ExamplePlugin\bin\Debug\net10.0\ExamplePlugin.dll" -Destination "Windy\bin\Debug\net10.0\Plugins\ExamplePlugin.dll" -Force
```

运行主体端：

```powershell
dotnet run --project "Windy\Windy.csproj"
```

## 配置文件

启动端会读取：

```text
Windy\bin\Debug\net10.0\Config\Windy.json
```

如果不存在，会自动创建默认配置。

示例：

```json
{
  "OwnerOpenID": "",
  "Debug": false,
  "Adaptors": [
    {
      "Name": "qq-official",
      "Enabled": true,
      "Settings": {
        "AppId": "",
        "ClientSecret": "",
        "Sandbox": "false"
      }
    },
    {
      "Name": "milky",
      "Enabled": false,
      "Settings": {
        "Endpoint": "ws://127.0.0.1:3001",
        "AccessToken": ""
      }
    }
  ],
  "Plugins": {
    "Directory": "Plugins"
  }
}
```

注意：不要把真实 `ClientSecret` 提交到仓库。

## 插件开发

插件继承 `WindyPlugin`：

```csharp
using Windy.SDK.Adaptor;
using Windy.SDK.Plugin;

public sealed class Plugin : WindyPlugin
{
    public override string Name => "Example";
    public override string Version => "1.0.0";
    public override string Author => "Cjx";
    public override string Description => "示例插件";
    public override AdaptorType RequiredAdaptor => AdaptorType.QQOfficial;

    public override void Initialize()
    {
        // 这里注册命令以外的 Hook 或读取插件配置。
    }
}
```

插件只能声明一个适配器类型：

```csharp
public override AdaptorType RequiredAdaptor => AdaptorType.QQOfficial;
```

插件初始化前 SDK 会注入：

```csharp
Adaptor   // 当前插件绑定的适配器实例
Hooks     // Hook 注册器
```

## 命令系统

命令使用 Attribute 注册：

```csharp
using Windy.SDK.Command;
using Windy.SDK.Events;

[Command("ID", "查看当前消息信息", MessageScene.Group)]
[Command("ID", "查看当前消息信息", MessageScene.GroupAt)]
public async Task TestUID(CommandArgs args)
{
    await args.Adaptor.SendMessage($"{args.Message.GroupId}\n{args.Message.AuthorId}\n{args.Message.AuthorName}");
}
```

`CommandArgs` 常用字段：

```csharp
args.CommandName
args.Parameters
args.Adaptor
args.Message.Content
args.Message.AuthorId
args.Message.AuthorName
args.Message.GroupId
args.Message.MessageId
args.Message.EventId
args.Message.Attachments
args.Message.Raw
```

QQ 官方适配器会在命令执行前清理机器人自己的 AT 标签。例如：

```text
<@机器人ID> 测试
```

进入命令系统后会变成：

```text
测试
```

只会移除 `mentions[].is_you == true` 的机器人自身 AT，不会删除 AT 其他人的内容。

## 发送消息

被动回复当前消息：

```csharp
await args.Adaptor.SendMessage("hello");
```

主动发送群消息：

```csharp
await Adaptor.SendMessage(groupOpenId, "hello");
```

混合消息内容：

```csharp
MessageContent content = new MessageContent()
    .AddText("文本")
    .AddImage(imageBytes)
    .AddMarkdown("# 标题")
    .AddButton(keyboard);

await args.Adaptor.SendMessage(content);
```

Markdown + 按钮：

```csharp
ButtonKeyboard keyboard = new()
{
    Rows =
    [
        new ButtonRow
        {
            Buttons =
            [
                new MessageButton { RenderLabel = "菜单", ActionData = "/菜单" },
                new MessageButton { RenderLabel = "返回", ActionData = "/返回" },
            ],
        },
    ],
};

MessageContent content = new MessageContent()
    .AddMarkdown("# **黑体大标题**\n\n- 功能1\n- 功能2\n- 功能3")
    .AddButton(keyboard);

await args.Adaptor.SendMessage(content);
```

## QQ 官方文本交互标签

命名空间：

```csharp
using Windy.SDK.Adaptor.QQOfficial;
```

用法：

```csharp
QQOfficialLabel.At("user_openid")
QQOfficialLabel.AtEveryone()
QQOfficialLabel.CommandEnter("/菜单")
QQOfficialLabel.CommandInput("/查询", "点我查询", reference: true)
QQOfficialLabel.Channel("channel_id")
QQOfficialLabel.Emoji("4")
```

兼容旧拼写：

```csharp
QQOfficicalLabel.At("user_openid")
```

## Hook 事件系统

Hook 支持：

```text
优先级 priority
Handled 截断后续 Hook
消息 Hook 拦截命令执行
QQ 官方事件类型化 Args
```

消息 Hook：

```csharp
RegisterMessageHook(OnMessageAsync, priority: 0);

private static Task OnMessageAsync(MessageEventArgs args)
{
    if (args.Content == "hook拦截")
    {
        args.Handled = true;
        return args.Adaptor.SendMessage("这条消息不会继续进入命令系统.");
    }

    return Task.CompletedTask;
}
```

QQ 官方事件 Hook：

```csharp
using Windy.SDK.Adaptor.QQOfficial;

this.RegisterGroupAddRobotHook(OnGroupAddRobotAsync, priority: 100);

private static Task OnGroupAddRobotAsync(QQOfficialGroupOperationEventArgs args)
{
    Message.Yellow($"group={args.GroupOpenId}, operator={args.OperatorMemberOpenId}, timestamp={args.Timestamp}");
    args.Handled = true;
    return Task.CompletedTask;
}
```

群管理事件常用 Args：

```csharp
args.GroupOpenId
args.OperatorMemberOpenId
args.Timestamp
args.Handled
```

群成员事件常用 Args：

```csharp
args.GroupOpenId
args.MemberOpenId
args.OperatorMemberOpenId
args.Timestamp
args.Handled
```

## QQ 官方事件枚举

`QQOfficialEventType` 包含：

```csharp
Ready
Resumed
GroupAtMessageCreate
GroupMessageCreate
C2CMessageCreate
GroupAddRobot
GroupDelRobot
GroupMemberAdd
GroupMemberRemove
GroupMsgReceive
GroupMsgReject
FriendAdd
FriendDel
C2CMsgReceive
C2CMsgReject
SubscribeMessageStatus
InteractionCreate
MessageAuditPass
MessageAuditReject
AtMessageCreate
MessageCreate
DirectMessageCreate
Unknown
```

也可以用通用枚举 Hook：

```csharp
this.RegisterQQOfficialEventHook(QQOfficialEventType.AtMessageCreate, OnGenericOfficialEventAsync);
```

## QQ 官方适配器能力

当前支持：

```text
WebSocket Gateway 连接
AccessToken 获取
心跳
op=7 Reconnect 重连
op=6 Resume 恢复会话
文本消息
图片/富媒体消息
Markdown 消息
按钮消息
Markdown + 按钮同发
文本交互标签
群消息与 AT 消息解析
发送人 OpenID 与用户名解析
机器人 AT 标签清理
类型化事件 Hook
```

## Milky 适配器

`MilkyAdaptor` 当前是预留结构，尚未完成协议连接与消息发送。后续计划基于 Milky/Sora 接入。

## 备注

QQ 官方 Bot 的部分事件是否推送取决于开放平台权限、事件订阅、机器人类型和实际配置。SDK 会尽量把收到的事件抛到 Hook 管线，未收到的事件需要先检查开放平台侧配置。
