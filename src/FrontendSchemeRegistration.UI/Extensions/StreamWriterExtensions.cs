namespace FrontendSchemeRegistration.UI.Extensions
{
    public static class StreamWriterExtensions
    {
        public static async Task WriteCsvCellAsync(this StreamWriter writer, string s)
        {
            writer.WriteAsync(string.Concat(CleanCsv(s), ","));
        }

        /// <summary>
        /// Turn a string into a CSV cell output
        /// </summary>
        /// <param name="s">String to output</param>
        /// <returns>The CSV cell formatted string</returns>
        public static string CleanCsv(string s)
        {

            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            if (!s.Any(x => x == ',' || x == '\"' || x == '\r' || x == '\n'))
            {
                return s;
            }

            s = s.Replace("\"", "\"\"");

            return string.Format("\"{0}\"", s);
        }
    }
}
