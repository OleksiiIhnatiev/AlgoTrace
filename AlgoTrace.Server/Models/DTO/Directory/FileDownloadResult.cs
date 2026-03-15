namespace AlgoTrace.Server.Models.DTO
{
    public class FileDownloadResult
    {
        public byte[] Bytes { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
}
