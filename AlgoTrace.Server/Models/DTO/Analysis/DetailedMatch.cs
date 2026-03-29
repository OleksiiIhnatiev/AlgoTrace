using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Models.DTO.Analysis
{
    public class DetailedMatch
    {
        public int Id { get; set; }
        public List<int> LeftLines { get; set; } = new();
        public List<int> RightLines { get; set; } = new();
        public string Type { get; set; } = "";
        public string Severity { get; set; } = "low";
        public List<Fragment>? Fragments { get; set; }

        public List<TokenInfo>? LeftTokens { get; set; }
        public List<TokenInfo>? RightTokens { get; set; }
    }

    public class Fragment
    {
        public List<int> LeftCols { get; set; } = new();
        public List<int> RightCols { get; set; } = new();
    }

    public class TokenInfo
    {
        public string Value { get; set; }
        public int Line { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
    }
}
