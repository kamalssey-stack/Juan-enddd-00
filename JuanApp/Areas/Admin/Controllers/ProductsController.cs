using JuanApp.Areas.Admin.ViewModels.ProductVM;
using JuanApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace JuanApp.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class ProductsController(IProductService service, ICategoryService categories) : Controller
{
 public async Task<IActionResult> Index() => View(await service.getAllAdminAsync());
 public async Task<IActionResult> Detail(int id) { var item=await service.getDetailAdminAsync(id); return item is null?NotFound():View(item); }
 private async Task LoadCategories() => ViewBag.Categories=await categories.getActiveCategoriesAsync();
 [HttpGet] public async Task<IActionResult> Create() { await LoadCategories(); return View(new ProductCreateVM()); }
 [HttpPost] public async Task<IActionResult> Create(ProductCreateVM model) { if(!ModelState.IsValid){await LoadCategories();return View(model);} await service.createAsync(model,$"{Request.Scheme}://{Request.Host}"); TempData["AdminNotice"]="Product added; subscribers were notified."; return RedirectToAction(nameof(Index)); }
 [HttpGet] public async Task<IActionResult> Edit(int id) { var model=await service.getForUpdateAsync(id); if(model is null)return NotFound(); await LoadCategories(); return View(model); }
 [HttpPost] public async Task<IActionResult> Edit(ProductUpdateVM model) { if(!ModelState.IsValid){await LoadCategories();return View(model);} if(!await service.updateAsync(model))return NotFound(); TempData["AdminNotice"]="Product updated."; return RedirectToAction(nameof(Index)); }
 [HttpPost] public async Task<IActionResult> Delete(int id) { await service.deleteAsync(id); TempData["AdminNotice"]="Product removed."; return RedirectToAction(nameof(Index)); }
}
