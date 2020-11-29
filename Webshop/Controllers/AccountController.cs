using System;
using System.Threading.Tasks;
using Webshop.Models;
using Webshop.Models.AccountViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Webshop.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<SiteUser> _userManager;
        private readonly SignInManager<SiteUser> _signInManager;
        private readonly ILogger<CaffFileController> _logger;
        public AccountController(
            UserManager<SiteUser> userManager,
            SignInManager<SiteUser> signInManager,
            ILogger<CaffFileController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Registers the new user
        /// </summary>
        /// <param name="user">UserViewModel with the user's login data</param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken()]
        public async Task<IActionResult> Register(RegisterViewModel user, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                SiteUser SiteUser = new SiteUser
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    Name = user.Name

                };
                var result = await _userManager.CreateAsync(SiteUser, user.Password);
                if (result.Succeeded)
                {
                    // Add a user to the default role
                    await _userManager.AddToRoleAsync(SiteUser, "User");

                    await _signInManager.SignInAsync(SiteUser, isPersistent: false);
                    if (!String.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        _logger.LogInformation("The user: " + _userManager.GetUserName(User) + " page has been registered.");
                        return Redirect(returnUrl);
                    }
                    else
                    {
                       

                        return RedirectToAction("Index", "CaffFile");
                    }
                }
                AddErrors(result);
            }
            

            // If we got this far, something failed, redisplay form
            return View(user);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
         //   _logger.LogInformation("The LOGIN page has been accessed");
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// User login
        /// </summary>
        /// <param name="user">LoginViewModel with the user's login credentials</param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel user, string returnUrl = null)
        {
            _logger.LogInformation("The LOGGED IN page has been accessed");
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(user.UserName, user.Password, user.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    if (!String.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        _logger.LogInformation("The user: " + user.UserName + " page has been logged in.");

                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "CaffFile");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    _logger.LogInformation("Invalid login attempt with: "+user.UserName);

                    return View(user);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(user);
        }

        /// <summary>
        /// User logout
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("The user: " + _userManager.GetUserName(User) + " has been logged out.");
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "CaffFile");
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}