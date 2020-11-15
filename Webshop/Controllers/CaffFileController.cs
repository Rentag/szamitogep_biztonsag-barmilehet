using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Webshop.Authorization;
using Webshop.Data;
using Webshop.Models;
using Webshop.ViewModels.CaffFileViewModels;

namespace Webshop.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class CaffFileController : Controller
    {

        private readonly WebshopContext _context;
        private readonly UserManager<SiteUser> _userManager;
        private readonly IAuthorizationService _authorizationService;


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
                    var caffPath = VerifyAndUploadCaffFile(caff.Content);
                    // TODO: Process the CAFF file and get the result CIFF
                    // TODO: Add the CIFF to PNG converter function here that returns the path of the generated image
                    var imagePath = "pathFromConverterFunction";

                    newCaffFile.Path = caffPath;
                    newCaffFile.ImagePath = imagePath;
                    _context.CaffFiles.Add(newCaffFile);
                    await _context.SaveChangesAsync();

                }
                return RedirectToAction(nameof(Index));

            }
            return View(caff);
        }

        public String VerifyAndUploadCaffFile(IFormFile caff)
        {
            return "pathFromUploaderFunction";
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

    }
}
