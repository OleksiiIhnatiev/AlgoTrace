using AlgoTrace.Server.Models.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Interfaces
{
    public interface ITextAlgorithm
    {
        string Name { get; }
        List<DetailedMatch> Execute(string source, string target, out double similarityScore);
    }
}