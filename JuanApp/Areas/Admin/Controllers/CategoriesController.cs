using JuanApp.Areas.Admin.ViewModels.CategoryVM;
using JuanApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace JuanApp.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class CategoriesController(ICategoryService service) : Controller
{
 public async Task<IActionResult> Index() => View(await service.getAllAsync());
 [HttpGet] public IActionResult Create() => View(new CategoryCreateVM());
 [HttpPost] public async Task<IActionResult> Create(CategoryCreateVM model) { if(!ModelState.IsValid)return View(model); await service.createAsync(model); TempData["AdminNotice"]="Category added."; return RedirectToAction(nameof(Index)); }
 [HttpGet] public async Task<IActionResult> Edit(int id) { var model=await service.getForUpdateAsync(id); return model is null?NotFound():View(model); }
 [HttpPost] public async Task<IActionResult> Edit(CategoryUpdateVM model) { if(!ModelState.IsValid)return View(model); if(!await service.updateAsync(model))return NotFound(); TempData["AdminNotice"]="Category updated."; return RedirectToAction(nameof(Index)); }
 [HttpPost] public async Task<IActionResult> Delete(int id) { await service.deleteAsync(id); TempData["AdminNotice"]="Category removed."; return RedirectToAction(nameof(Index)); }
}
