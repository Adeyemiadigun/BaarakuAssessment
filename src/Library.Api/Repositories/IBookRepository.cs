using Library.Api.Models;

namespace Library.Api.Repositories;

public interface IBookRepository
{
    Task<Book?> GetByIdWithLoansAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken = default);
}