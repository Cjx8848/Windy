using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Windy.SDK.Services
{
    public static class FileServer
    {
        private static readonly object SyncRoot = new();

        private static TcpListener? listener;
        private static CancellationTokenSource? cancellation;
        private static Timer? cleanupTimer;
        private static bool started;

        private static string host = "0.0.0.0";
        private static int port = 8849;
        private static string endpoint = "/files/";
        private static string publicBaseUrl = "http://127.0.0.1:8849";
        private static string storagePath = "Datas/FileServer";
        private static int retentionMinutes = 30;

        public static void Start(AdaptorConfig config)
        {
            lock (SyncRoot)
            {
                if (started)
                    return;

                if (!config.GetBool("FileServerEnabled"))
                    return;

                host = config.Get("FileServerHost", "0.0.0.0");
                port = int.TryParse(config.Get("FileServerPort", "8849"), out int p) ? p : 8849;
                endpoint = config.Get("FileServerEndpoint", "/files/");
                publicBaseUrl = config.Get("FileServerPublicBaseUrl", $"http://127.0.0.1:{port}");
                storagePath = config.Get("FileServerStoragePath", "Datas/FileServer");
                retentionMinutes = int.TryParse(config.Get("FileServerRetentionMinutes", "30"), out int r) ? Math.Max(1, r) : 30;

                Directory.CreateDirectory(GetStoragePath());
                cancellation = new CancellationTokenSource();
                listener = new TcpListener(ParseHost(host), port);
                listener.Start();
                started = true;
                _ = AcceptLoopAsync(cancellation.Token);

                cleanupTimer = new Timer(_ => CleanupExpiredFiles(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }

            Message.Blue($"[FileServer] 图床已启动: {host}:{port}{NormalizeEndpoint(endpoint)}  |  文件保留 {retentionMinutes} 分钟");
        }

        public static void Stop()
        {
            lock (SyncRoot)
            {
                if (!started)
                    return;

                cleanupTimer?.Dispose();
                cleanupTimer = null;
                cancellation?.Cancel();
                listener?.Stop();
                cancellation?.Dispose();
                cancellation = null;
                listener = null;
                started = false;
            }
        }

        public static string Upload(byte[] data, string extension = "png")
        {
            if (data.Length == 0)
                throw new ArgumentException("文件内容不能为空.", nameof(data));

            string fileName = CreateFileName(extension);
            string path = Path.Combine(GetStoragePath(), fileName);
            File.WriteAllBytes(path, data);
            return BuildUrl(fileName);
        }

        public static string Upload(Stream stream, string extension = "png")
        {
            using MemoryStream memoryStream = new();
            stream.CopyTo(memoryStream);
            return Upload(memoryStream.ToArray(), extension);
        }

        public static string Upload(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在.", filePath);

            string ext = Path.GetExtension(filePath);
            return Upload(File.ReadAllBytes(filePath), ext);
        }

        private static async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await listener!.AcceptTcpClientAsync(token);
                    _ = HandleClientAsync(client, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Message.Red($"[FileServer] 连接失败: {ex.Message}");
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            await using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream, Encoding.ASCII, leaveOpen: true);
            using (client)
            {
                string? requestLine = await reader.ReadLineAsync(token);
                if (string.IsNullOrWhiteSpace(requestLine))
                {
                    await WriteResponseAsync(stream, 400, "Bad Request", "text/plain", "Bad Request"u8.ToArray(), token);
                    return;
                }

                while (!string.IsNullOrEmpty(await reader.ReadLineAsync(token)))
                {
                }

                string[] parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2 || parts[0] != "GET")
                {
                    await WriteResponseAsync(stream, 405, "Method Not Allowed", "text/plain", "Method Not Allowed"u8.ToArray(), token);
                    return;
                }

                string normEndpoint = NormalizeEndpoint(endpoint);
                string path = Uri.UnescapeDataString(parts[1].Split('?')[0]);
                if (!path.StartsWith(normEndpoint, StringComparison.OrdinalIgnoreCase))
                {
                    await WriteResponseAsync(stream, 404, "Not Found", "text/plain", "Not Found"u8.ToArray(), token);
                    return;
                }

                string fileName = path[normEndpoint.Length..];
                if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\'))
                {
                    await WriteResponseAsync(stream, 404, "Not Found", "text/plain", "Not Found"u8.ToArray(), token);
                    return;
                }

                string filePath = Path.Combine(GetStoragePath(), fileName);
                if (!File.Exists(filePath))
                {
                    await WriteResponseAsync(stream, 404, "Not Found", "text/plain", "Not Found"u8.ToArray(), token);
                    return;
                }

                byte[] data = await File.ReadAllBytesAsync(filePath, token);
                await WriteResponseAsync(stream, 200, "OK", GetContentType(filePath), data, token);
            }
        }

        private static async Task WriteResponseAsync(NetworkStream stream, int statusCode, string statusText, string contentType, byte[] data, CancellationToken token)
        {
            string header = $"HTTP/1.1 {statusCode} {statusText}\r\nContent-Type: {contentType}\r\nContent-Length: {data.Length}\r\nCache-Control: public, max-age=86400\r\nConnection: close\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            await stream.WriteAsync(headerBytes, token);
            await stream.WriteAsync(data, token);
        }

        private static void CleanupExpiredFiles()
        {
            try
            {
                string dir = GetStoragePath();
                if (!Directory.Exists(dir))
                    return;

                DateTime cutoff = DateTime.Now.AddMinutes(-retentionMinutes);
                foreach (string file in Directory.GetFiles(dir))
                {
                    try
                    {
                        if (File.GetCreationTime(file) < cutoff)
                            File.Delete(file);
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Message.Red($"[FileServer] 清理过期文件失败: {ex.Message}");
            }
        }

        private static string BuildUrl(string fileName)
        {
            string baseUrl = string.IsNullOrWhiteSpace(publicBaseUrl)
                ? $"http://{(host == "0.0.0.0" ? "127.0.0.1" : host)}:{port}"
                : publicBaseUrl.TrimEnd('/');

            return $"{baseUrl}{NormalizeEndpoint(endpoint)}{Uri.EscapeDataString(fileName)}";
        }

        private static string CreateFileName(string extension)
        {
            extension = NormalizeExtension(extension);
            return $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}{extension}";
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return ".bin";

            extension = extension.Trim();
            return extension.StartsWith('.') ? extension : $".{extension}";
        }

        private static string NormalizeEndpoint(string ep)
        {
            if (string.IsNullOrWhiteSpace(ep))
                return "/files/";

            ep = ep.Trim();
            if (!ep.StartsWith('/'))
                ep = $"/{ep}";
            if (!ep.EndsWith('/'))
                ep = $"{ep}/";
            return ep;
        }

        private static string GetStoragePath()
        {
            return Path.IsPathRooted(storagePath)
                ? storagePath
                : Path.Combine(WindyRuntime.BasicPath, storagePath);
        }

        private static IPAddress ParseHost(string h)
        {
            if (string.IsNullOrWhiteSpace(h) || h == "0.0.0.0")
                return IPAddress.Any;
            if (h == "::")
                return IPAddress.IPv6Any;
            return IPAddress.Parse(h);
        }

        private static string GetContentType(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".bmp" => "image/bmp",
                ".html" or ".htm" => "text/html; charset=utf-8",
                ".json" => "application/json; charset=utf-8",
                ".txt" => "text/plain; charset=utf-8",
                _ => "application/octet-stream",
            };
        }
    }
}
