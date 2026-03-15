using Microsoft.AspNetCore.Identity;

namespace AlgoTrace.Server.Models
{
    public class User : IdentityUser
    {
        public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
        public virtual ICollection<SourceFile> Files { get; set; } = new List<SourceFile>();
    }
}