using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace SupplySmart.Integrations.Repositories;

public interface IBaseRepository<T> where T : class
{
    Task<T?> GetAsync(object primaryKey);
    Task<T?> GetByConditionAsync(Expression<Func<T, bool>> expression);
    Task<T?> GetByConditionAsync(Expression<Func<T, bool>> expression, IEnumerable<string> includes, bool includeTracking = false);
    Task<IList<T>> GetAllByConditionAsync(Expression<Func<T, bool>> expression, IEnumerable<string> includes = null!);
    Task<IList<T>> GetAllAsync(IEnumerable<string> includes = null!, bool includeTracking = false);
    Task<T> AddAsync(T obj);
    Task AddRangeAsync(IEnumerable<T> obj);
    Task<T> UpdateAsync(T obj);
    Task DeleteAsync(object primaryKey);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> expression);
    IQueryable<T> GetQueryable();
}

public class BaseRepository<T, TContext>(TContext context) : IBaseRepository<T> where T : class where TContext : DbContext
{
    private readonly DbContext _context = context;

    public async Task<T?> GetAsync(object primaryKey)
    {
        var entity = await _context.Set<T>().FindAsync(primaryKey);
        if (entity == null) return null;
        _context.Entry(entity).State = EntityState.Detached;
        return entity;
    }

    public async Task<T?> GetByConditionAsync(Expression<Func<T, bool>> expression)
    {
        return await _context.Set<T>().AsNoTracking().Where(expression).FirstOrDefaultAsync();
    }

    public async Task<T?> GetByConditionAsync(Expression<Func<T, bool>> expression, IEnumerable<string> includes, bool includeTracking = false)
    {
        var query = _context.Set<T>().AsQueryable();
        if (includes != null)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }
        return includeTracking ? await query.FirstOrDefaultAsync(expression) : await query.AsNoTracking().FirstOrDefaultAsync(expression);
    }

    public async Task<IList<T>> GetAllByConditionAsync(Expression<Func<T, bool>> expression, IEnumerable<string> includes = null!)
    {
        var query = _context.Set<T>().AsQueryable();
        if (includes != null)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }
        return await query.AsNoTracking().Where(expression).ToListAsync();
    }

    public async Task<IList<T>> GetAllAsync(IEnumerable<string> includes = null!, bool includeTracking = false)
    {
        var query = _context.Set<T>().AsQueryable();
        if (includes != null)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }
        return includeTracking ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
    }

    public async Task<T> AddAsync(T obj)
    {
        var entity = await _context.AddAsync(obj);
        await _context.SaveChangesAsync();
        return entity.Entity;
    }

    public async Task AddRangeAsync(IEnumerable<T> obj)
    {
        await _context.AddRangeAsync(obj);
        await _context.SaveChangesAsync();
    }

    public async Task<T> UpdateAsync(T obj)
    {
        var result = _context.Update(obj);
        await _context.SaveChangesAsync();
        return result.Entity;
    }

    public async Task DeleteAsync(object primaryKey)
    {
        var entity = await _context.Set<T>().FindAsync(primaryKey);
        if (entity == null) return;
        _context.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> expression)
    {
        return await _context.Set<T>().AnyAsync(expression);
    }

    public IQueryable<T> GetQueryable()
    {
        return _context.Set<T>().AsQueryable();
    }
}
