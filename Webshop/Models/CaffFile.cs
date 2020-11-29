using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Webshop.Models
{
    public class CaffFile
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public int UserId { get; set; }
        [DataType(DataType.MultilineText)]
        public string Comment { get; set; }
        public string ImagePath { get; set; }
        public SiteUser User { get; set; }
    }
}
