using Newtonsoft.Json;

namespace Windy.SDK.Utils
{
    public static class JsonTool
    {
        public static JsonObject<T> Create<T>(string path) where T : new()
        {
            return new JsonObject<T>(path);
        }
    }

    public sealed class JsonObject<T> where T : new()
    {
        private string errorText = "";
        private string successText = "";
        private string initText = "";
        internal JsonObject(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("配置路径不能为空.", nameof(path));
            }

            Path = path;
            Content = new T();
        }

        public string Path { get; }

        public T Content { get; set; }

        public JsonObject<T> InitContent(T content)
        {
            Content = content;
            return this;
        }

        public JsonObject<T> Error(string text)
        {
            errorText = text;
            return this;
        }

        public JsonObject<T> InitMessage(string text)
        {
            initText = text;
            return this;
        }

        public JsonObject<T> Success(string text)
        {
            successText = text;
            return this;
        }

        public bool Exists()
        {
            return File.Exists(Path);
        }

        public JsonObject<T> Read()
        {
            try
            {
                if (!File.Exists(Path))
                {
                    Write();
                    ShowInit();
                    return this;
                }

                using StreamReader file = File.OpenText(Path);
                JsonSerializer serializer = new();
                Content = (T?)serializer.Deserialize(file, typeof(T)) ?? new T();
                ShowSuccess();
                return this;
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return this;
            }
        }

        public void Write()
        {
            try
            {
                string? directory = System.IO.Path.GetDirectoryName(Path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using FileStream fileStream = new(Path, FileMode.Create, FileAccess.Write, FileShare.Read);
                using StreamWriter streamWriter = new(fileStream);
                JsonSerializer serializer = new()
                {
                    Formatting = Formatting.Indented,
                };

                serializer.Serialize(streamWriter, Content);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ShowInit()
        {
            if (!string.IsNullOrEmpty(initText))
            {
                Message.LogConsole(initText, ConsoleColor.Green);
            }
            else
            {
                Message.LogConsole($"[{typeof(T).Name}] 成功生成配置!", ConsoleColor.Green);
            }
        }

        private void ShowSuccess()
        {
            if (!string.IsNullOrEmpty(successText))
            {
                Message.LogConsole(successText, ConsoleColor.Green);
            }
            else
            {
                Message.LogConsole($"[{typeof(T).Name}] 成功读取配置!", ConsoleColor.Green);
            }
        }

        private void ShowError(Exception ex)
        {
            string text = string.IsNullOrEmpty(errorText) ? $"[{typeof(T).Name}] 配置加载失败:" : errorText;
            Message.LogConsole($"{text} {ex}", ConsoleColor.Red);
        }
    }
}
