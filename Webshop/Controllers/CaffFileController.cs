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
using System.Diagnostics;

using SixLabors.ImageSharp;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Webshop.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class CaffFileController : Controller
    {

        private readonly WebshopContext _context;
        private readonly UserManager<SiteUser> _userManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly object balanceLock = new object();

       /// <summary>
       /// CAFF file extension
       /// </summary>
        private string[] permittedExtensions = { ".caff" };
        /// <summary>
        /// CAFF files signatures: \x43\x41\x46\x46
        /// </summary>
        private static readonly Dictionary<string, List<byte[]>> _fileSignature =
                                                new Dictionary<string, List<byte[]>>
        {
            { ".caff", new List<byte[]>
                {
                    new byte[] { 0x43, 0x41, 0x46, 0x46}
                }
            },
        };
        /// <summary>
        /// Maximum upload size: 20MB
        /// </summary>
        private static long fileSizeLimit = 20000 * 1024;

        private readonly ILogger<CaffFileController> _logger;

        [DllImport("ciffgen.dll")]
        public static extern void generateCiff(string caffSrc, string ciffDst);
        public CaffFileController(WebshopContext context, UserManager<SiteUser> userManager, IAuthorizationService authorizationService, ILogger<CaffFileController> logger)
        {
            _context = context;
            _userManager = userManager;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        /// <summary>
        /// Shows all of the uploaded files, optionally filtered by creator
        /// </summary>
        /// <param name="searchString">name of the file's creator</param>
        /// <returns>A view with the filtered files</returns>
        public async Task<IActionResult> Index(string searchString)
        {
            _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The /Index page has been accessed.");
            ViewData["CurrentFilter"] = searchString;
            var caffFiles = from c in _context.CaffFiles.Include(c => c.User) select c;

            if (!String.IsNullOrEmpty(searchString))
            {
                caffFiles = caffFiles.Where(c => c.User.UserName.Contains(searchString));
            }
            return View("Index", await caffFiles.AsNoTracking().ToListAsync());
        }
        
        /// <summary>
        /// Collects the uploaded files for the logged in user
        /// </summary>
        /// <returns>A view with the uploaded files</returns>
        public IActionResult List()
        {
            _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The /List page has been accessed.");
            return View("List", _context.CaffFiles.Where(c => c.UserId == int.Parse(_userManager.GetUserId(User))));
        }

        public IActionResult Create()
        {
            _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The /Create page has been accessed.");
            return View();
        }

        /// <summary>
        /// Handles the file upload functionality and save files into the database.
        /// </summary>
        /// <param name="caff">The model of the uploaded caff file.</param>
        /// <returns></returns>
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
                    var verified =await VerifyAndUploadCaffFileAsync(caff.Content);
                   
                    
                    if (verified == null)
                    {
                        return View("Error");
                    }
                    var caffPath = verified[0];
                    var imagePath = verified[2];

                    newCaffFile.Path = caffPath;
                    newCaffFile.ImagePath = imagePath;
                    _context.CaffFiles.Add(newCaffFile);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: Uploaded a new CAFF. CaffPath: " + caffPath);
                    _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: Uploaded a new CAFF. ImagePath: " + imagePath);
                }

                return RedirectToAction(nameof(Index));

            }
            _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The model is invalid.");

            return View(caff);
        }

        /// <summary>
        /// Check whether CAFF file meets the requirements (maximum size, signature, extension, being not null).
        /// Use the native component(ciffgen.dll) in order to create ciff files from caff.
        /// Use the Ciff assembly to create png file from ciff.
        /// </summary>
        /// <param name="formFile">Uploaded file.</param>
        /// <returns>Return with a list that includes the path of CAFF,CIFF and PNG file.</returns>
        public async Task<List<string>> VerifyAndUploadCaffFileAsync(IFormFile formFile)
        {
            var filePaths = new List<string>();
            var ext = Path.GetExtension(formFile.FileName).ToLowerInvariant();
            bool isvalid = true;
            if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
            {
                isvalid = false;
                _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The uploaded file .caff is not valid because the file is empty.");
                ViewBag.Failure = "File upload was unsuccessful due to not appropriate file extension.";
                return null;
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
                    _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The uploaded file .caff is not valid because the signature is incorrect.");

                    ViewBag.Failure = "File upload was unsuccessful due to not appropriate magic numbers.";
                    return null;
                }
            }
            //Check if not null and length is maximumom of x bytes.


            if (formFile.Length > 0 && formFile.Length <= fileSizeLimit)
            {
                if (isvalid)
                {
                    var random = Path.GetRandomFileName();
                    var replaced = random.Split(".")[0] + ".caff";
                    var replaced_ciff = random.Split(".")[0] + ".ciff";

                    filePaths.Add(Path.Combine("\\Uploads\\CAFF\\", replaced));
                    filePaths.Add(Path.Combine("\\Uploads\\CIFF\\", replaced_ciff));
                    filePaths.Add("\\images\\uploads\\" + random.Split(".")[0] + ".png");


                    using (var stream = System.IO.File.Create(Directory.GetCurrentDirectory() + filePaths[0]))
                    {
                        await formFile.CopyToAsync(stream);
                    }

                    generateCiff(Directory.GetCurrentDirectory()+filePaths[0], Directory.GetCurrentDirectory()+filePaths[1]);
                   
                  
                    var ciffToPng = new ciff();

                    ciffToPng.cifftopng(Directory.GetCurrentDirectory() + filePaths[1],
                                    Directory.GetCurrentDirectory() + "\\wwwroot\\images\\uploads\\" + random.Split(".")[0] + ".png");
                    
                    _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The uploaded file, " + random.Split(".")[0] + ".caff is valid.");
                   
                }
            }
            else
            {
                _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The uploaded file .caff is not valid because the file size is incorrect.");

                ViewBag.Failure = "File upload was unsuccessful due to not appropriate file size.";
                return null;
            }
            return filePaths;
        }

        /// <summary>
        /// Prepares the chosen caff file for editing
        /// </summary>
        /// <param name="id">The ID of the chonsen file</param>
        /// <returns></returns>
        public IActionResult Edit(int? id)
        {
            _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The /Edit page has been accessed.");

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
                _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The /Edit page hasn't been accessed because the user does not have the right to do so.");

                return Unauthorized();
            }
            return View(caffFileToEdit);
        }

        // POST: CaffFile/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CaffFile editedCaffFile)
        {
            var originalCaffFile = GetCaffFile(editedCaffFile.Id);
            if (ModelState.IsValid)
            {

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
                _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The file " + originalCaffFile.Path + " has been modified successfully.");
                return RedirectToAction("Index");
            }
            // var originalCaffFile = GetCaffFile(editedCaffFile.Id);
            _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The file " + originalCaffFile.Path + " hasn't been modified successfully.");
            return View(editedCaffFile);
        }
        /// <summary>
        /// Confirmation of deleting the previously chosen file.
        /// </summary>
        /// <param name="id">The ID of the chosen file.</param>
        /// <returns></returns>
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
                _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The /Delete page hasn't been accessed. File: " + caffToDelete.Path);

                return Unauthorized();
            }
            //string[] files = Directory.GetFiles("");

            _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The /Delete page has been accessed.");


            return View(caffToDelete);
        }

        /// <summary>
        /// Delete the file from database.
        /// Delete CAFF,CIFF and PNG from physical storage.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
                _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The user tried to access to the delete page of " + originalCaffFile.Path + "but has no right to do so. ");

                return Unauthorized();

            }
            var paths = new List<string>();

            paths.Add(originalCaffFile.ImagePath);
            paths.Add(originalCaffFile.Path);
            var s = originalCaffFile.Path.Replace("CAFF", "CIFF");
            paths.Add(originalCaffFile.Path.Replace("CAFF", "CIFF").Replace("caff","ciff"));
            if (System.IO.File.Exists(Directory.GetCurrentDirectory() + "\\wwwroot" + paths[0]))
            {
                System.IO.File.Delete(Directory.GetCurrentDirectory() + "\\wwwroot" + paths[0]);
            }
            if (System.IO.File.Exists(Directory.GetCurrentDirectory() + "\\wwwroot" + paths[1]))
            {
                System.IO.File.Delete(Directory.GetCurrentDirectory() + "\\wwwroot" + paths[1]);
            }
            if (System.IO.File.Exists(Directory.GetCurrentDirectory() + "\\wwwroot" + paths[2]))
            {
                System.IO.File.Delete(Directory.GetCurrentDirectory() + "\\wwwroot" + paths[2]);
            }
           
           
            _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The file " + originalCaffFile.Path + " has been deleted successfully.");


            _context.CaffFiles.Remove(originalCaffFile);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Get the CaffFile object with the given ID.
        /// </summary>
        /// <param name="caffId">The ID of the CaffFile.</param>
        /// <returns>Return with the CaffFile object.</returns>
        public CaffFile GetCaffFile(int? caffId)
        {
            if (caffId == null)
                return null;

            return _context.CaffFiles
                .FirstOrDefault(Caff => Caff.Id == caffId);
        }

        /// <summary>
        /// Download the CAFF with the given ID.
        /// </summary>
        /// <param name="id">The ID of the CAFF file that is about to be downloaded.</param>
        /// <returns>Return with the chosen CAFF file</returns>
        public async Task<IActionResult> Download(int? id)
        {
            if (id == null)
                return View("Error");


            var path = Directory.GetCurrentDirectory() + GetCaffFile(id).Path;

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            _logger.LogInformation("User : " + _userManager.GetUserName(User) + "  Action: The file " + path + " has been downloaded successfully.");

            return File(memory, GetContentType(path), Path.GetFileName(path));
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
