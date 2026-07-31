using Data.Data;
using Domain.Dto;
using Domain.Enum;
using Microsoft.EntityFrameworkCore;

namespace Domain.Services.PurchaseOrder;

public class PurchaseOrderService : BaseService<Models.PurchaseOrder>, IPurchaseOrderService
{
    private readonly MainDbContext _context;

    public PurchaseOrderService(MainDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Models.PurchaseOrder?> GetByIdWithDetailsAsync(int id)
    {
        return await Records
            .Include(po => po.Vendor)
            .Include(po => po.Lines).ThenInclude(l => l.CatalogItem)
            .Include(po => po.Approvals)
            .Include(po => po.Documents)
            .FirstOrDefaultAsync(po => po.Id == id);
    }

    public async Task<IList<Models.PurchaseOrder>> GetAllWithVendorAsync()
    {
        return await Records.Include(po => po.Vendor).OrderByDescending(po => po.RequestDate).ToListAsync();
    }

    public async Task<string> GeneratePoNumberAsync()
    {
        var year = DateTime.Now.Year;
        var count = await Records.CountAsync(po => po.RequestDate.Year == year);
        return $"PO-{year}-{(count + 1):D5}";
    }

    public async Task<(IList<Models.PurchaseOrder> Items, int TotalCount)> SearchAsync(PurchaseOrderSearchDto filter)
    {
        var query = Records.Include(po => po.Vendor).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(po =>
                po.PoNumber.ToLower().Contains(search) ||
                (po.Vendor.Name != null && po.Vendor.Name.ToLower().Contains(search)) ||
                (po.RequestedByName != null && po.RequestedByName.ToLower().Contains(search)));
        }

        if (filter.Status.HasValue)
            query = query.Where(po => po.Status == filter.Status.Value);

        if (filter.VendorId.HasValue)
            query = query.Where(po => po.VendorId == filter.VendorId.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(po => po.RequestDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(po => po.RequestDate <= filter.ToDate.Value);

        var totalCount = await query.CountAsync();

        query = filter.SortBy?.ToLower() switch
        {
            "ponumber" => filter.SortDescending ? query.OrderByDescending(po => po.PoNumber) : query.OrderBy(po => po.PoNumber),
            "totalamount" => filter.SortDescending ? query.OrderByDescending(po => po.TotalAmount) : query.OrderBy(po => po.TotalAmount),
            "status" => filter.SortDescending ? query.OrderByDescending(po => po.Status) : query.OrderBy(po => po.Status),
            _ => filter.SortDescending ? query.OrderByDescending(po => po.RequestDate) : query.OrderBy(po => po.RequestDate)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<SpendOverviewDto> GetSpendOverviewAsync()
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var sixMonthsAgo = startOfMonth.AddMonths(-5);

        var allOrders = await Records.Include(po => po.Vendor).ToListAsync();

        var pendingApprovals = allOrders.Count(po =>
            po.Status == EPurchaseOrderStatus.PendingManagerApproval ||
            po.Status == EPurchaseOrderStatus.PendingFinanceApproval ||
            po.Status == EPurchaseOrderStatus.PendingProcurementApproval);

        var monthlySpend = allOrders
            .Where(po => po.RequestDate >= startOfMonth && po.Status == EPurchaseOrderStatus.Approved)
            .Sum(po => po.TotalAmount);

        var recentOrders = allOrders.Count(po => po.RequestDate >= now.AddDays(-30));

        var totalVendors = await _context.Set<Models.Vendor>().CountAsync(v => v.IsActive);

        var monthlyTrend = Enumerable.Range(0, 6)
            .Select(i => sixMonthsAgo.AddMonths(i))
            .Select(month => new MonthlySpendItem
            {
                Month = month.ToString("MMM yyyy"),
                Amount = allOrders
                    .Where(po => po.RequestDate.Year == month.Year && po.RequestDate.Month == month.Month && po.Status == EPurchaseOrderStatus.Approved)
                    .Sum(po => po.TotalAmount)
            }).ToList();

        var statusBreakdown = allOrders
            .GroupBy(po => po.Status)
            .Select(g => new StatusCountItem { Status = g.Key.ToString(), Count = g.Count() })
            .ToList();

        var topVendors = allOrders
            .Where(po => po.Status == EPurchaseOrderStatus.Approved)
            .GroupBy(po => po.Vendor.Name)
            .Select(g => new TopVendorItem { VendorName = g.Key, TotalSpend = g.Sum(po => po.TotalAmount), OrderCount = g.Count() })
            .OrderByDescending(v => v.TotalSpend)
            .Take(5)
            .ToList();

        var recentOrdersList = allOrders
            .OrderByDescending(po => po.RequestDate)
            .Take(10)
            .Select(po => new RecentOrderItem
            {
                Id = po.Id,
                PoNumber = po.PoNumber,
                VendorName = po.Vendor?.Name ?? "Unknown",
                TotalAmount = po.TotalAmount,
                Status = po.Status.ToString(),
                RequestDate = po.RequestDate
            }).ToList();

        return new SpendOverviewDto
        {
            PendingApprovals = pendingApprovals,
            MonthlySpend = monthlySpend,
            RecentOrders = recentOrders,
            TotalVendors = totalVendors,
            TotalOrders = allOrders.Count,
            TotalSpend = allOrders.Where(po => po.Status == EPurchaseOrderStatus.Approved).Sum(po => po.TotalAmount),
            MonthlySpendTrend = monthlyTrend,
            StatusBreakdown = statusBreakdown,
            TopVendors = topVendors,
            RecentOrdersList = recentOrdersList
        };
    }

    public async Task<IList<Models.PurchaseOrder>> GetPendingApprovalsAsync(string userId)
    {
        return await Records
            .Include(po => po.Vendor)
            .Include(po => po.Approvals)
            .Where(po =>
                po.Status == EPurchaseOrderStatus.PendingManagerApproval ||
                po.Status == EPurchaseOrderStatus.PendingFinanceApproval ||
                po.Status == EPurchaseOrderStatus.PendingProcurementApproval)
            .OrderByDescending(po => po.RequestDate)
            .ToListAsync();
    }
}
