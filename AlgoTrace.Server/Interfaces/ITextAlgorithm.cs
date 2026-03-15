using AlgoTrace.Server.Models.DTO.Analysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Interfaces
{
    public interface ITextAlgorithm
    {
        string Key { get; }
        string Name { get; }
        List<DetailedMatch> Execute(string source, string target, out double similarityScore);
    }
}
