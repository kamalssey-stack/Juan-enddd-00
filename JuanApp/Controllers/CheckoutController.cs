using System.Security.Claims;
using JuanApp.Services.Interfaces;
using JuanApp.ViewModels.Checkout;
using Microsoft.AspNetCore.Mvc;

namespace JuanApp.Controllers;

public class CheckoutController(IBasketService basketService, IOrderService orderService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var basket = await basketService.getBasketAsync();
        if (!basket.Items.Any()) return RedirectToAction("Index", "Basket");
        return View(new CheckoutVM { Basket = basket });
    }

    [HttpPost]
    public async Task<IActionResult> Index(CheckoutVM model)
    {
        model.Basket = await basketService.getBasketAsync();
        if (!ModelState.IsValid || !model.Basket.Items.Any()) return View(model);
        try
        {
            var order = await orderService.createOrderAsync(model, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData["OrderNumber"] = order.OrderNumber;
            TempData["OrderTotal"] = order.TotalAmount.ToString("0.00");
            return RedirectToAction(nameof(Complete));
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(model); }
    }

    [HttpGet]
    public IActionResult Complete() => View();
}
