using JuanApp.Areas.Admin.ViewModels.ProductVM;
using JuanApp.Services.Implements;
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
 [HttpPost] public async Task<IActionResult> Create(ProductCreateVM model) { await ValidateImagesAsync(model.MainImage, nameof(model.MainImage), model.HoverImage, nameof(model.HoverImage), model.AdditionalImages, nameof(model.AdditionalImages)); if(!ModelState.IsValid){await LoadCategories();return View(model);} await service.createAsync(model,$"{Request.Scheme}://{Request.Host}"); TempData["AdminNotice"]="Product added; subscribers were notified."; return RedirectToAction(nameof(Index)); }
 [HttpGet] public async Task<IActionResult> Edit(int id) { var model=await service.getForUpdateAsync(id); if(model is null)return NotFound(); await LoadCategories(); return View(model); }
 [HttpPost] public async Task<IActionResult> Edit(ProductUpdateVM model) { await ValidateImagesAsync(model.MainImage, nameof(model.MainImage), model.HoverImage, nameof(model.HoverImage), model.AdditionalImages, nameof(model.AdditionalImages)); if(!ModelState.IsValid){await LoadCategories();return View(model);} if(!await service.updateAsync(model))return NotFound(); TempData["AdminNotice"]="Product updated."; return RedirectToAction(nameof(Index)); }
 [HttpPost] public async Task<IActionResult> Delete(int id) { await service.deleteAsync(id); TempData["AdminNotice"]="Product removed."; return RedirectToAction(nameof(Index)); }

 private async Task ValidateImagesAsync(IFormFile? mainImage, string mainImageKey, IFormFile? hoverImage, string hoverImageKey, List<IFormFile>? additionalImages, string additionalImagesKey)
 {
     await AddImageErrorAsync(mainImage, mainImageKey);
     await AddImageErrorAsync(hoverImage, hoverImageKey);
     if (additionalImages is null) return;
     foreach (var image in additionalImages)
         await AddImageErrorAsync(image, additionalImagesKey);
 }

 private async Task AddImageErrorAsync(IFormFile? file, string modelKey)
 {
     if (file is null || file.Length == 0) return;
     var validation = await ImageUploadValidator.ValidateAsync(file);
     if (!validation.IsValid) ModelState.AddModelError(modelKey, validation.Error!);
 }
}
