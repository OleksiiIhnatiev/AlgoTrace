namespace AlgoTrace.Server.Models
{
    public class SourceFile
    {
        public Guid FileId { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Path { get; set; }

        public string UserId { get; set; }
        public virtual User User { get; set; }

        public Guid? FolderId { get; set; }
        public virtual Folder? Folder { get; set; }
    }
}
