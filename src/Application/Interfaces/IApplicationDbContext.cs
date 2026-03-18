using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces;

public interface IApplicationDbContext
{
        DbSet<Incident> Incidents { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}