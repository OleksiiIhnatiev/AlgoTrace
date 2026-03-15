namespace AlgoTrace.Server.Models.DTO.Directory
{
    public class FolderContentDto
    {
        public Guid? FolderId { get; set; }
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        public IEnumerable<FolderDto> Folders { get; set; }
        public IEnumerable<FileDto> Files { get; set; }
    }
}
