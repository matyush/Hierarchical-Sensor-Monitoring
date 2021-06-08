﻿using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using HSMServer.Authentication;
using HSMServer.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using HSMServer.Constants;
using HSMServer.Model.Validators;
using System.Linq;
using HSMServer.Model.ViewModel;
using HSMServer.MonitoringServerCore;

namespace HSMServer.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IUserManager _userManager;
        private readonly IMonitoringCore _monitoringCore;

        public AccountController(IUserManager userManager, IMonitoringCore monitoringCore)
        {
            _userManager = userManager;
            _monitoringCore = monitoringCore;
        }

        [AllowAnonymous]
        [ActionName(nameof(Index))]
        public IActionResult Index()
        {
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [ActionName(nameof(Authenticate))]
        [Consumes("application/x-www-form-urlencoded")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Authenticate([FromForm]LoginViewModel model)
        {
            LoginValidator validator = new LoginValidator(_userManager);
            var results = validator.Validate(model);
            if (!results.IsValid) 
            {
                TempData[TextConstants.TempDataErrorText] = ValidatorHelper.GetErrorString(results.Errors);
                return RedirectToAction("Index", "Home");
            }

            //var user = _userManager.Authenticate(model.Login, model.Password);
            //if (user == null) return RedirectToAction("Index", "Home");

            TempData.Remove(TextConstants.TempDataErrorText);
            await Authenticate(model.Login, model.KeepLoggedIn);

            return RedirectToAction("Index", "Home");
        }

        public void Logout()
        {
            TempData.Remove(TextConstants.TempDataErrorText);
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            RedirectToAction("Index", "Home");
        }

        public IActionResult Users()
        {
            var users = _userManager.Users;
            var products = _monitoringCore.GetProducts(HttpContext.User as User);

            UsersListPageViewModel viewModel = new UsersListPageViewModel();
            viewModel.Users = users.Select(x => new UserViewModel(x))?.ToList();
            viewModel.Products = products.Select(x => new ProductViewModel(x)).ToList();

            return View(viewModel);
        }

        private async Task Authenticate(string login, bool keepLoggedIn)
        {
            var claims = new List<Claim> { new Claim(ClaimsIdentity.DefaultNameClaimType, login) };
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie",
                ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            AuthenticationProperties properties = new AuthenticationProperties();
            properties.IsPersistent = keepLoggedIn;
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id),
                properties);
  
        }
    }
}
