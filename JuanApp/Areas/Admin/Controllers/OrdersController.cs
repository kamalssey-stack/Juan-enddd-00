using JuanApp.Models.Enums;
using JuanApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace JuanApp.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class OrdersController(IOrderService service) : Controller
{
 public async Task<IActionResult> Index() => View(await service.getAllOrdersAdminAsync());
 public async Task<IActionResult> Detail(int id) { var item=await service.getOrderByIdAsync(id); return item is null?NotFound():View(item); }
 [HttpPost] public async Task<IActionResult> Status(int id, OrderStatus status) { if(!Enum.IsDefined(status))return BadRequest(); if(!await service.updateOrderStatusAsync(id,status))return NotFound(); TempData["AdminNotice"]="Order status updated."; return RedirectToAction(nameof(Detail),new{id}); }
}
