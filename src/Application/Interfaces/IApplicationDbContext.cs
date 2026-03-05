using Domain.Entities;

namespace Application.Interfaces;

public interface IApplicationDbContext
{
        IQueryable<Incident> Incidents { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}