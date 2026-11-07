using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;

namespace SistemaOS.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
    {
        var query = _dbSet.AsQueryable();
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        var query = _dbSet.Where(predicate);
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return await query.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        // Verifica se a entidade já está sendo trackeada pelo contexto
        var entityType = typeof(T);
        var idProperty = entityType.GetProperty("Id");
        if (idProperty == null)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return;
        }

        var entityId = idProperty.GetValue(entity);
        var existingEntry = _context.ChangeTracker.Entries<T>()
            .FirstOrDefault(e =>
            {
                var entryId = idProperty.GetValue(e.Entity);
                return entryId != null && entryId.Equals(entityId);
            });

        if (existingEntry != null)
        {
            // Entidade já trackeada - copia os valores e salva
            existingEntry.CurrentValues.SetValues(entity);
        }
        else
        {
            // Entidade não trackeada - anexa normalmente
            _dbSet.Update(entity);
        }
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null)
            return await _dbSet.CountAsync();
        return await _dbSet.CountAsync(predicate);
    }

    public virtual IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }
}