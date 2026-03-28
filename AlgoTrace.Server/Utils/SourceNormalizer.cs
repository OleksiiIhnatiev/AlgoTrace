﻿using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Utils
{
    public static class SourceNormalizer
    {
        public static string NormalizeLine(string line, bool ignoreWhitespace = true)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            try
            {
                var processed = line.ToLower();
                if (ignoreWhitespace)
                {
                    processed = Regex.Replace(processed, @"\s+", "", RegexOptions.None, TimeSpan.FromSeconds(1));
                }
                return processed;
            }
            catch (RegexMatchTimeoutException)
            {
                if (ignoreWhitespace)
                {
                    return line.Replace(" ", "").Replace("\t", "").ToLower();
                }
                return line.ToLower();
            }
        }

        public static string[] GetLines(string code)
        {
            return code?.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                ?? Array.Empty<string>();
        }
    }
}
