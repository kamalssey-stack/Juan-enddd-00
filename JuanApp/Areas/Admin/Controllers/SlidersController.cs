using JuanApp.Areas.Admin.ViewModels.SliderVM;
using JuanApp.Data;
using JuanApp.Models;
using JuanApp.Services.Implements;
using JuanApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JuanApp.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public class SlidersController(JuanDbContext context, IFileService files) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var sliders = await context.Sliders.OrderBy(slider => slider.OrderNumber).ToListAsync();
        return View(sliders);
    }

    [HttpGet]
    public IActionResult Create() => View(new SliderCreateVM());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SliderCreateVM model)
    {
        if (model.Image is null || model.Image.Length == 0)
            ModelState.AddModelError(nameof(model.Image), "Please upload a slider image.");
        else
        {
            var validation = await ImageUploadValidator.ValidateAsync(model.Image);
            if (!validation.IsValid) ModelState.AddModelError(nameof(model.Image), validation.Error!);
        }

        if (!ModelState.IsValid) return View(model);

        var imageName = await files.UploadAsync(model.Image!, "assets/img/slider");
        context.Sliders.Add(new Slider
        {
            Title = model.Title.Trim(),
            Subtitle = model.Subtitle?.Trim(),
            Description = model.Description?.Trim(),
            ButtonText = model.ButtonText?.Trim(),
            ButtonLink = model.ButtonLink?.Trim(),
            OrderNumber = model.OrderNumber,
            ImageUrl = imageName
        });

        await context.SaveChangesAsync();
        TempData["AdminNotice"] = "Slider added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var slider = await context.Sliders.FindAsync(id);
        if (slider is null) return NotFound();

        return View(new SliderUpdateVM
        {
            Id = slider.Id,
            Title = slider.Title,
            Subtitle = slider.Subtitle,
            Description = slider.Description,
            ButtonText = slider.ButtonText,
            ButtonLink = slider.ButtonLink,
            OrderNumber = slider.OrderNumber,
            CurrentImageUrl = slider.ImageUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SliderUpdateVM model)
    {
        var slider = await context.Sliders.FindAsync(model.Id);
        if (slider is null) return NotFound();

        if (model.Image is not null && model.Image.Length > 0)
        {
            var validation = await ImageUploadValidator.ValidateAsync(model.Image);
            if (!validation.IsValid) ModelState.AddModelError(nameof(model.Image), validation.Error!);
        }

        if (!ModelState.IsValid)
        {
            model.CurrentImageUrl = slider.ImageUrl;
            return View(model);
        }

        slider.Title = model.Title.Trim();
        slider.Subtitle = model.Subtitle?.Trim();
        slider.Description = model.Description?.Trim();
        slider.ButtonText = model.ButtonText?.Trim();
        slider.ButtonLink = model.ButtonLink?.Trim();
        slider.OrderNumber = model.OrderNumber;
        slider.UpdatedAt = DateTime.UtcNow;

        if (model.Image is not null && model.Image.Length > 0)
            slider.ImageUrl = await files.UploadAsync(model.Image, "assets/img/slider");

        await context.SaveChangesAsync();
        TempData["AdminNotice"] = "Slider updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var slider = await context.Sliders.FindAsync(id);
        if (slider is null) return NotFound();

        // Soft-delete the record; leave the image file and historical data untouched.
        slider.IsDeleted = true;
        slider.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        TempData["AdminNotice"] = "Slider removed from the homepage.";
        return RedirectToAction(nameof(Index));
    }
}
