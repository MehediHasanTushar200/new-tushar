using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using hamko.Service;

namespace hamko.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var allGroups = _context.Groups.Where(g => g.Status).ToList();

            var reportData = allGroups
                .Where(g => g.ParentId == null)
                .Select(main => new GroupReportViewModel
                {
                    Id = main.Id,
                    GroupName = main.Name,
                    Items = _context.Items
                        .Where(i => i.GroupId == main.Id)
                        .Select(item => new ItemReportViewModel
                        {
                            Id = item.Id,
                            ItemName = item.Name,
                            Products = _context.Products
                                .Where(p => p.ItemId == item.Id)
                                .Select(product => new ProductReportViewModel
                                {
                                    ProductName = product.Name,
                                    StockIns = _context.StockIns
                                        .Where(s => s.ProductId == product.Id)
                                        .Select(s => new StockInReportViewModel
                                        {
                                            Id = s.Id,
                                            Quantity = s.Quantity,
                                            Price = s.Price,
                                            Total = s.Quantity * s.Price
                                        }).ToList()
                                }).ToList()
                        }).ToList(),

                    SubGroups = allGroups
                        .Where(sub => sub.ParentId == main.Id)
                        .Select(sub => new GroupReportViewModel
                        {
                            Id = sub.Id,
                            GroupName = sub.Name,
                            Items = _context.Items
                                .Where(i => i.GroupId == sub.Id)
                                .Select(item => new ItemReportViewModel
                                {
                                    Id = item.Id,
                                    ItemName = item.Name,
                                    Products = _context.Products
                                        .Where(p => p.ItemId == item.Id)
                                        .Select(product => new ProductReportViewModel
                                        {
                                            ProductName = product.Name,
                                            StockIns = _context.StockIns
                                                .Where(s => s.ProductId == product.Id)
                                                .Select(s => new StockInReportViewModel
                                                {
                                                    Id = s.Id,
                                                    Quantity = s.Quantity,
                                                    Price = s.Price,
                                                    Total = s.Quantity * s.Price
                                                }).ToList()
                                        }).ToList()
                                }).ToList()
                        }).ToList()
                }).ToList();

            // Total Calculation
            foreach (var g in reportData)
            {
                g.TotalQty = 0;
                g.TotalPrice = 0;
                g.TotalAmount = 0;

                foreach (var item in g.Items)
                {
                    item.TotalQty = 0;
                    item.TotalPrice = 0;
                    item.TotalAmount = 0;

                    foreach (var product in item.Products)
                    {
                        foreach (var stock in product.StockIns)
                        {
                            item.TotalQty += stock.Quantity;
                            item.TotalPrice += stock.Price;
                            item.TotalAmount += stock.Total;
                        }
                    }

                    g.TotalQty += item.TotalQty;
                    g.TotalPrice += item.TotalPrice;
                    g.TotalAmount += item.TotalAmount;
                }

                foreach (var sub in g.SubGroups)
                {
                    sub.TotalQty = 0;
                    sub.TotalPrice = 0;
                    sub.TotalAmount = 0;

                    foreach (var item in sub.Items)
                    {
                        item.TotalQty = 0;
                        item.TotalPrice = 0;
                        item.TotalAmount = 0;

                        foreach (var product in item.Products)
                        {
                            foreach (var stock in product.StockIns)
                            {
                                item.TotalQty += stock.Quantity;
                                item.TotalPrice += stock.Price;
                                item.TotalAmount += stock.Total;
                            }
                        }

                        sub.TotalQty += item.TotalQty;
                        sub.TotalPrice += item.TotalPrice;
                        sub.TotalAmount += item.TotalAmount;
                    }

                    g.TotalQty += sub.TotalQty;
                    g.TotalPrice += sub.TotalPrice;
                    g.TotalAmount += sub.TotalAmount;
                }
            }

            return View(reportData);
        }

        public class GroupReportViewModel
        {
            public int Id { get; set; }
            public string GroupName { get; set; }
            public List<GroupReportViewModel> SubGroups { get; set; } = new();
            public List<ItemReportViewModel> Items { get; set; } = new();
            public decimal TotalQty { get; set; }
            public decimal TotalPrice { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public class ItemReportViewModel
        {
            public int Id { get; set; }
            public string ItemName { get; set; }
            public List<ProductReportViewModel> Products { get; set; } = new();
            public decimal TotalQty { get; set; }
            public decimal TotalPrice { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public class ProductReportViewModel
        {
            public string ProductName { get; set; }
            public List<StockInReportViewModel> StockIns { get; set; } = new();
        }

        public class StockInReportViewModel
        {
            public int Id { get; set; }
            public decimal Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal Total { get; set; }
        }
    }
}