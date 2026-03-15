namespace AlgoTrace.Server.Models.DTO.Directory
{
    public class FolderDto
    {
        public Guid FolderId { get; set; }
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
    }
}
