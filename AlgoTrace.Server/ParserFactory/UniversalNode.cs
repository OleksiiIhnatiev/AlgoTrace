using System.Collections.Generic;
using System.Linq;
using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.ParserFactory
{
    public class UniversalNode
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public List<UniversalNode> Children { get; set; } = new();
        public CodeLocation Location { get; set; }

        public IEnumerable<UniversalNode> Flatten()
        {
            return new[] { this }.Concat(Children.SelectMany(c => c.Flatten()));
        }
    }
}
