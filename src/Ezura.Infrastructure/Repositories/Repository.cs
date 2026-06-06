using System.Linq.Expressions;
using Ezura.Core.Interfaces.Repositories;
using Ezura.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ezura.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation using EF Core.
/// Provides standard CRUD and query capabilities with async support.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly EzuraDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(EzuraDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id) =>
        await _dbSet.FindAsync(id);

    public virtual async Task<IEnumerable<T>> GetAllAsync() =>
        await _dbSet.ToListAsync();

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await _dbSet.Where(predicate).ToListAsync();

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate) =>
        await _dbSet.FirstOrDefaultAsync(predicate);

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null) =>
        predicate == null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) =>
        await _dbSet.AnyAsync(predicate);

    public virtual async Task AddAsync(T entity) =>
        await _dbSet.AddAsync(entity);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities) =>
        await _dbSet.AddRangeAsync(entities);

    public virtual void Update(T entity) =>
        _dbSet.Update(entity);

    public virtual void Remove(T entity) =>
        _dbSet.Remove(entity);

    public virtual void RemoveRange(IEnumerable<T> entities) =>
        _dbSet.RemoveRange(entities);

    public virtual IQueryable<T> Query() =>
        _dbSet.AsQueryable();
}
