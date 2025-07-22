using Microsoft.AspNetCore.Components.Forms;

namespace MyAssistant.Data
{
    public class UploadedFile
    {
        public string Name { get; set; } = "";
        public long Size { get; set; }
        public string ContentType { get; set; } = "";
        public static UploadedFile FromBrowserFile(IBrowserFile file)
        {
            return new UploadedFile
            {
                Name = file.Name,
                Size = file.Size,
                ContentType = file.ContentType
            };
        }
    }
}
