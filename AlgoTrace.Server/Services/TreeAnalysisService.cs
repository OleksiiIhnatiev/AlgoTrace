using AlgoTrace.Server.Algorithms.Tree;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.ParserFactory;

namespace AlgoTrace.Server.Services
{
    public class TreeAnalysisService : ITreeAnalysisService
    {
        private readonly IEnumerable<ITreeAlgorithm> _algorithms;
        private readonly IEnumerable<ICodeParser> _parsers;

        public TreeAnalysisService(IEnumerable<ITreeAlgorithm> algorithms, IEnumerable<ICodeParser> parsers)
        {
            _algorithms = algorithms;
            _parsers = parsers;
        }

        public AnalysisResponse Analyze(AnalysisRequest request)
        {
            return new AnalysisResponse();
        }
    }
}