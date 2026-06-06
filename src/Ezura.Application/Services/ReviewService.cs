using Ezura.Core.DTOs;
using Ezura.Core.Entities;
using Ezura.Core.Interfaces.Repositories;
using Ezura.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Ezura.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _uow;

    public ReviewService(IUnitOfWork uow) { _uow = uow; }

    public async Task<ReviewDto> SubmitReviewAsync(string userId, SubmitReviewDto dto)
    {
        var product = await _uow.Products.Query().FirstOrDefaultAsync(p => p.Id == dto.ProductId);
        if (product == null) throw new ArgumentException("Product not found");

        var review = new Review
        {
            ProductId = dto.ProductId,
            UserId = userId,
            Rating = Math.Clamp(dto.Rating, 1, 5),
            Title = dto.Title,
            Body = dto.Body,
            IsApproved = false,
            IsVerifiedPurchase = await HasUserPurchasedProduct(userId, dto.ProductId)
        };

        await _uow.Reviews.AddAsync(review);
        await _uow.SaveChangesAsync();

        return MapToDto(review, product.Name, "");
    }

    public async Task<IEnumerable<ReviewDto>> GetByProductIdAsync(int productId, bool approvedOnly = true)
    {
        var query = _uow.Reviews.Query()
            .Include(r => r.User)
            .Include(r => r.Product)
            .Where(r => r.ProductId == productId);

        if (approvedOnly) query = query.Where(r => r.IsApproved);

        var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return reviews.Select(r => MapToDto(r, r.Product?.Name ?? "", r.User?.FullName ?? ""));
    }

    public async Task<IEnumerable<ReviewDto>> GetAllPendingAsync()
    {
        var reviews = await _uow.Reviews.Query()
            .Include(r => r.User)
            .Include(r => r.Product)
            .Where(r => !r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reviews.Select(r => MapToDto(r, r.Product?.Name ?? "", r.User?.FullName ?? ""));
    }

    public async Task<IEnumerable<ReviewDto>> GetAllAsync(bool approvedOnly = false)
    {
        IQueryable<Ezura.Core.Entities.Review> query = _uow.Reviews.Query()
            .Include(r => r.User)
            .Include(r => r.Product);

        if (approvedOnly)
            query = query.Where(r => r.IsApproved);

        var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

        return reviews.Select(r => MapToDto(r, r.Product?.Name ?? "", r.User?.FullName ?? ""));
    }

    public async Task ApproveAsync(int reviewId)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId);
        if (review != null)
        {
            review.IsApproved = true;
            _uow.Reviews.Update(review);
            await _uow.SaveChangesAsync();
        }
    }

    public async Task RejectAsync(int reviewId)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId);
        if (review != null)
        {
            _uow.Reviews.Remove(review);
            await _uow.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int reviewId)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId);
        if (review != null)
        {
            _uow.Reviews.Remove(review);
            await _uow.SaveChangesAsync();
        }
    }

    private async Task<bool> HasUserPurchasedProduct(string userId, int productId)
    {
        var orders = await _uow.Orders.GetByUserIdAsync(userId);
        return orders.Any(o => o.Items?.Any(i => i.ProductId == productId) == true);
    }

    private static ReviewDto MapToDto(Review r, string productName, string userName) => new()
    {
        Id = r.Id,
        ProductId = r.ProductId,
        ProductName = productName,
        UserId = r.UserId,
        UserName = userName,
        Rating = r.Rating,
        Title = r.Title,
        Body = r.Body,
        IsApproved = r.IsApproved,
        CreatedAt = r.CreatedAt
    };
}
