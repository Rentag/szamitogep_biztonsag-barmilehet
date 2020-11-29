using System.ComponentModel.DataAnnotations;

namespace Webshop.Models.AccountViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "You must enter a valid username.")]
        [RegularExpression("^[A-Za-z0-9_-]{5,40}$", ErrorMessage = "Invalid username")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "You must enter a valid password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
