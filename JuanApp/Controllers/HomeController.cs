using JuanApp.Data;
using JuanApp.Models;
using JuanApp.Services.Interfaces;
using JuanApp.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JuanApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProductService _products;
    private readonly JuanDbContext _db;

    public HomeController(ILogger<HomeController> logger, IProductService products, JuanDbContext db)
    {
        _logger = logger;
        _products = products;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new HomeVM
        {
            Sliders = await _db.Sliders.OrderBy(s => s.OrderNumber).ToListAsync(),
            FeaturedProducts = await _products.getFeaturedAsync(8),
            NewProducts = await _products.getNewArrivalsAsync(8),
            BestSellerProducts = await _products.getBestSellersAsync(8)
        };
        return View(vm);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
