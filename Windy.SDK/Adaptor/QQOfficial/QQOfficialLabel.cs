namespace Windy.SDK.Adaptor.QQOfficial
{
    public static class QQOfficialLabel
    {
        public static string At(string userId)
        {
            return $"<qqbot-at-user id=\"{Attribute(userId)}\" />";
        }

        public static string AtEveryone()
        {
            return "<qqbot-at-everyone />";
        }

        public static string CommandEnter(string text)
        {
            return $"<qqbot-cmd-enter text=\"{Url(text)}\" />";
        }

        public static string CommandInput(string text, string? show = null, bool reference = false)
        {
            string label = $"<qqbot-cmd-input text=\"{Url(text)}\"";
            if (!string.IsNullOrWhiteSpace(show))
            {
                label += $" show=\"{Url(show)}\"";
            }

            label += $" reference=\"{reference.ToString().ToLowerInvariant()}\" />";
            return label;
        }

        public static string Channel(string channelId)
        {
            return $"<#{Attribute(channelId)}>";
        }

        public static string Emoji(string emojiId)
        {
            return $"<emoji:{Attribute(emojiId)}>";
        }

        private static string Url(string value)
        {
            return Uri.EscapeDataString(value);
        }

        private static string Attribute(string value)
        {
            return value
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal);
        }
    }

    public static class QQOfficicalLabel
    {
        public static string At(string userId) => QQOfficialLabel.At(userId);

        public static string AtEveryone() => QQOfficialLabel.AtEveryone();

        public static string CommandEnter(string text) => QQOfficialLabel.CommandEnter(text);

        public static string CommandInput(string text, string? show = null, bool reference = false) => QQOfficialLabel.CommandInput(text, show, reference);

        public static string Channel(string channelId) => QQOfficialLabel.Channel(channelId);

        public static string Emoji(string emojiId) => QQOfficialLabel.Emoji(emojiId);
    }
}
