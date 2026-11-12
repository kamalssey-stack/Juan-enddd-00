using JuanApp.Services.Interfaces;
using JuanApp.ViewModels.Shop;
using Microsoft.AspNetCore.Mvc;

namespace JuanApp.Controllers;

public class ShopController(IProductService products, ICategoryService categories) : Controller
{
    public async Task<IActionResult> Index(int page = 1, int? categoryId = null, string? search = null, string? sortBy = null)
    {
        var vm = new ShopVM
        {
            Products = await products.getAllAsync(Math.Max(page, 1), 12, categoryId, search, sortBy),
            Categories = await categories.getActiveCategoriesAsync(),
            SelectedCategoryId = categoryId,
            Search = search,
            SortBy = sortBy
        };
        return View(vm);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await products.getOneAsync(id);
        if (product is null) return NotFound();
        return View(new ProductDetailVM { Product = product, RelatedProducts = await products.getRelatedProductsAsync(product.CategoryId, id) });
    }

    [HttpPost]
    public async Task<IActionResult> Rate(int productId, int rating, string? comment)
    {
        if (!User.Identity?.IsAuthenticated ?? true) return Challenge();
        if (rating is < 1 or > 5) return BadRequest("Rating must be from 1 to 5.");
        await products.addRatingAsync(productId, rating, comment, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, User.Identity?.Name ?? "Customer");
        return RedirectToAction(nameof(Detail), new { id = productId });
    }
}
