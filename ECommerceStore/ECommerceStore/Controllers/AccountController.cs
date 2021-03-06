﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerceStore.Models;
using ECommerceStore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceStore.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;
        private IEmailSender _emailSender;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }
         //View
        public IActionResult Index()
        {
            
            return View();
        }

        // Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel lvm)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(lvm.Email, lvm.Password, isPersistent: false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(lvm.Email);

                    if (await _userManager.IsInRoleAsync(user, ApplicationRoles.Admin))
                    {
                        return RedirectToAction("Index", "Admin");
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Your Credential is Incorrect");
                }
            }

            return View();
        }

        // Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel rvm)
        {
            if (ModelState.IsValid)
            {
                
                ApplicationUser user = new ApplicationUser
                {
                    UserName = rvm.Email,
                    Email = rvm.Email,
                    FirstName = rvm.FirstName,
                    LastName = rvm.LastName,
                    Subscribe = rvm.Subscribe
                };

                var result = await _userManager.CreateAsync(user, rvm.Password);

                if (result.Succeeded)
                {
                    List<Claim> claimList = new List<Claim>();

                    Claim nameClaim = new Claim("FullName", $"{user.FirstName} {user.LastName}");
                    Claim emailClaim = new Claim(ClaimTypes.Email, user.Email);
                    Claim roleClaim = new Claim(ClaimTypes.Role, "Member");

                    if (user.Subscribe)
                    {
                        Claim subscribeClaim = new Claim("Subscription", $"{user.Subscribe}");
                        claimList.Add(subscribeClaim);
                    }

                    claimList.Add(nameClaim);
                    claimList.Add(emailClaim);
                    claimList.Add(roleClaim);

                    await _userManager.AddClaimsAsync(user, claimList);

                    await _userManager.AddToRoleAsync(user, ApplicationRoles.Member);

                    if (user.Email.Contains("@codefellows.com"))
                    {
                        await _userManager.AddToRoleAsync(user, ApplicationRoles.Admin);

                    }
                    await _signInManager.SignInAsync(user, false);
                    if (await _userManager.IsInRoleAsync(user, ApplicationRoles.Admin))
                    {

                        return RedirectToAction("Index", "Admin");
                    }

                    string msgTitle = "Thank you for Registering at RuckSack";
                    string msgContent = $"<div>" +
                                        $"<h2> Thank you {user.FirstName} {user.LastName} for registering at RuckSack! </h2>" +
                                         $"<p> Thanks man </p>" +
                                         $"</div>";

                    // Sends welcome email to newly registered user
                    await _emailSender.SendEmailAsync(user.Email, msgTitle, msgContent);

                    return RedirectToAction("Index", "Home");

                }
            }
                
            else
            {
                ModelState.AddModelError(string.Empty, "Your Credential Is Incorrect"); 
            }
                return View();
        }

        //Log Out
        public async Task<IActionResult> LogOut()
        {
            if (_signInManager.IsSignedIn(User))
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }


        // OAuth
        public IActionResult ExternalLogin(string provider)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string remoteError = null)
        {
            if(remoteError != null)
            {
                TempData["ErrorMessage"] = "Error from Proider";
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false, true);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var fullname = info.Principal.FindFirstValue(ClaimTypes.Name);
            string[] names = fullname.Split(" ");

            return RedirectToAction("ExternalLoginConfirmation", new ExternalLoginViewModel
            {
                Email = email,
                FirstName = names[0],
                LastName = names[1]
            });
        } 


        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel elvm)
        {
            if (ModelState.IsValid)
            {
                if (await _userManager.FindByEmailAsync(elvm.Email) != null)
                {
                    var savedUser = await _userManager.FindByEmailAsync(elvm.Email);
                    await _signInManager.SignInAsync(savedUser, false);
                    if (await _userManager.IsInRoleAsync(savedUser, ApplicationRoles.Admin))
                    {

                        return RedirectToAction("Index", "Admin");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                var info = await _signInManager.GetExternalLoginInfoAsync();

                if(info == null)    
                {
                    TempData["ErrorMessage"] = "Error Loading Information";
                }

                var user = new ApplicationUser
                {   
                    UserName = elvm.Email,
                    Email = elvm.Email,
                    FirstName = elvm.FirstName,
                    LastName = elvm.LastName
                };

                var result = await _userManager.CreateAsync(user);


                if (result.Succeeded)
                {
                    List<Claim> claimList = new List<Claim>();

                    Claim nameClaim = new Claim("FullName", $"{user.FirstName} {user.LastName}");
                    Claim emailClaim = new Claim(ClaimTypes.Email, user.Email);
                    Claim roleClaim = new Claim(ClaimTypes.Role, "Member");

                    if (user.Subscribe)
                    {
                        Claim subscribeClaim = new Claim("Subscription", $"{user.Subscribe}");
                        claimList.Add(subscribeClaim);
                    }

                    claimList.Add(nameClaim);
                    claimList.Add(emailClaim);
                    claimList.Add(roleClaim);

                    await _userManager.AddClaimsAsync(user, claimList);

                    await _userManager.AddToRoleAsync(user, ApplicationRoles.Member);

                    if (user.Email.Contains("@codefellows.com"))
                    {
                        await _userManager.AddToRoleAsync(user, ApplicationRoles.Admin);

                    }

                   await _signInManager.SignInAsync(user, false);

                    if (await _userManager.IsInRoleAsync(user, ApplicationRoles.Admin))
                    {

                        return RedirectToAction("Index", "Admin");
                    }

                    result = await _userManager.AddLoginAsync(user, info);

                    string msgTitle = "Thank you for Registering at RuckSack";
                    string msgContent = $"<div>" +
                                        $"<h2> Thank you {user.FirstName} {user.LastName} for registering at RuckSack! </h2>" +
                                         $"<p> Subscribe to our site for exclusive deals. </p>" +
                                         $"</div>";

                    // Sends welcome email to newly registered user
                    await _emailSender.SendEmailAsync(user.Email, msgTitle, msgContent);

                    return RedirectToAction("Index", "Home");
                    
                }
            }
            return RedirectToAction("Login");
        }
    }
}
