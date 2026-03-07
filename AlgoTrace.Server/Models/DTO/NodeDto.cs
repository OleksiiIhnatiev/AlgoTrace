using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Models.DTO
{
    public class NodeDto
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "file";
        public List<NodeDto>? Children { get; set; }

        //Fields for nodes with "file" type
        public int? Score { get; set; }
        public string? Path { get; set; }
        public Dictionary<string, int>? ReferenceScores { get; set; }
        public Dictionary<string, List<DetailedMatch>>? DetailedMatches { get; set; }
    }
}
