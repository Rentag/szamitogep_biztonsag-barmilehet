using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Webshop.Authorization;
using Webshop.Data;
using Webshop.Models;
using Webshop.ViewModels.CaffFileViewModels;
using Ciff;

using SixLabors.ImageSharp;
namespace Webshop.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class CaffFileController : Controller
    {

        private readonly WebshopContext _context;
        private readonly UserManager<SiteUser> _userManager;
        private readonly IAuthorizationService _authorizationService;

        //for caff validation
        private string[] permittedExtensions = { ".caff" };
        private static readonly Dictionary<string, List<byte[]>> _fileSignature =
                                                new Dictionary<string, List<byte[]>>
        {
            { ".caff", new List<byte[]>
                {
                    new byte[] { 0x43, 0x41, 0x46, 0x46}
                }
            },
        };
        //20MB
        private static long fileSizeLimit = 20000 * 1024;

        public CaffFileController(WebshopContext context, UserManager<SiteUser> userManager, IAuthorizationService authorizationService)
        {
            _context = context;
            _userManager = userManager;
            _authorizationService = authorizationService;
        }
        

        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var caffFiles = from c in _context.CaffFiles.Include(c => c.User) select c;

            if (!String.IsNullOrEmpty(searchString))
            {
                caffFiles = caffFiles.Where(c => c.User.UserName.Contains(searchString));
            }
            return View("Index", await caffFiles.AsNoTracking().ToListAsync());
        }

        public IActionResult List()
        {
            return View("List", _context.CaffFiles.Where(c => c.UserId == int.Parse(_userManager.GetUserId(User))));
        }

        public IActionResult Create()
        {
            return View();
        }

        // POST: CaffFile/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CaffFileViewModel caff)
        {
            if (ModelState.IsValid)
            {
                CaffFile newCaffFile = new CaffFile
                {
                    UserId = int.Parse(_userManager.GetUserId(User)),
                    Comment = caff.Comment
                };

                var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                User, newCaffFile,
                                                CaffFileOperations.Create);
                if (!isAuthorized.Succeeded)
                {
                    return Unauthorized();
                }
                
                if (caff.Content != null)
                {
                    // TODO: Add the file uploader function here that returns the path of the uploaded CAFF file
                    var verified = VerifyAndUploadCaffFileAsync(caff.Content);
                    var caffPath = verified[0];
                    if (caffPath == "")
                    {
                        return View("Error");
                    }
                    // TODO: Process the CAFF file and get the result CIFF
                    // TODO: Add the CIFF to PNG converter function here that returns the path of the generated image
                    var imagePath = verified[1];

                    newCaffFile.Path = caffPath;
                    newCaffFile.ImagePath = imagePath;
                    _context.CaffFiles.Add(newCaffFile);
                   
                    await _context.SaveChangesAsync();

                }
                
                return RedirectToAction(nameof(Index));

            }
            return View(caff);
        }

        public  List<string> VerifyAndUploadCaffFileAsync(IFormFile formFile)
        {
            var filePaths = new List<string>();
            var ext = Path.GetExtension(formFile.FileName).ToLowerInvariant();
            bool isvalid = true;
            if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
            {
                isvalid = false;
                ViewBag.Failure = "File upload was unsuccessful due to not appropriate file extension.";
              //  return View("Error");
            }

            //Check Magic bytes
            using (var reader = new BinaryReader(formFile.OpenReadStream()))
            {
                var signatures = _fileSignature[ext];
                var all_headerBytes = reader.ReadBytes(signatures.Max(m => m.Length + 9));
                byte[] headerBytes = new byte[signatures[0].Length];
                for (int i = 0; i < all_headerBytes.Length; i++)
                {
                    if (i > 8)
                    {
                        headerBytes[i - 9] = all_headerBytes[i];
                    }
                }
                
                if (!(signatures.Any(signature =>
                    headerBytes.Take(signature.Length + 9).SequenceEqual(signature))))
                {
                    isvalid = false;
                    ViewBag.Failure = "File upload was unsuccessful due to not appropriate magic numbers.";
                    //return View("Error");
                }
            }
            //Check if not null and length is maximumom of x bytes.


            if (formFile.Length > 0 && formFile.Length <= fileSizeLimit)
            {
                if (isvalid)
                {
                    var random = Path.GetRandomFileName();
                    var replaced = random.Split(".")[0] + ".caff";
                    
                    //filePaths[0] - CAFF PATH
                    //filePaths[1] - PNG PATH
                    filePaths.Add(Path.Combine("\\Uploads\\CAFF\\", replaced));
                    var caffToCiff = "dll";
                    var path = "path";

                    //TODO: DYNAMIC
                    filePaths.Add("\\images\\uploads\\"+ random.Split(".")[0] + ".png");
                    var ciffToPng = new ciff();
                    
                    ciffToPng.cifftopng(Directory.GetCurrentDirectory()+"\\Uploads\\CIFF\\something.ciff",
                                    Directory.GetCurrentDirectory() + "\\wwwroot\\images\\uploads\\" + random.Split(".")[0] + ".png");
                    using (var stream = System.IO.File.Create(Directory.GetCurrentDirectory() + filePaths[0]))
                    {
                        formFile.CopyToAsync(stream);
                    }
                    //return filePaths;
                }
            }
            else
            {
                ViewBag.Failure = "File upload was unsuccessful due to not appropriate file size.";
                //return View("Error");
            }
            return filePaths;
        }

        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            CaffFile caffFileToEdit = GetCaffFile(id);
            if (caffFileToEdit == null)
            {
                return NotFound();
            }

            var isAuthorized = _authorizationService.AuthorizeAsync(User, caffFileToEdit, CaffFileOperations.Update);
            if (!isAuthorized.Result.Succeeded)
            {
                return Unauthorized();
            }
            return View(caffFileToEdit);
        }

        // POST: CaffFile/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CaffFile editedCaffFile)
        {
            if (ModelState.IsValid)
            {
                var originalCaffFile = GetCaffFile(editedCaffFile.Id);
                if (originalCaffFile == null)
                {
                    return NotFound();
                }

                var isAuthorized = _authorizationService.AuthorizeAsync(User, originalCaffFile, CaffFileOperations.Update);
                if (!isAuthorized.Result.Succeeded)
                {
                    return Unauthorized();
                }

                originalCaffFile.Comment = editedCaffFile.Comment;
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(editedCaffFile);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var caffToDelete = GetCaffFile(id);
            if (caffToDelete == null)
            {
                return NotFound();
            }
            
            var isAuthorized = _authorizationService.AuthorizeAsync(User, caffToDelete, CaffFileOperations.Delete);
            if (!isAuthorized.Result.Succeeded)
            {
                return Unauthorized();
            }
            //string[] files = Directory.GetFiles("");
           


            return View(caffToDelete);
        }

        // POST: CaffFile/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var originalCaffFile = GetCaffFile(id);
            if (originalCaffFile == null)
            {
                return NotFound();
            }

            var isAuthorized = _authorizationService.AuthorizeAsync(User, originalCaffFile, CaffFileOperations.Delete);
            if (!isAuthorized.Result.Succeeded)
            {
                return Unauthorized();
            }
            var paths = new List<string>();
            
            //Delete file physically
            paths.Add(originalCaffFile.ImagePath);
            paths.Add(originalCaffFile.Path);
            paths.Add(originalCaffFile.Path.Replace("CAFF", "CIFF"));
            System.IO.File.Delete(Directory.GetCurrentDirectory()+ "\\wwwroot" + paths[0]);
            System.IO.File.Delete(Directory.GetCurrentDirectory() + paths[1]);
            System.IO.File.Delete(Directory.GetCurrentDirectory() + paths[2]);
            
            
            
            _context.CaffFiles.Remove(originalCaffFile);
            await _context.SaveChangesAsync();
            // TODO: Delete CAFF and PNG files as well
            return RedirectToAction(nameof(Index));
        }

        public CaffFile GetCaffFile(int? caffId)
        {
            if (caffId == null)
                return null;

            return _context.CaffFiles
                .FirstOrDefault(Caff => Caff.Id == caffId);
        }

        // TODO: Download CAFF file
        public async Task<IActionResult> Download(int? id)
        {
            if (id == null)
                return View("Error");

           
            var path =  Directory.GetCurrentDirectory() + GetCaffFile(id).Path;

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory,GetContentType(path), Path.GetFileName(path));
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".caff", "text/plain"}
            };
        }
    }
}
