using Library.Api.Data;
using Library.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Repositories;

public sealed class LoanRepository(LibraryDbContext context) : ILoanRepository
{
    public Task<Loan?> GetMostRecentActiveAsync(int bookId, string borrowerEmail, CancellationToken cancellationToken = default)
        => context.Loans
            .Where(l => l.BookId == bookId
                        && l.ReturnedAt == null
                        && l.BorrowerEmail.Equals(borrowerEmail, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(l => l.BorrowedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(Loan loan) => context.Loans.Add(loan);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}