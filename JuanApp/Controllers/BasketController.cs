using JuanApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JuanApp.Controllers;

public class BasketController(IBasketService basketService, IProductService products) : Controller
{
    public async Task<IActionResult> Index() => View(await basketService.getBasketAsync());

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int count = 1)
    {
        if (count < 1 || count > 50) return BadRequest("Count must be between 1 and 50.");
        if (await products.getOneAsync(productId) is null) return NotFound();
        await basketService.addToBasketAsync(productId, count);
        TempData["Notice"] = "Added to your bag.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ChangeCount(int productId, int count)
    {
        if (count > 50) return BadRequest("Maximum quantity is 50.");
        await basketService.changeCountAsync(productId, count);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int productId)
    {
        await basketService.removeFromBasketAsync(productId);
        return RedirectToAction(nameof(Index));
    }
}
