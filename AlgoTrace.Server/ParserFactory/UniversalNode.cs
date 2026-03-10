namespace AlgoTrace.Server.ParserFactory
{
    public class UniversalNode
    {
        public string Type { get; set; } = "";
        public string Value { get; set; } = "";
        public List<UniversalNode> Children { get; set; } = new();

        public IEnumerable<UniversalNode> Flatten()
        {
            return new[] { this }.Concat(Children.SelectMany(x => x.Flatten()));
        }
    }
}
