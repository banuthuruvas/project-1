using API.Authorization;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Domain.Dto;
using Domain.Enum;
using Domain.Models;
using Domain.Security;
using Domain.Services.PurchaseOrder;
using Domain.Services.PurchaseOrderDocument;
using Domain.Services.FileStorage;

namespace API.Controllers;

public class PurchaseOrderController : BaseController
{
    private readonly IPurchaseOrderService _poService;
    private readonly IPurchaseOrderDocumentService _poDocService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMapper _mapper;
    private readonly ILogger<PurchaseOrderController> _logger;

    public PurchaseOrderController(
        IPurchaseOrderService poService,
        IPurchaseOrderDocumentService poDocService,
        IFileStorageService fileStorageService,
        IMapper mapper,
        ILogger<PurchaseOrderController> logger)
    {
        _poService = poService;
        _poDocService = poDocService;
        _fileStorageService = fileStorageService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementOrderRead)]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetAll()
    {
        var orders = await _poService.GetAllWithVendorAsync();
        var dtos = orders.Select(po =>
        {
            var dto = _mapper.Map<PurchaseOrderDto>(po);
            dto.VendorName = po.Vendor?.Name;
            return dto;
        }).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementOrderRead)]
    public async Task<ActionResult<PurchaseOrderDto>> Get(int id)
    {
        var po = await _poService.GetByIdWithDetailsAsync(id);
        if (po == null) return NotFound("Purchase order not found");
        var dto = _mapper.Map<PurchaseOrderDto>(po);
        dto.VendorName = po.Vendor?.Name;
        return Ok(dto);
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementOrderManage)]
    public async Task<ActionResult<PurchaseOrderDto>> Save([FromBody] PurchaseOrderDto dto)
    {
        if (dto.VendorId <= 0) return BadRequest("Vendor is required");
        if (dto.Lines == null || dto.Lines.Count == 0) return BadRequest("At least one line item is required");

        var entity = new PurchaseOrder
        {
            PoNumber = await _poService.GeneratePoNumberAsync(),
            RequestedBy = UserId ?? "system",
            RequestedByName = UserName,
            RequestDate = DateTime.Now,
            DeliveryAddress = dto.DeliveryAddress,
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            Status = EPurchaseOrderStatus.Draft,
            Notes = dto.Notes,
            VendorId = dto.VendorId,
        };

        foreach (var lineDto in dto.Lines)
        {
            entity.Lines.Add(new PurchaseOrderLine
            {
                LineNumber = lineDto.LineNumber,
                ItemName = lineDto.ItemName,
                Description = lineDto.Description,
                UnitOfMeasure = lineDto.UnitOfMeasure,
                Quantity = lineDto.Quantity,
                UnitPrice = lineDto.UnitPrice,
                LineTotal = lineDto.Quantity * lineDto.UnitPrice,
                CatalogItemId = lineDto.CatalogItemId
            });
        }

        entity.TotalAmount = entity.Lines.Sum(l => l.LineTotal);

        var saved = await _poService.SaveAsync(entity);
        _logger.LogInformation("Created purchase order {PoNumber}", saved.PoNumber);

        var result = await _poService.GetByIdWithDetailsAsync(saved.Id);
        var resultDto = _mapper.Map<PurchaseOrderDto>(result!);
        resultDto.VendorName = result!.Vendor?.Name;
        return Ok(resultDto);
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementOrderManage)]
    public async Task<ActionResult<PurchaseOrderDto>> Edit([FromBody] PurchaseOrderDto dto)
    {
        if (dto.Id <= 0) return BadRequest("Invalid ID");
        var existing = await _poService.GetByIdWithDetailsAsync(dto.Id);
        if (existing == null) return NotFound("Purchase order not found");
        if (existing.Status != EPurchaseOrderStatus.Draft)
            return BadRequest("Only draft orders can be edited");

        existing.DeliveryAddress = dto.DeliveryAddress;
        existing.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
        existing.Notes = dto.Notes;
        existing.VendorId = dto.VendorId;

        existing.Lines.Clear();
        foreach (var lineDto in dto.Lines)
        {
            existing.Lines.Add(new PurchaseOrderLine
            {
                LineNumber = lineDto.LineNumber,
                ItemName = lineDto.ItemName,
                Description = lineDto.Description,
                UnitOfMeasure = lineDto.UnitOfMeasure,
                Quantity = lineDto.Quantity,
                UnitPrice = lineDto.UnitPrice,
                LineTotal = lineDto.Quantity * lineDto.UnitPrice,
                CatalogItemId = lineDto.CatalogItemId,
                PurchaseOrderId = existing.Id
            });
        }
        existing.TotalAmount = existing.Lines.Sum(l => l.LineTotal);

        var updated = await _poService.SaveOrUpdateAsync(existing);
        _logger.LogInformation("Updated purchase order {Id}", updated.Id);

        var result = await _poService.GetByIdWithDetailsAsync(updated.Id);
        var resultDto = _mapper.Map<PurchaseOrderDto>(result!);
        resultDto.VendorName = result!.Vendor?.Name;
        return Ok(resultDto);
    }

    [HttpPost("{id}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementOrderManage)]
    public async Task<ActionResult<PurchaseOrderDto>> Submit(int id)
    {
        var po = await _poService.GetByIdWithDetailsAsync(id);
        if (po == null) return NotFound("Purchase order not found");
        if (po.Status != EPurchaseOrderStatus.Draft)
            return BadRequest("Only draft orders can be submitted");

        po.Status = EPurchaseOrderStatus.PendingManagerApproval;

        // Create approval chain — stages and order driven by EApprovalStage enum
        po.Approvals.Add(new PurchaseOrderApproval { ApprovalStage = EApprovalStage.Manager, StageOrder = (int)EApprovalStage.Manager, PurchaseOrderId = po.Id });
        po.Approvals.Add(new PurchaseOrderApproval { ApprovalStage = EApprovalStage.Finance, StageOrder = (int)EApprovalStage.Finance, PurchaseOrderId = po.Id });
        po.Approvals.Add(new PurchaseOrderApproval { ApprovalStage = EApprovalStage.Procurement, StageOrder = (int)EApprovalStage.Procurement, PurchaseOrderId = po.Id });

        await _poService.SaveOrUpdateAsync(po);
        _logger.LogInformation("Submitted purchase order {PoNumber}", po.PoNumber);

        var result = await _poService.GetByIdWithDetailsAsync(po.Id);
        var dto = _mapper.Map<PurchaseOrderDto>(result!);
        dto.VendorName = result!.Vendor?.Name;
        return Ok(dto);
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementOrderApprove)]
    public async Task<ActionResult<PurchaseOrderDto>> ProcessApproval([FromBody] ApprovalActionDto actionDto)
    {
        var po = await _poService.GetByIdWithDetailsAsync(actionDto.PurchaseOrderId);
        if (po == null) return NotFound("Purchase order not found");

        // Determine current stage
        var currentApproval = po.Approvals
            .Where(a => a.Action == null)
            .OrderBy(a => a.StageOrder)
            .FirstOrDefault();

        if (currentApproval == null) return BadRequest("No pending approval stage");

        currentApproval.ApproverId = UserId;
        currentApproval.ApproverName = UserName;
        currentApproval.Action = actionDto.Action;
        currentApproval.ActionDate = DateTime.Now;
        currentApproval.Comments = actionDto.Comments;

        if (actionDto.Action == EApprovalAction.Reject)
        {
            po.Status = EPurchaseOrderStatus.Rejected;
            po.RejectionReason = actionDto.Comments;
        }
        else
        {
            // Move to next stage
            var nextApproval = po.Approvals
                .Where(a => a.Action == null && a.StageOrder > currentApproval.StageOrder)
                .OrderBy(a => a.StageOrder)
                .FirstOrDefault();

            if (nextApproval == null)
            {
                po.Status = EPurchaseOrderStatus.Approved;
            }
            else
            {
                po.Status = nextApproval.ApprovalStage switch
                {
                    EApprovalStage.Finance => EPurchaseOrderStatus.PendingFinanceApproval,
                    EApprovalStage.Procurement => EPurchaseOrderStatus.PendingProcurementApproval,
                    EApprovalStage.Manager => EPurchaseOrderStatus.PendingManagerApproval,
                    _ => throw new InvalidOperationException($"Unknown approval stage: {nextApproval.ApprovalStage}")
                };
            }
        }

        await _poService.SaveOrUpdateAsync(po);
        _logger.LogInformation("Processed approval for PO {PoNumber}: {Action}", po.PoNumber, actionDto.Action);

        var result = await _poService.GetByIdWithDetailsAsync(po.Id);
        var dto = _mapper.Map<PurchaseOrderDto>(result!);
        dto.VendorName = result!.Vendor?.Name;
        return Ok(dto);
    }

    [HttpPost("Delete/{id}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementOrderManage)]
    public async Task<ActionResult> Delete(int id)
    {
        var po = await _poService.GetByIdWithDetailsAsync(id);
        if (po == null) return NotFound("Purchase order not found");
        if (po.Status != EPurchaseOrderStatus.Draft)
            return BadRequest("Only draft orders can be deleted");

        foreach (var doc in po.Documents)
        {
            try { await _fileStorageService.DeleteFileAsync(doc.FilePath); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete file {FilePath}", doc.FilePath); }
        }

        var deleted = await _poService.DeleteAsync(id);
        if (!deleted) return BadRequest("Failed to delete order");
        _logger.LogInformation("Deleted purchase order {Id}", id);
        return Ok();
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementOrderRead)]
    public async Task<ActionResult> Search([FromBody] PurchaseOrderSearchDto filter)
    {
        var (items, totalCount) = await _poService.SearchAsync(filter);
        var dtos = items.Select(po =>
        {
            var dto = _mapper.Map<PurchaseOrderDto>(po);
            dto.VendorName = po.Vendor?.Name;
            return dto;
        }).ToList();

        return Ok(new { Items = dtos, TotalCount = totalCount, filter.Page, filter.PageSize });
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementOrderRead)]
    public async Task<ActionResult<SpendOverviewDto>> GetSpendOverview()
    {
        var overview = await _poService.GetSpendOverviewAsync();
        return Ok(overview);
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementOrderRead)]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetPendingApprovals()
    {
        var orders = await _poService.GetPendingApprovalsAsync(UserId ?? "");
        var dtos = orders.Select(po =>
        {
            var dto = _mapper.Map<PurchaseOrderDto>(po);
            dto.VendorName = po.Vendor?.Name;
            return dto;
        }).ToList();
        return Ok(dtos);
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.DocumentManage)]
    public async Task<ActionResult<PurchaseOrderDocumentDto>> UploadDocument(int purchaseOrderId, IFormFile file, string? documentType)
    {
        if (file == null || file.Length == 0) return BadRequest("No file provided");

        var po = await _poService.GetByIdAsync(purchaseOrderId);
        if (po == null) return NotFound("Purchase order not found");

        var filePath = await _fileStorageService.SaveFileAsync(file, file.FileName);

        var doc = new PurchaseOrderDocument
        {
            FilePath = filePath,
            UserFileName = file.FileName,
            FileSize = file.Length,
            DocumentType = documentType,
            PurchaseOrderId = purchaseOrderId
        };

        var saved = await _poDocService.SaveAsync(doc);
        _logger.LogInformation("Uploaded document for PO {Id}", purchaseOrderId);
        return Ok(_mapper.Map<PurchaseOrderDocumentDto>(saved));
    }

    [HttpPost("DeleteDocument/{documentId}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.DocumentManage)]
    public async Task<ActionResult> DeleteDocument(int documentId)
    {
        var doc = await _poDocService.GetByIdAsync(documentId);
        if (doc == null) return NotFound("Document not found");

        try { await _fileStorageService.DeleteFileAsync(doc.FilePath); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete file {FilePath}", doc.FilePath); }

        await _poDocService.DeleteAsync(documentId);
        _logger.LogInformation("Deleted PO document {Id}", documentId);
        return Ok();
    }
}
