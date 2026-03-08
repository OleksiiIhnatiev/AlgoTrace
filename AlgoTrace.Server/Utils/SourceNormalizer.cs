using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Utils
{
    public static class SourceNormalizer
    {
        public static string NormalizeLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(line, @"\s+", "").ToLower();
        }

        public static string[] GetLines(string code)
        {
            return code?.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                ?? Array.Empty<string>();
        }
    }
}
