using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces;

public interface IApplicationDbContext
{
        // DbSet<Incident> Incidents { get; }
        IQueryable<Incident> Incidents { get; }
        void AddEntity<T>(T entity) where T : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}