using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs
{
    public class MediaUploadRequest
    {
        public List<IFormFile> Files { get; set; }

        public string RelatedEntity { get; set; }
        public Guid RelatedId { get; set; }
    }
}
