using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using hamko.Models;
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

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Groups = _context.Groups.Where(g => g.Status).ToList();
            ViewBag.Items = _context.Items.ToList();

            var data = GetReportData(null, null, null, null);
            return View(data);
        }

        [HttpPost]
        public IActionResult Index(DateTime FromDate, DateTime ToDate, int? GroupId, int? ItemId)
        {
            ViewBag.Groups = _context.Groups.Where(g => g.Status).ToList();
            ViewBag.Items = _context.Items.ToList();

            var data = GetReportData(FromDate, ToDate, GroupId, ItemId);
            return View(data);
        }

        private List<GroupReportViewModel> GetReportData(DateTime? from, DateTime? to, int? groupId = null, int? itemId = null)
        {
            var allGroups = _context.Groups.Where(g => g.Status).ToList();
            var allItems = _context.Items.ToList();

            if (groupId.HasValue)
                allItems = allItems.Where(i => i.GroupId == groupId.Value).ToList();

            if (itemId.HasValue)
                allItems = allItems.Where(i => i.Id == itemId.Value).ToList();

            var itemIds = allItems.Select(i => i.Id).ToList();

            List<Product> allProducts;
            if (from.HasValue && to.HasValue)
            {
                var inIds = _context.StockIns
                    .Where(s => s.Purchase != null && s.Purchase.Date >= from.Value && s.Purchase.Date <= to.Value)
                    .Select(s => s.ProductId);

                var outIds = _context.StockOuts
                    .Where(s => s.Sales != null && s.Sales.Date >= from.Value && s.Sales.Date <= to.Value)
                    .Select(s => s.ProductId);

                var ids = inIds.Union(outIds).Distinct();
                allProducts = _context.Products.Where(p => ids.Contains(p.Id)).ToList();
            }
            else
            {
                allProducts = _context.Products.ToList();
            }

            if (groupId.HasValue || itemId.HasValue)
                allProducts = allProducts.Where(p => itemIds.Contains(p.ItemId)).ToList();

            var stockInsQuery = _context.StockIns.Where(s => s.Purchase != null).AsQueryable();
            if (from.HasValue && to.HasValue)
                stockInsQuery = stockInsQuery.Where(s => s.Purchase.Date >= from.Value && s.Purchase.Date <= to.Value);
            var allStockIns = stockInsQuery.ToList();

            var stockOutsQuery = _context.StockOuts.Where(s => s.Sales != null).AsQueryable();
            if (from.HasValue && to.HasValue)
                stockOutsQuery = stockOutsQuery.Where(s => s.Sales.Date >= from.Value && s.Sales.Date <= to.Value);
            var allStockOuts = stockOutsQuery.ToList();

            var report = allGroups
                .Where(g => g.ParentId == null)
                .Select(g => new GroupReportViewModel
                {
                    Id = g.Id,
                    GroupName = g.Name,
                    Items = allItems
                        .Where(i => i.GroupId == g.Id)
                        .Select(i => new ItemReportViewModel
                        {
                            Id = i.Id,
                            ItemName = i.Name,
                            Products = allProducts
                                .Where(p => p.ItemId == i.Id)
                                .Select(p =>
                                {
                                    var productStockIns = allStockIns.Where(s => s.ProductId == p.Id);
                                    var productStockOuts = allStockOuts.Where(s => s.ProductId == p.Id);

                                    var inQty = productStockIns.Sum(s => s.Quantity);
                                    var inPrice = productStockIns.Any() ? productStockIns.Average(s => s.Price) : 0;
                                    var inTotal = productStockIns.Sum(s => s.Quantity * s.Price);

                                    var outQty = productStockOuts.Sum(s => s.Quantity);
                                    var outPrice = productStockOuts.Any() ? productStockOuts.Average(s => s.Price) : 0;
                                    var outTotal = productStockOuts.Sum(s => s.Quantity * s.Price);

                                    return new ProductReportViewModel
                                    {
                                        ProductName = p.Name,
                                        InQty = inQty,
                                        InPrice = inPrice,
                                        InTotal = inTotal,
                                        OutQty = outQty,
                                        OutPrice = outPrice,
                                        OutTotal = outTotal
                                    };
                                }).ToList()
                        }).ToList(),
                    SubGroups = allGroups
                        .Where(sub => sub.ParentId == g.Id)
                        .Select(sub => new GroupReportViewModel
                        {
                            Id = sub.Id,
                            GroupName = sub.Name,
                            Items = allItems
                                .Where(i => i.GroupId == sub.Id)
                                .Select(i => new ItemReportViewModel
                                {
                                    Id = i.Id,
                                    ItemName = i.Name,
                                    Products = allProducts
                                        .Where(p => p.ItemId == i.Id)
                                        .Select(p =>
                                        {
                                            var productStockIns = allStockIns.Where(s => s.ProductId == p.Id);
                                            var productStockOuts = allStockOuts.Where(s => s.ProductId == p.Id);

                                            var inQty = productStockIns.Sum(s => s.Quantity);
                                            var inPrice = productStockIns.Any() ? productStockIns.Average(s => s.Price) : 0;
                                            var inTotal = productStockIns.Sum(s => s.Quantity * s.Price);

                                            var outQty = productStockOuts.Sum(s => s.Quantity);
                                            var outPrice = productStockOuts.Any() ? productStockOuts.Average(s => s.Price) : 0;
                                            var outTotal = productStockOuts.Sum(s => s.Quantity * s.Price);

                                            return new ProductReportViewModel
                                            {
                                                ProductName = p.Name,
                                                InQty = inQty,
                                                InPrice = inPrice,
                                                InTotal = inTotal,
                                                OutQty = outQty,
                                                OutPrice = outPrice,
                                                OutTotal = outTotal
                                            };
                                        }).ToList()
                                }).ToList()
                        }).ToList()
                }).ToList();

            foreach (var group in report)
            {
                CalculateGroupTotals(group);
            }

            return report;
        }


        // Recursive: Group Total Calculator
        private void CalculateGroupTotals(GroupReportViewModel group)
        {
            group.TotalQty = 0;
            group.TotalPrice = 0;
            group.TotalAmount = 0;
            group.TotalOutQty = 0;
            group.TotalOutPrice = 0;
            group.TotalOutAmount = 0;

            foreach (var item in group.Items)
            {
                CalculateItemTotals(item);
                group.TotalQty += item.TotalQty;
                group.TotalPrice += item.TotalPrice;
                group.TotalAmount += item.TotalAmount;

                group.TotalOutQty += item.TotalOutQty;
                group.TotalOutPrice += item.TotalOutPrice;
                group.TotalOutAmount += item.TotalOutAmount;
            }

            foreach (var sub in group.SubGroups)
            {
                CalculateGroupTotals(sub);
                group.TotalQty += sub.TotalQty;
                group.TotalPrice += sub.TotalPrice;
                group.TotalAmount += sub.TotalAmount;

                group.TotalOutQty += sub.TotalOutQty;
                group.TotalOutPrice += sub.TotalOutPrice;
                group.TotalOutAmount += sub.TotalOutAmount;
            }
        }

        // Recursive: Item Total Calculator
        private void CalculateItemTotals(ItemReportViewModel item)
        {
            item.TotalQty = 0;
            item.TotalPrice = 0;
            item.TotalAmount = 0;
            item.TotalOutQty = 0;
            item.TotalOutPrice = 0;
            item.TotalOutAmount = 0;

            foreach (var product in item.Products)
            {
                item.TotalQty += product.InQty;
                item.TotalPrice += product.InPrice;
                item.TotalAmount += product.InTotal;

                item.TotalOutQty += product.OutQty;
                item.TotalOutPrice += product.OutPrice;
                item.TotalOutAmount += product.OutTotal;
            }
        }

        // ViewModel Classes

        public class GroupReportViewModel
        {
            public int Id { get; set; }
            public string GroupName { get; set; }
            public List<GroupReportViewModel> SubGroups { get; set; } = new();
            public List<ItemReportViewModel> Items { get; set; } = new();
            public decimal TotalQty { get; set; }
            public decimal TotalPrice { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal TotalOutQty { get; set; }
            public decimal TotalOutPrice { get; set; }
            public decimal TotalOutAmount { get; set; }
        }

        public class ItemReportViewModel
        {
            public int Id { get; set; }
            public string ItemName { get; set; }
            public List<ProductReportViewModel> Products { get; set; } = new();
            public decimal TotalQty { get; set; }
            public decimal TotalPrice { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal TotalOutQty { get; set; }
            public decimal TotalOutPrice { get; set; }
            public decimal TotalOutAmount { get; set; }
        }

        public class ProductReportViewModel
        {
            public string ProductName { get; set; }
            public decimal InQty { get; set; }
            public decimal InPrice { get; set; }
            public decimal InTotal { get; set; }
            public decimal OutQty { get; set; }
            public decimal OutPrice { get; set; }
            public decimal OutTotal { get; set; }
        }
    }
}
