using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs
{
    public class UploadImageResult
    {
        public string Url { get; set; }
        public string PublicId { get; set; }
        public string FileType { get; set; } 
        public long FileSize { get; set; }
        public string MediaType { get; set; }
    }
}
