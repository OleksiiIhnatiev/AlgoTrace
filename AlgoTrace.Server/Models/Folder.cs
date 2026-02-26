namespace AlgoTrace.Server.Models
{
    public class Folder
    {
        public int FolderId { get; set; }
        public string Name { get; set; }

        public string UserId { get; set; }
        public virtual User User { get; set; }

        public int? ParentId { get; set; }
        public virtual Folder Parent { get; set; }
        public virtual ICollection<Folder> SubFolders { get; set; } = new List<Folder>();

        public virtual ICollection<File> Files { get; set; } = new List<File>();
    }
}
