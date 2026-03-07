using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Models.DTO
{
    public class AnalysisInfo
    {
        public int OverallScore { get; set; }
        public string Mode { get; set; } = "";
        public string Date { get; set; } = "";
    }
}
