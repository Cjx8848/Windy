using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Windy.SDK
{
    public static class Message
    {
        private static readonly object SyncRoot = new();
        private static readonly Regex ColorRegex = new(@"\[c/(?<color>[0-9a-fA-F]{6}):(?<text>.*?)\]", RegexOptions.Compiled);

        public static string LogDirectory { get; set; } = Path.Combine(WindyRuntime.BasicPath, "logs");

        public static string LogFileName { get; set; } = "latest.log";

        public static string TimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

        public static void Write(string text, ConsoleColor? color = null , bool withtime = true)
        {
            WriteConsole(withtime ? WithTime(text) : text, color, true);
        }

        public static void WriteLine(string text, ConsoleColor? color = null)
        {
            Write(text, color);
        }

        public static void Blue(string text)
        {
            Write(text, ConsoleColor.Cyan);
        }

        public static void BlueInfo(string text)
        {
            Write(text, ConsoleColor.Cyan, false);
        }

        public static void Yellow(string text)
        {
            Write(text, ConsoleColor.Yellow);
        }

        public static void YellowInfo(string text)
        {
            Write(text, ConsoleColor.Yellow, false);
        }

        public static void Green(string text)
        {
            Write(text, ConsoleColor.Green);
        }

        public static void GreenInfo(string text)
        {
            Write(text, ConsoleColor.Green, false);
        }

        public static void Red(string text)
        {
            Write(text, ConsoleColor.DarkRed);
        }

        public static void RedInfo(string text)
        {
            Write(text, ConsoleColor.DarkRed, false);
        }

        public static void WriteHex(string text, string hexColor)
        {
            Write(text, ToConsoleColor(hexColor));
        }

        public static void Log(string text)
        {
            WriteLog(WithTime(StripColorTags(text)));
        }

        public static void LogConsole(string text, ConsoleColor? color = null)
        {
            string message = WithTime(text);
            WriteConsole(message, color, true);
            WriteLog(StripColorTags(message));
        }

        private static string WithTime(string text)
        {
            return $"[{DateTime.Now.ToString(TimeFormat)}] {text}";
        }

        private static void WriteConsole(string text, ConsoleColor? color, bool newLine)
        {
            lock (SyncRoot)
            {
                ConsoleColor originalColor = Console.ForegroundColor;

                if (color.HasValue)
                {
                    Console.ForegroundColor = color.Value;
                    WriteRaw(text, newLine);
                    Console.ForegroundColor = originalColor;
                    return;
                }

                WriteMixedColor(text, newLine, originalColor);
            }
        }

        private static void WriteMixedColor(string text, bool newLine, ConsoleColor originalColor)
        {
            int lastIndex = 0;

            foreach (Match match in ColorRegex.Matches(text))
            {
                if (match.Index > lastIndex)
                {
                    Console.ForegroundColor = originalColor;
                    Console.Write(text[lastIndex..match.Index]);
                }

                Console.ForegroundColor = ToConsoleColor(match.Groups["color"].Value);
                Console.Write(match.Groups["text"].Value);
                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                Console.ForegroundColor = originalColor;
                Console.Write(text[lastIndex..]);
            }

            Console.ForegroundColor = originalColor;

            if (newLine)
            {
                Console.WriteLine();
            }
        }

        private static void WriteRaw(string text, bool newLine)
        {
            if (newLine)
            {
                Console.WriteLine(StripColorTags(text));
                return;
            }

            Console.Write(StripColorTags(text));
        }

        private static void WriteLog(string text)
        {
            //if (!Main.Config.EnableLog)
                //return;
            lock (SyncRoot)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(Path.Combine(LogDirectory, LogFileName), text + Environment.NewLine, Encoding.UTF8);
            }
        }

        private static string StripColorTags(string text)
        {
            return ColorRegex.Replace(text, match => match.Groups["text"].Value);
        }

        private static ConsoleColor ToConsoleColor(string hexColor)
        {
            string hex = hexColor.Trim().TrimStart('#');
            if (hex.Length != 6 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int value))
            {
                throw new ArgumentException("Color must be a 6-digit hex value, such as FFFFFF.", nameof(hexColor));
            }

            int red = value >> 16 & 0xFF;
            int green = value >> 8 & 0xFF;
            int blue = value & 0xFF;

            return NearestConsoleColor(red, green, blue);
        }

        private static ConsoleColor NearestConsoleColor(int red, int green, int blue)
        {
            ConsoleColor nearestColor = ConsoleColor.White;
            double nearestDistance = double.MaxValue;

            foreach ((ConsoleColor color, int r, int g, int b) in ConsoleColors)
            {
                double distance = Math.Pow(red - r, 2) + Math.Pow(green - g, 2) + Math.Pow(blue - b, 2);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestColor = color;
                }
            }

            return nearestColor;
        }

        private static readonly (ConsoleColor Color, int Red, int Green, int Blue)[] ConsoleColors =
        {
            (ConsoleColor.Black, 0, 0, 0),
            (ConsoleColor.DarkBlue, 0, 0, 128),
            (ConsoleColor.DarkGreen, 0, 128, 0),
            (ConsoleColor.DarkCyan, 0, 128, 128),
            (ConsoleColor.DarkRed, 128, 0, 0),
            (ConsoleColor.DarkMagenta, 128, 0, 128),
            (ConsoleColor.DarkYellow, 128, 128, 0),
            (ConsoleColor.Gray, 192, 192, 192),
            (ConsoleColor.DarkGray, 128, 128, 128),
            (ConsoleColor.Blue, 0, 0, 255),
            (ConsoleColor.Green, 0, 255, 0),
            (ConsoleColor.Cyan, 0, 255, 255),
            (ConsoleColor.Red, 255, 0, 0),
            (ConsoleColor.Magenta, 255, 0, 255),
            (ConsoleColor.Yellow, 255, 255, 0),
            (ConsoleColor.White, 255, 255, 255),
        };
        static ConsoleColor ConvertToConsoleColor(string hex)
        {
            int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            var consoleColors = new (ConsoleColor Color, int R, int G, int B)[]
            {
        (ConsoleColor.Black, 0, 0, 0),
        (ConsoleColor.DarkBlue, 0, 0, 139),
        (ConsoleColor.DarkGreen, 0, 100, 0),
        (ConsoleColor.DarkCyan, 0, 139, 139),
        (ConsoleColor.DarkRed, 139, 0, 0),
        (ConsoleColor.DarkMagenta, 139, 0, 139),
        (ConsoleColor.DarkYellow, 184, 134, 11),
        (ConsoleColor.Gray, 169, 169, 169),
        (ConsoleColor.DarkGray, 85, 85, 85),
        (ConsoleColor.Blue, 0, 0, 255),
        (ConsoleColor.Green, 0, 128, 0),
        (ConsoleColor.Cyan, 0, 255, 255),
        (ConsoleColor.Red, 255, 0, 0),
        (ConsoleColor.Magenta, 255, 0, 255),
        (ConsoleColor.Yellow, 255, 255, 0),
        (ConsoleColor.White, 255, 255, 255)
            };
            ConsoleColor closestColor = ConsoleColor.Black;
            double closestDistance = double.MaxValue;
            foreach (var consoleColor in consoleColors)
            {
                double distance = Math.Sqrt(Math.Pow(r - consoleColor.R, 2) + Math.Pow(g - consoleColor.G, 2) + Math.Pow(b - consoleColor.B, 2));
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestColor = consoleColor.Color;
                }
            }
            return closestColor;
        }
        static ConsoleColor ConvertToConsoleColor(Color color)
        {
            int r = color.R, g = color.G, b = color.B;

            if (r > 200 && g < 50 && b < 50) return ConsoleColor.Red;
            else if (r < 50 && g > 200 && b < 50) return ConsoleColor.Green;
            else if (r < 50 && g < 50 && b > 200) return ConsoleColor.Blue;
            else if (r > 200 && g > 200 && b < 50) return ConsoleColor.Yellow;
            else if (r < 50 && g > 200 && b > 200) return ConsoleColor.Cyan;
            else if (r > 200 && g < 50 && b > 200) return ConsoleColor.Magenta;
            else if (r > 200 && g > 200 && b > 200) return ConsoleColor.White;
            else if (r < 50 && g < 50 && b < 50) return ConsoleColor.Black;
            else if ((r > 100 && r < 200) && (g > 100 && g < 200) && (b > 100 && b < 200)) return ConsoleColor.Gray;
            else if ((r > 50 && r < 100) && (g > 50 && g < 100) && (b > 50 && b < 100)) return ConsoleColor.DarkGray;
            else return ConsoleColor.Green; // 如果没有匹配，使用暗灰色作为默认颜色
        }
        public static void SendChannalMessage(string message, Color color)
        {
            message = ReplaceItemCodesWithNames(message);
            ConsoleColor defaultConsoleColor = ConvertToConsoleColor(color);
            string pattern = @"\[c\/([0-9a-fA-F]{6}):([^\]]+)\]";
            MatchCollection matches = Regex.Matches(message, pattern);
            int lastPos = 0;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(WithTime("[ChannalMessgae]"));
            Console.ResetColor();

            foreach (Match match in matches)
            {
                Console.ForegroundColor = defaultConsoleColor; // 应用初始颜色
                Console.Write(message.Substring(lastPos, match.Index - lastPos));
                Console.ResetColor(); // 重置颜色
                string colorCode = match.Groups[1].Value;
                string text = match.Groups[2].Value;
                Console.ForegroundColor = ConvertToConsoleColor(colorCode);
                Console.Write(text);
                Console.ResetColor();
                lastPos = match.Index + match.Length;
            }
            Console.ForegroundColor = defaultConsoleColor; // 应用初始颜色
            Console.Write(message.Substring(lastPos));
            Console.ResetColor(); // 重置颜色
            Console.Write("\n");
            WriteLog(WithTime(StripColorTags(message)));
        }
        //TODO
        public static string ReplaceItemCodesWithNames(string input)
        {
            string pattern = @"\[i(?:\/p\d+)?:([^]]+)\]";
            return Regex.Replace(input, pattern, match =>
            {
                string itemId = match.Groups[1].Value;
                var itemName = "暂无适配";
                return $"[c/00BFFF:[{itemName}]]";
            });
        }
    }
}
