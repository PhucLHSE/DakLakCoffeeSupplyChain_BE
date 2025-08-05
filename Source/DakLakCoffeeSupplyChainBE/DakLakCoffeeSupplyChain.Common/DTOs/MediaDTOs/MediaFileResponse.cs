using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs
{
    public class MediaFileResponse
    {
        public Guid MediaId { get; set; }
        public string MediaType { get; set; }
        public string MediaUrl { get; set; }
        public string Caption { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
