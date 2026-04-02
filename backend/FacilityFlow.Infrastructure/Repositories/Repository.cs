using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext Db;
    protected readonly DbSet<T> DbSet;

    public Repository(AppDbContext db)
    {
        Db = db;
        DbSet = db.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id) => await DbSet.FindAsync(id);
    public async Task<List<T>> GetAllAsync() => await DbSet.ToListAsync();
    public IQueryable<T> Query() => DbSet.AsQueryable();
    public void Add(T entity) => DbSet.Add(entity);
    public void AddRange(IEnumerable<T> entities) => DbSet.AddRange(entities);
    public void Remove(T entity) => DbSet.Remove(entity);
    public void RemoveRange(IEnumerable<T> entities) => DbSet.RemoveRange(entities);
    public async Task<bool> ExistsAsync(Guid id) => await DbSet.FindAsync(id) != null;
    public async Task SaveChangesAsync() => await Db.SaveChangesAsync();
}
