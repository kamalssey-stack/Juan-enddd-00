using System.Security.Claims;
using System.Text.Encodings.Web;
using JuanApp.Models;
using JuanApp.Services.Interfaces;
using JuanApp.ViewModels.Account;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace JuanApp.Controllers;
public class AccountController(UserManager<AppUser> users, SignInManager<AppUser> signIn, IEmailService email, IOrderService orders, IConfiguration configuration, IWebHostEnvironment environment) : Controller
{
    [HttpGet] public IActionResult Login(string? returnUrl = null) { ViewBag.ReturnUrl = returnUrl; ViewBag.GoogleLoginEnabled = GoogleLoginEnabled; return View(new AccountLoginVM()); }
    [HttpPost] public async Task<IActionResult> Login(AccountLoginVM model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.GoogleLoginEnabled = GoogleLoginEnabled;
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByNameAsync(model.UsernameOrEmail) ?? await users.FindByEmailAsync(model.UsernameOrEmail);
        if (user is null) { ModelState.AddModelError(string.Empty, "The email or password is incorrect."); return View(model); }
        var configuredAdminEmail = configuration["SeedAdmin:Email"];
        var isConfiguredAdmin = !string.IsNullOrWhiteSpace(configuredAdminEmail)
            && string.Equals(user.Email, configuredAdminEmail, StringComparison.OrdinalIgnoreCase);
        if (isConfiguredAdmin && !await users.IsInRoleAsync(user, "Admin"))
            await users.AddToRoleAsync(user, "Admin");
        if (!await users.IsEmailConfirmedAsync(user))
        {
            if (!await users.CheckPasswordAsync(user, model.Password))
            {
                ModelState.AddModelError(string.Empty, "The email or password is incorrect.");
                return View(model);
            }

            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(VerifyEmail), "Account", new { email = user.Email, token }, Request.Scheme)!;
            var body = await ReadEmailTemplateAsync("emailConfirm.html", link, user.UserName ?? user.Email!);
            var emailSent = await email.SendEmailAsync(user.Email!, "Confirm Email", body);
            ModelState.AddModelError(string.Empty, emailSent
                ? "Your email is not confirmed. A new confirmation link has been sent."
                : "Your email is not confirmed, but the confirmation email could not be sent. Check Gmail SMTP settings.");
            return View(model);
        }
        var result = await signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Home")!);
        if (result.RequiresTwoFactor) return RedirectToAction(nameof(TwoFactor), new { model.RememberMe, returnUrl });
        ModelState.AddModelError(string.Empty, "The email or password is incorrect.");
        return View(model);
    }
    [HttpGet] public IActionResult Register() => View(new AccountRegisterVM());
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Register(AccountRegisterVM model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new AppUser { UserName = model.Username.Trim(), Email = model.Email.Trim(), FullName = model.FullName.Trim() };
        var result = await users.CreateAsync(user, model.Password);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model); }
        await users.AddToRoleAsync(user, "Member");
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var link = Url.Action(nameof(VerifyEmail), "Account", new { email = user.Email, token }, Request.Scheme)!;
        var body = await ReadEmailTemplateAsync("emailConfirm.html", link, user.UserName ?? user.Email!);
        var emailSent = await email.SendEmailAsync(user.Email!, "Confirm Email", body);
        TempData["AccountNotice"] = emailSent
            ? "Your account is ready. Check your email to confirm it before signing in."
            : "Your account was created, but the confirmation email was not sent. Configure Email:Username (Gmail address) and Email:Password (Google app password) in User Secrets, then resend the confirmation email.";
        return RedirectToAction(nameof(Login));
    }
    [HttpGet] public IActionResult ResendConfirmation() => View(new ForgotPasswordVM());

    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ResendConfirmation(ForgotPasswordVM model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        if (user is not null && !await users.IsEmailConfirmedAsync(user))
        {
            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(VerifyEmail), "Account", new { email = user.Email, token }, Request.Scheme)!;
            var body = await ReadEmailTemplateAsync("emailConfirm.html", link, user.UserName ?? user.Email!);
            await email.SendEmailAsync(user.Email!, "Confirm Email", body);
        }
        TempData["AccountNotice"] = "If this address belongs to an unconfirmed account, a new confirmation link has been sent.";
        return RedirectToAction(nameof(Login));
    }
    [HttpGet] public async Task<IActionResult> VerifyEmail(string email, string token)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null) return NotFound();
        var result = await users.ConfirmEmailAsync(user, token);
        ViewBag.Confirmed = result.Succeeded;
        return View("ConfirmEmail");
    }
    [HttpGet] public IActionResult ForgotPassword() => View(new ForgotPasswordVM());
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        if (user is not null)
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action(nameof(ResetPassword), "Account", new { userId = user.Id, token }, Request.Scheme)!;
            var body = await ReadEmailTemplateAsync("resetpassword.html", link, user.UserName ?? user.Email!);
            await email.SendEmailAsync(user.Email!, "Reset Password", body);
        }
        TempData["AccountNotice"] = "If the account exists, password reset instructions have been sent.";
        return RedirectToAction(nameof(Login));
    }
    [HttpGet] public IActionResult ResetPassword(string userId, string token) => View(new ResetPasswordVM { UserId = userId, Token = token });
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByIdAsync(model.UserId);
        if (user is null) { ModelState.AddModelError(string.Empty, "Invalid reset request."); return View(model); }
        var result = await users.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model); }
        TempData["AccountNotice"] = "Password updated. You can sign in now.";
        return RedirectToAction(nameof(Login));
    }
    [HttpGet] public IActionResult TwoFactor(bool rememberMe, string? returnUrl = null) { ViewBag.RememberMe = rememberMe; ViewBag.ReturnUrl = returnUrl; return View(new TwoFactorVM { RememberMe = rememberMe }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> TwoFactor(TwoFactorVM model, string? returnUrl = null)
    {
        ViewBag.RememberMe = model.RememberMe; ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(model);
        var result = await signIn.TwoFactorAuthenticatorSignInAsync(model.Code, model.RememberMe, model.RememberMachine);
        if (result.Succeeded) return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Home")!);
        ModelState.AddModelError(string.Empty, "That verification code was not accepted."); return View(model);
    }
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Security()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        var model = new TwoFactorSetupVM { IsEnabled = user.TwoFactorEnabled };
        if (!model.IsEnabled)
        {
            var key = await users.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key)) { await users.ResetAuthenticatorKeyAsync(user); key = await users.GetAuthenticatorKeyAsync(user); }
            model.SharedKey = key ?? string.Empty;
            model.AuthenticatorUri = $"otpauth://totp/Juan:{Uri.EscapeDataString(user.Email!)}?secret={key}&issuer=Juan&digits=6";
        }
        return View(model);
    }
    [HttpGet] public async Task<IActionResult> AuthenticatorSetup()
    {
        var user = await users.GetUserAsync(User); if (user is null) return Challenge();
        var key = await users.GetAuthenticatorKeyAsync(user); if (string.IsNullOrEmpty(key)) { await users.ResetAuthenticatorKeyAsync(user); key = await users.GetAuthenticatorKeyAsync(user); }
        return Json(new { sharedKey = key, authenticatorUri = $"otpauth://totp/Juan:{Uri.EscapeDataString(user.Email!)}?secret={key}&issuer=Juan&digits=6" });
    }
    [HttpPost] public async Task<IActionResult> EnableTwoFactor(string code)
    {
        var user = await users.GetUserAsync(User); if (user is null) return Challenge();
        var normalized = code.Replace(" ", "").Replace("-", "");
        if (!await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, normalized)) return BadRequest("Invalid authenticator code.");
        var result = await users.SetTwoFactorEnabledAsync(user, true);
        TempData[result.Succeeded ? "AccountNotice" : "AccountError"] = result.Succeeded ? "Two-factor authentication is enabled." : string.Join(" ", result.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Security));
    }
    [HttpPost] public IActionResult ExternalLogin(string provider = "Google")
    {
        if (!GoogleLoginEnabled) { TempData["AccountError"] = "Google sign-in is not configured yet."; return RedirectToAction(nameof(Login)); }
        var callback = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl = "/" });
        return Challenge(signIn.ConfigureExternalAuthenticationProperties(provider, callback), provider);
    }
    [HttpGet] public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (!string.IsNullOrEmpty(remoteError)) { TempData["AccountError"] = "External login failed."; return RedirectToAction(nameof(Login)); }
        var info = await signIn.GetExternalLoginInfoAsync(); if (info is null) return RedirectToAction(nameof(Login));
        var login = await signIn.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false, true);
        if (login.Succeeded) return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        var address = info.Principal.FindFirst(ClaimTypes.Email)?.Value; if (string.IsNullOrWhiteSpace(address)) return RedirectToAction(nameof(Login));
        var user = await users.FindByEmailAsync(address);
        if (user is null)
        {
            user = new AppUser { UserName = address, Email = address, EmailConfirmed = true, FullName = info.Principal.Identity?.Name ?? address };
            var created = await users.CreateAsync(user); if (!created.Succeeded) { TempData["AccountError"] = "Could not create the account."; return RedirectToAction(nameof(Login)); }
            await users.AddToRoleAsync(user, "Member");
        }
        var linked = await users.AddLoginAsync(user, info); if (!linked.Succeeded) { TempData["AccountError"] = "Could not link the external account."; return RedirectToAction(nameof(Login)); }
        await signIn.SignInAsync(user, false, info.LoginProvider); return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
    private bool GoogleLoginEnabled => !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) && !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]);

    [HttpPost] public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction("Index", "Home"); }
    public IActionResult AccessDenied() { Response.StatusCode = 403; return View(); }
    [HttpGet] public async Task<IActionResult> MyOrders() { var user = await users.GetUserAsync(User); return user is null ? Challenge() : Json(await orders.getUserOrdersAsync(user.Id)); }

    private async Task<string> ReadEmailTemplateAsync(string templateName, string url, string username)
    {
        var templatePath = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "templates", templateName);
        using StreamReader reader = new(templatePath);
        var body = await reader.ReadToEndAsync();
        body = body.Replace("{{url}}", HtmlEncoder.Default.Encode(url), StringComparison.Ordinal);
        body = body.Replace("{{username}}", HtmlEncoder.Default.Encode(username), StringComparison.Ordinal);
        return body;
    }
}




