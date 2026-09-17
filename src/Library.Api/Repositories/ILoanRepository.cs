using Library.Api.Models;

namespace Library.Api.Repositories;

public interface ILoanRepository
{
    Task<Loan?> GetMostRecentActiveAsync(int bookId, string borrowerEmail, CancellationToken cancellationToken = default);
    void Add(Loan loan);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}