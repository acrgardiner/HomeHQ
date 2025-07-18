namespace projectaardvarkx2.Helpers
{
    public static class Formatter
    {
        public static string FormatFileSize(float bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public static string FormatDate(DateTime? date)
        {
            string result = "";

            if (date.HasValue)
            {
                result = date.Value.ToString("d");
            }

            return result;
        }
    }
}
