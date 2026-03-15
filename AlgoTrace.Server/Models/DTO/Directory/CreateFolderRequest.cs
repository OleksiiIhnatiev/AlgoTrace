namespace AlgoTrace.Server.Models.DTO
{
    public class CreateFolderRequest
    {
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
    }
}
