using JuanApp.Data;
using JuanApp.Areas.Admin.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace JuanApp.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "Admin")]
public class DashboardController(JuanDbContext db) : Controller
{
 public async Task<IActionResult> Index()
 {
  var vm = new DashboardVM { TotalProducts=await db.Products.CountAsync(), TotalOrders=await db.Orders.CountAsync(), TotalSalesAmount=await db.Orders.Where(o=>o.Status!=JuanApp.Models.Enums.OrderStatus.Cancelled).SumAsync(o=>(decimal?)o.TotalAmount)??0, TotalSubscribers=await db.Subscribers.CountAsync(s=>s.IsActive), TotalUsers=await db.Users.CountAsync(), RecentOrders=await db.Orders.Include(o=>o.OrderItems).OrderByDescending(o=>o.CreatedAt).Take(8).ToListAsync() };
  return View(vm);
 }
}
