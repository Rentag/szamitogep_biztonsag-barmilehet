using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Webshop.ViewModels.CaffFileViewModels
{
    public class CaffFileViewModel
    {
        public string Comment { get; set; }
        [Required]
        public IFormFile Content { get; set; }
    }
}
