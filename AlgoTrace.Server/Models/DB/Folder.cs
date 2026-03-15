namespace AlgoTrace.Server.Models
{
    public class Folder
    {
        public Guid FolderId { get; set; } = Guid.NewGuid();
        public string Name { get; set; }

        public string UserId { get; set; }
        public virtual User User { get; set; }

        public Guid? ParentId { get; set; }
        public virtual Folder Parent { get; set; }
        public virtual ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
        public virtual ICollection<SourceFile> Files { get; set; } = new List<SourceFile>();
    }
}