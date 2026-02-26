namespace AlgoTrace.Server.Models
{
    public class File
    {
        public int FileId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }

        public int FolderId { get; set; }
        public virtual Folder Folder { get; set; }
    }
}
