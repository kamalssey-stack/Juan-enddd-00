using JuanApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JuanApp.Controllers;

public class SubscribeController(ISubscriberService subscribers) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Index(string email)
    {
        var result = await subscribers.subscribeAsync(email);
        TempData[result.Success ? "SubscribeNotice" : "SubscribeError"] = result.Message;
        return RedirectToAction("Index", "Home");
    }
}
