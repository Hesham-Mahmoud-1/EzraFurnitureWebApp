using AutoMapper;
using Ezura.Core.DTOs;
using Ezura.Core.Entities;
using Ezura.Core.Enums;
using Ezura.Core.Interfaces.Repositories;
using Ezura.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Ezura.Application.Services;

// ============================================================
// PRODUCT SERVICE
// ============================================================
public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork uow, ILogger<ProductService> logger)
    {
        _uow = uow; _logger = logger;
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var p = await _uow.Products.GetWithImagesAsync(id);
        if (p == null) return null;
        await IncrementViewCountAsync(id);
        return MapToDto(p);
    }

    public async Task<ProductDto?> GetBySlugAsync(string slug)
    {
        var p = await _uow.Products.GetBySlugAsync(slug);
        return p == null ? null : MapToDto(p);
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(ProductFilterDto filter)
    {
        var (products, total) = await _uow.Products.GetPagedAsync(
            filter.Page, filter.PageSize, filter.CategoryId, filter.Search,
            filter.SortBy, filter.MinPrice, filter.MaxPrice);

        return new PagedResult<ProductDto>
        {
            Items = products.Select(MapToDto),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<ProductDto>> GetFeaturedAsync(int count = 8)
    {
        var products = await _uow.Products.GetFeaturedProductsAsync(count);
        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductDto>> GetRelatedAsync(int productId, int count = 4)
    {
        var products = await _uow.Products.GetRelatedProductsAsync(productId, count);
        return products.Select(MapToDto);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, string userId)
    {
        var slug = GenerateSlug(dto.Name);
        var product = new Product
        {
            Name = dto.Name, Slug = slug,
            ShortDescription = dto.ShortDescription,
            Description = dto.Description,
            Specifications = dto.Specifications,
            Price = dto.Price, DiscountPrice = dto.DiscountPrice,
            CostPrice = dto.CostPrice, CategoryId = dto.CategoryId,
            MaterialType = dto.MaterialType,
            WidthCm = dto.WidthCm, HeightCm = dto.HeightCm,
            DepthCm = dto.DepthCm, WeightKg = dto.WeightKg,
            Color = dto.Color, FinishType = dto.FinishType,
            StockQuantity = dto.StockQuantity,
            LowStockThreshold = dto.LowStockThreshold,
            IsAvailable = dto.IsAvailable, IsFeatured = dto.IsFeatured,
            IsCustomizable = dto.IsCustomizable, Sku = dto.Sku,
            Tags = dto.Tags, MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription, CreatedBy = userId
        };

        await _uow.Products.AddAsync(product);
        await _uow.SaveChangesAsync();
        _logger.LogInformation("Product {Name} created by {UserId}", dto.Name, userId);
        return MapToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto, string userId)
    {
        var product = await _uow.Products.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found.");

        product.Name = dto.Name;
        product.Slug = GenerateSlug(dto.Name);
        product.ShortDescription = dto.ShortDescription;
        product.Description = dto.Description;
        product.Specifications = dto.Specifications;
        product.Price = dto.Price;
        product.DiscountPrice = dto.DiscountPrice;
        product.CostPrice = dto.CostPrice;
        product.CategoryId = dto.CategoryId;
        product.MaterialType = dto.MaterialType;
        product.WidthCm = dto.WidthCm; product.HeightCm = dto.HeightCm;
        product.DepthCm = dto.DepthCm; product.WeightKg = dto.WeightKg;
        product.Color = dto.Color; product.FinishType = dto.FinishType;
        product.StockQuantity = dto.StockQuantity;
        product.IsAvailable = dto.IsAvailable;
        product.IsFeatured = dto.IsFeatured;
        product.IsCustomizable = dto.IsCustomizable;
        product.Tags = dto.Tags;
        product.UpdatedBy = userId;

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync();
        return MapToDto(product);
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var product = await _uow.Products.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found.");
        product.IsDeleted = true;
        product.DeletedAt = DateTime.UtcNow;
        product.DeletedBy = userId;
        _uow.Products.Update(product);
        await _uow.SaveChangesAsync();
    }

    public async Task<bool> UpdateStockAsync(int id, int quantity, string userId)
    {
        var product = await _uow.Products.GetByIdAsync(id);
        if (product == null) return false;
        product.StockQuantity = quantity;
        product.UpdatedBy = userId;
        _uow.Products.Update(product);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task IncrementViewCountAsync(int id)
    {
        var product = await _uow.Products.GetByIdAsync(id);
        if (product != null)
        {
            product.ViewCount++;
            _uow.Products.Update(product);
            await _uow.SaveChangesAsync();
        }
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLower()
            .Replace(" ", "-")
            .Replace("_", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-{2,}", "-");
        return $"{slug.Trim('-')}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id, Name = p.Name, Slug = p.Slug,
        ShortDescription = p.ShortDescription, Description = p.Description,
        Specifications = p.Specifications, Price = p.Price,
        DiscountPrice = p.DiscountPrice, EffectivePrice = p.EffectivePrice,
        CategoryId = p.CategoryId, CategoryName = p.Category?.Name ?? "",
        MaterialType = p.MaterialType, WidthCm = p.WidthCm, HeightCm = p.HeightCm,
        DepthCm = p.DepthCm, WeightKg = p.WeightKg, Color = p.Color,
        FinishType = p.FinishType, StockQuantity = p.StockQuantity,
        IsAvailable = p.IsAvailable, IsFeatured = p.IsFeatured,
        IsCustomizable = p.IsCustomizable, Sku = p.Sku,
        ThumbnailUrl = p.ThumbnailUrl ?? p.Images?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
        ViewCount = p.ViewCount, Tags = p.Tags, CreatedAt = p.CreatedAt,
        Images = p.Images?.Select(i => new ProductImageDto
        {
            Id = i.Id, ImageUrl = i.ImageUrl, AltText = i.AltText,
            IsPrimary = i.IsPrimary, SortOrder = i.SortOrder
        }).ToList() ?? new()
    };
}

// ============================================================
// CART SERVICE
// ============================================================
public class CartService : ICartService
{
    private readonly IUnitOfWork _uow;

    public CartService(IUnitOfWork uow) { _uow = uow; }

    public async Task<CartDto> GetCartAsync(string? userId, string? sessionId)
    {
        var cart = await _uow.Carts.GetWithItemsAsync(userId, sessionId);
        return cart == null ? new CartDto() : MapToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(string? userId, string? sessionId,
        int productId, int quantity, string? notes = null)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);
        var product = await _uow.Products.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException("Product not found.");

        if (!product.IsAvailable || product.StockQuantity < quantity)
            throw new InvalidOperationException("Product not available in requested quantity.");

        var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null)
        {
            existing.Quantity += quantity;
            if (existing.Quantity > product.StockQuantity)
                existing.Quantity = product.StockQuantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                CartId = cart.Id, ProductId = productId,
                Quantity = quantity, CustomizationNotes = notes
            });
        }

        await _uow.SaveChangesAsync();
        return await GetCartAsync(userId, sessionId);
    }

    public async Task<CartDto> UpdateItemAsync(string? userId, string? sessionId,
        int cartItemId, int quantity)
    {
        var cart = await _uow.Carts.GetWithItemsAsync(userId, sessionId);
        if (cart == null) return new CartDto();

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item != null)
        {
            if (quantity <= 0)
                cart.Items.Remove(item);
            else
                item.Quantity = quantity;
            await _uow.SaveChangesAsync();
        }
        return MapToDto(cart);
    }

    public async Task<CartDto> RemoveItemAsync(string? userId, string? sessionId, int cartItemId)
    {
        var cart = await _uow.Carts.GetWithItemsAsync(userId, sessionId);
        if (cart == null) return new CartDto();

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item != null)
        {
            cart.Items.Remove(item);
            await _uow.SaveChangesAsync();
        }
        return MapToDto(cart);
    }

    public async Task<CartDto> UpdateNotesAsync(string? userId, string? sessionId,
        int cartItemId, string? notes, string? height = null, string? width = null, string? color = null)
    {
        var cart = await _uow.Carts.GetWithItemsAsync(userId, sessionId);
        if (cart == null) return new CartDto();

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item != null)
        {
            item.CustomizationNotes = notes;
            item.Height = height;
            item.Width = width;
            item.Color = color;
            await _uow.SaveChangesAsync();
        }
        return MapToDto(cart);
    }

    public async Task ClearCartAsync(string? userId, string? sessionId)
    {
        var cart = await _uow.Carts.GetWithItemsAsync(userId, sessionId);
        if (cart != null)
        {
            cart.Items.Clear();
            await _uow.SaveChangesAsync();
        }
    }

    public async Task<CartDto> MergeCartAsync(string sessionId, string userId)
    {
        var sessionCart = await _uow.Carts.GetBySessionIdAsync(sessionId);
        var userCart = await _uow.Carts.GetByUserIdAsync(userId);

        if (sessionCart == null) return await GetCartAsync(userId, null);

        if (userCart == null)
        {
            sessionCart.UserId = userId;
            sessionCart.SessionId = null;
            await _uow.SaveChangesAsync();
        }
        else
        {
            foreach (var item in sessionCart.Items)
            {
                var existing = userCart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (existing != null)
                    existing.Quantity += item.Quantity;
                else
                    userCart.Items.Add(new CartItem
                    {
                        CartId = userCart.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    });
            }
            _uow.Carts.Remove(sessionCart);
            await _uow.SaveChangesAsync();
        }
        return await GetCartAsync(userId, null);
    }

    private async Task<Cart> GetOrCreateCartAsync(string? userId, string? sessionId)
    {
        var cart = await _uow.Carts.GetWithItemsAsync(userId, sessionId);
        if (cart != null) return cart;

        cart = new Cart { UserId = userId, SessionId = sessionId };
        await _uow.Carts.AddAsync(cart);
        await _uow.SaveChangesAsync();
        return cart;
    }

    private static CartDto MapToDto(Cart cart) => new()
    {
        Id = cart.Id,
        Items = cart.Items?.Select(i => new CartItemDto
        {
            Id = i.Id, ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? "",
            ProductThumbnail = i.Product?.ThumbnailUrl,
            ProductSlug = i.Product?.Slug,
            Quantity = i.Quantity,
            UnitPrice = i.Product?.EffectivePrice ?? 0,
            TotalPrice = (i.Product?.EffectivePrice ?? 0) * i.Quantity,
            CustomizationNotes = i.CustomizationNotes,
            Height = i.Height,
            Width = i.Width,
            Color = i.Color,
            IsAvailable = i.Product?.IsAvailable ?? false,
            StockQuantity = i.Product?.StockQuantity ?? 0
        }).ToList() ?? new(),
        SubTotal = cart.Items?.Sum(i => (i.Product?.EffectivePrice ?? 0) * i.Quantity) ?? 0,
        ItemCount = cart.Items?.Sum(i => i.Quantity) ?? 0
    };
}

// ============================================================
// INVENTORY SERVICE
// ============================================================
public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;

    public InventoryService(IUnitOfWork uow, INotificationService notifications)
    {
        _uow = uow; _notifications = notifications;
    }

    public async Task<IEnumerable<InventoryItemDto>> GetAllAsync()
    {
        var items = await _uow.Inventory.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<InventoryItemDto?> GetByIdAsync(int id)
    {
        var item = await _uow.Inventory.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    public async Task<IEnumerable<InventoryItemDto>> GetLowStockAsync()
    {
        var items = await _uow.Inventory.GetLowStockItemsAsync();
        return items.Select(MapToDto);
    }

    public async Task<InventoryItemDto> CreateAsync(CreateInventoryItemDto dto, string userId)
    {
        var item = new InventoryItem
        {
            Name = dto.Name, Description = dto.Description,
            Category = dto.Category, Unit = dto.Unit,
            CurrentStock = dto.CurrentStock, MinimumStock = dto.MinimumStock,
            ReorderPoint = dto.ReorderPoint, UnitCost = dto.UnitCost,
            Sku = dto.Sku, SupplierId = dto.SupplierId,
            Location = dto.Location, Notes = dto.Notes, CreatedBy = userId
        };
        await _uow.Inventory.AddAsync(item);
        await _uow.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task<InventoryItemDto> UpdateAsync(int id, UpdateInventoryItemDto dto, string userId)
    {
        var item = await _uow.Inventory.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Inventory item {id} not found.");

        item.Name = dto.Name; item.Description = dto.Description;
        item.Category = dto.Category; item.Unit = dto.Unit;
        item.MinimumStock = dto.MinimumStock; item.ReorderPoint = dto.ReorderPoint;
        item.UnitCost = dto.UnitCost; item.SupplierId = dto.SupplierId;
        item.Location = dto.Location; item.Notes = dto.Notes;
        item.UpdatedBy = userId;

        _uow.Inventory.Update(item);
        await _uow.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task RecordMovementAsync(int itemId, RecordMovementDto dto, string userId)
    {
        var item = await _uow.Inventory.GetByIdAsync(itemId)
            ?? throw new KeyNotFoundException($"Inventory item {itemId} not found.");

        var movement = new InventoryMovement
        {
            InventoryItemId = itemId, Type = dto.Type,
            Quantity = dto.Quantity, UnitCost = dto.UnitCost,
            Reference = dto.Reference, Notes = dto.Notes,
            RecordedById = userId
        };

        item.CurrentStock += dto.Type switch
        {
            MovementType.Purchase or MovementType.Return => dto.Quantity,
            MovementType.Usage or MovementType.Waste => -dto.Quantity,
            MovementType.Adjustment => dto.Quantity, // signed
            _ => 0
        };

        await _uow.Inventory.AddMovementAsync(movement);
        _uow.Inventory.Update(item);
        await _uow.SaveChangesAsync();

        if (item.CurrentStock <= item.MinimumStock)
            await _notifications.SendToAdminsAsync(
                "Low Stock Alert",
                $"{item.Name} is running low ({item.CurrentStock} {item.Unit} remaining)",
                NotificationType.LowStock);
    }

    public async Task<IEnumerable<InventoryMovementDto>> GetMovementsAsync(int itemId, int count = 20)
    {
        var movements = await _uow.Inventory.GetMovementsAsync(itemId, count);
        return movements.Select(m => new InventoryMovementDto
        {
            Id = m.Id, Type = m.Type, TypeName = m.Type.ToString(),
            Quantity = m.Quantity, UnitCost = m.UnitCost,
            Reference = m.Reference, Notes = m.Notes,
            RecordedBy = m.RecordedById, CreatedAt = m.CreatedAt
        });
    }

    private static InventoryItemDto MapToDto(InventoryItem i) => new()
    {
        Id = i.Id, Name = i.Name, Description = i.Description,
        Category = i.Category, CategoryName = i.Category.ToString(),
        Unit = i.Unit, CurrentStock = i.CurrentStock,
        MinimumStock = i.MinimumStock, UnitCost = i.UnitCost,
        SupplierName = i.Supplier?.Name,
        IsLowStock = i.CurrentStock <= i.MinimumStock,
        IsActive = i.IsActive, CreatedAt = i.CreatedAt
    };
}

// ============================================================
// CUSTOM REQUEST SERVICE
// ============================================================
public class CustomRequestService : ICustomRequestService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;
    private readonly INotificationService _notifications;

    public CustomRequestService(IUnitOfWork uow, IEmailService email,
        INotificationService notifications)
    { _uow = uow; _email = email; _notifications = notifications; }

    public async Task<CustomRequestDto?> GetByIdAsync(int id)
    {
        var req = await _uow.CustomRequests.GetByIdAsync(id);
        return req == null ? null : MapToDto(req);
    }

    public async Task<PagedResult<CustomRequestDto>> GetPagedAsync(int page, int pageSize,
        CustomRequestStatus? status = null)
    {
        var (items, total) = await _uow.CustomRequests.GetPagedAsync(page, pageSize, status);
        return new PagedResult<CustomRequestDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<CustomRequestDto> SubmitAsync(SubmitCustomRequestDto dto, string? userId = null)
    {
        var request = new CustomRequest
        {
            UserId = userId, CustomerName = dto.CustomerName,
            CustomerEmail = dto.CustomerEmail, CustomerPhone = dto.CustomerPhone,
            FurnitureType = dto.FurnitureType, Description = dto.Description,
            WidthCm = dto.WidthCm, HeightCm = dto.HeightCm, DepthCm = dto.DepthCm,
            PreferredMaterial = dto.PreferredMaterial,
            PreferredColor = dto.PreferredColor, BudgetRange = dto.BudgetRange,
            RequiredByDate = dto.RequiredByDate
        };

        foreach (var url in dto.UploadedImageUrls)
            request.Images.Add(new CustomRequestImage { ImageUrl = url });

        await _uow.CustomRequests.AddAsync(request);
        await _uow.SaveChangesAsync();

        _ = Task.Run(() => _notifications.SendToAdminsAsync(
            "New Custom Request",
            $"Custom furniture request from {dto.CustomerName} for {dto.FurnitureType}",
            NotificationType.CustomRequest));

        return MapToDto(request);
    }

    public async Task<CustomRequestDto> UpdateStatusAsync(int id, CustomRequestStatus status,
        string userId, string? notes = null)
    {
        var request = await _uow.CustomRequests.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Custom request {id} not found.");
        request.Status = status;
        if (notes != null) request.AdminNotes = notes;
        _uow.CustomRequests.Update(request);
        await _uow.SaveChangesAsync();
        return MapToDto(request);
    }

    public async Task<CustomRequestDto> SendQuoteAsync(int id, decimal quotedPrice, string userId)
    {
        var request = await _uow.CustomRequests.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Custom request {id} not found.");
        request.QuotedPrice = quotedPrice;
        request.Status = CustomRequestStatus.QuoteSent;
        _uow.CustomRequests.Update(request);
        await _uow.SaveChangesAsync();

        await _email.SendEmailAsync(request.CustomerEmail, "Your Custom Furniture Quote",
            $"<p>Dear {request.CustomerName},</p><p>Your quote for <strong>{request.FurnitureType}</strong> is <strong>EGP {quotedPrice:N2}</strong>. Please contact us to proceed.</p>");

        return MapToDto(request);
    }

    private static CustomRequestDto MapToDto(CustomRequest r) => new()
    {
        Id = r.Id, CustomerName = r.CustomerName,
        CustomerEmail = r.CustomerEmail, CustomerPhone = r.CustomerPhone,
        FurnitureType = r.FurnitureType, Description = r.Description,
        WidthCm = r.WidthCm, HeightCm = r.HeightCm, DepthCm = r.DepthCm,
        PreferredMaterial = r.PreferredMaterial, PreferredColor = r.PreferredColor,
        BudgetRange = r.BudgetRange, RequiredByDate = r.RequiredByDate,
        Status = r.Status, StatusName = r.Status.ToString(),
        QuotedPrice = r.QuotedPrice, AdminNotes = r.AdminNotes,
        ImageUrls = r.Images?.Select(i => i.ImageUrl).ToList() ?? new(),
        CreatedAt = r.CreatedAt
    };
}
