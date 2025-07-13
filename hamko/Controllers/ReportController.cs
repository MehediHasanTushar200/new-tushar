using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using hamko.Service;
using hamko.Models;

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
            var allData = GetReportData(null, null);
            return View(allData);
        }

        [HttpPost]
        public IActionResult Index(DateTime FromDate, DateTime ToDate)
        {

            var filteredData = GetReportData(FromDate, ToDate);
            return View(filteredData);
        }

        private List<GroupReportViewModel> GetReportData(DateTime? from, DateTime? to)
        {
            var allGroups = _context.Groups.Where(g => g.Status).ToList();
            var allItems = _context.Items.ToList();
            List<Product> allProducts;

            if (from.HasValue && to.HasValue)
            {
                
                var stockInProductIds = _context.StockIns
                    .Where(s => s.Purchase != null
                                && s.Purchase.Date >= from.Value
                                && s.Purchase.Date <= to.Value)
                    .Select(s => s.ProductId)
                    .Distinct();

                var stockOutProductIds = _context.StockOuts
                    .Where(s => s.Sales != null
                                && s.Sales.Date >= from.Value
                                && s.Sales.Date <= to.Value)
                    .Select(s => s.ProductId)
                    .Distinct();

                var productIdsInRange = stockInProductIds.Union(stockOutProductIds);

                allProducts = _context.Products
                    .Where(p => productIdsInRange.Contains(p.Id))
                    .ToList();
            }
            else
            {
                allProducts = _context.Products.ToList();
            }

            // StockIns Query
            var allStockInsQuery = _context.StockIns.Where(s => s.Purchase != null).AsQueryable();
            if (from.HasValue && to.HasValue)
            {
                allStockInsQuery = allStockInsQuery.Where(s => s.Purchase.Date >= from.Value && s.Purchase.Date <= to.Value);
            }
            var allStockIns = allStockInsQuery.ToList();

            // StockOuts Query
            var allStockOutsQuery = _context.StockOuts.Where(s => s.Sales != null).AsQueryable();
            if (from.HasValue && to.HasValue)
            {
                allStockOutsQuery = allStockOutsQuery.Where(s => s.Sales.Date >= from.Value && s.Sales.Date <= to.Value);
            }
            var allStockOuts = allStockOutsQuery.ToList();

            var reportData = allGroups
                .Where(g => g.ParentId == null)
                .Select(main => new GroupReportViewModel
                {
                    Id = main.Id,
                    GroupName = main.Name,
                    Items = allItems
                        .Where(i => i.GroupId == main.Id)
                        .Select(item => new ItemReportViewModel
                        {
                            Id = item.Id,
                            ItemName = item.Name,
                            Products = allProducts
                                .Where(p => p.ItemId == item.Id)
                                .Select(product => new ProductReportViewModel
                                {
                                    ProductName = product.Name,
                                    StockIns = allStockIns
                                        .Where(s => s.ProductId == product.Id)
                                        .Select(s => new StockInReportViewModel
                                        {
                                            Id = s.Id,
                                            Quantity = s.Quantity,
                                            Price = s.Price,
                                            Total = s.Quantity * s.Price
                                        }).ToList(),
                                    StockOuts = allStockOuts
                                        .Where(s => s.ProductId == product.Id)
                                        .Select(s => new StockOutReportViewModel
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
                            Items = allItems
                                .Where(i => i.GroupId == sub.Id)
                                .Select(item => new ItemReportViewModel
                                {
                                    Id = item.Id,
                                    ItemName = item.Name,
                                    Products = allProducts
                                        .Where(p => p.ItemId == item.Id)
                                        .Select(product => new ProductReportViewModel
                                        {
                                            ProductName = product.Name,
                                            StockIns = allStockIns
                                                .Where(s => s.ProductId == product.Id)
                                                .Select(s => new StockInReportViewModel
                                                {
                                                    Id = s.Id,
                                                    Quantity = s.Quantity,
                                                    Price = s.Price,
                                                    Total = s.Quantity * s.Price
                                                }).ToList(),
                                            StockOuts = allStockOuts
                                                .Where(s => s.ProductId == product.Id)
                                                .Select(s => new StockOutReportViewModel
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

            // Calculate totals
            foreach (var g in reportData)
            {
                g.TotalQty = 0; g.TotalPrice = 0; g.TotalAmount = 0;
                g.TotalOutQty = 0; g.TotalOutPrice = 0; g.TotalOutAmount = 0;

                foreach (var item in g.Items)
                {
                    item.TotalQty = 0; item.TotalPrice = 0; item.TotalAmount = 0;
                    item.TotalOutQty = 0; item.TotalOutPrice = 0; item.TotalOutAmount = 0;

                    foreach (var product in item.Products)
                    {
                        foreach (var sin in product.StockIns)
                        {
                            item.TotalQty += sin.Quantity;
                            item.TotalPrice += sin.Price;
                            item.TotalAmount += sin.Total;
                        }
                        foreach (var sout in product.StockOuts)
                        {
                            item.TotalOutQty += sout.Quantity;
                            item.TotalOutPrice += sout.Price;
                            item.TotalOutAmount += sout.Total;
                        }
                    }

                    g.TotalQty += item.TotalQty;
                    g.TotalPrice += item.TotalPrice;
                    g.TotalAmount += item.TotalAmount;

                    g.TotalOutQty += item.TotalOutQty;
                    g.TotalOutPrice += item.TotalOutPrice;
                    g.TotalOutAmount += item.TotalOutAmount;
                }

                foreach (var sub in g.SubGroups)
                {
                    sub.TotalQty = 0; sub.TotalPrice = 0; sub.TotalAmount = 0;
                    sub.TotalOutQty = 0; sub.TotalOutPrice = 0; sub.TotalOutAmount = 0;

                    foreach (var item in sub.Items)
                    {
                        item.TotalQty = 0; item.TotalPrice = 0; item.TotalAmount = 0;
                        item.TotalOutQty = 0; item.TotalOutPrice = 0; item.TotalOutAmount = 0;

                        foreach (var product in item.Products)
                        {
                            foreach (var sin in product.StockIns)
                            {
                                item.TotalQty += sin.Quantity;
                                item.TotalPrice += sin.Price;
                                item.TotalAmount += sin.Total;
                            }
                            foreach (var sout in product.StockOuts)
                            {
                                item.TotalOutQty += sout.Quantity;
                                item.TotalOutPrice += sout.Price;
                                item.TotalOutAmount += sout.Total;
                            }
                        }

                        sub.TotalQty += item.TotalQty;
                        sub.TotalPrice += item.TotalPrice;
                        sub.TotalAmount += item.TotalAmount;

                        sub.TotalOutQty += item.TotalOutQty;
                        sub.TotalOutPrice += item.TotalOutPrice;
                        sub.TotalOutAmount += item.TotalOutAmount;
                    }

                    g.TotalQty += sub.TotalQty;
                    g.TotalPrice += sub.TotalPrice;
                    g.TotalAmount += sub.TotalAmount;

                    g.TotalOutQty += sub.TotalOutQty;
                    g.TotalOutPrice += sub.TotalOutPrice;
                    g.TotalOutAmount += sub.TotalOutAmount;
                }
            }

            return reportData;
        }

        // ViewModels
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
            public List<StockInReportViewModel> StockIns { get; set; } = new();
            public List<StockOutReportViewModel> StockOuts { get; set; } = new();
        }

        public class StockInReportViewModel
        {
            public int Id { get; set; }
            public decimal Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal Total { get; set; }
        }

        public class StockOutReportViewModel
        {
            public int Id { get; set; }
            public decimal Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal Total { get; set; }
        }
    }
}
