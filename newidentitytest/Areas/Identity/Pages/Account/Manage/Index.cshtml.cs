// Copilot: Fixed CS8618 and CS8601 nullability warnings.
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using newidentitytest.Models;

namespace newidentitytest.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            Username = string.Empty;
            Email = string.Empty;
            StatusMessage = string.Empty;
            Input = new InputModel();
        }

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user) ?? string.Empty;
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangeUsernameAsync(string newUsername)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (string.IsNullOrWhiteSpace(newUsername))
            {
                StatusMessage = "Error: Username cannot be empty.";
                return RedirectToPage();
            }

            var setUserNameResult = await _userManager.SetUserNameAsync(user, newUsername);
            if (!setUserNameResult.Succeeded)
            {
                StatusMessage = "Error: Unable to change username.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your username has been updated successfully";
            return RedirectToPage();
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            Username = await _userManager.GetUserNameAsync(user) ?? string.Empty;
            Email = await _userManager.GetEmailAsync(user) ?? string.Empty;
            Input = new InputModel
            {
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user) ?? string.Empty
            };
        }
    }
}
