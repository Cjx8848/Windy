using Spectre.Console;
using Windy.SDK;
using Windy.SDK.Utils;

namespace Windy;

public class Program
{
    public static async Task Main(string[] args)
    {
        AnsiConsole.Write(new FigletText("Windy").Color(Color.Aqua));
        Console.Title = "Windy | Cjx8848";

        var config = JsonTool.Create<WindyConfig>(Path.Combine(AppContext.BaseDirectory, "Config", "Windy.json"))
            .InitMessage("Windy 配置已创建，请启用需要的适配器后重启.")
            .Success("Windy 配置读取完成.")
            .Error("Windy 配置读取失败:")
            .Read();

        using CancellationTokenSource cancellationTokenSource = new();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        await WindyRuntime.InitializeAsync(config.Content, cancellationTokenSource.Token);
        Message.Blue("Windy Bot客户端成功启动!");

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }

        await WindyRuntime.StopAsync();
    }
}
