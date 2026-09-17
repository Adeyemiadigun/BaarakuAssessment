using Library.Api.Domain;
using Library.Api.Models;
using Library.Api.Repositories;
namespace Library.Api.Services;

public sealed class BookService(IBookRepository books, ILoanRepository loans) : IBookService
{
    public async Task<Loan> BorrowBookAsync(int bookId, string borrowerEmail, CancellationToken cancellationToken = default)
    {
        var book = await books.GetByIdWithLoansAsync(bookId, cancellationToken)
                   ?? throw new BookNotFoundException(bookId);

        var activeLoans = book.Loans.Count(l => l.ReturnedAt == null);
        if (activeLoans >= book.TotalCopies)
        {
            throw new NoCopiesAvailableException(bookId, book.TotalCopies);
        }

        if (book.Loans.Any(l => l.ReturnedAt == null
                                && l.BorrowerEmail.Equals(borrowerEmail, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateActiveLoanException(bookId, borrowerEmail);
        }

        var loan = new Loan
        {
            BookId = bookId,
            BorrowerEmail = borrowerEmail,
            BorrowedAt = DateTime.UtcNow,
            ReturnedAt = null,
        };

        loans.Add(loan);
        await loans.SaveChangesAsync(cancellationToken);
        return loan;
    }

    public async Task<Loan> ReturnBookAsync(int bookId, string borrowerEmail, CancellationToken cancellationToken = default)
    {
        var bookExists = await books.ExistsAsync(bookId, cancellationToken);
        if (!bookExists)
        {
            throw new BookNotFoundException(bookId);
        }

        var loan = await loans.GetMostRecentActiveAsync(bookId, borrowerEmail, cancellationToken)
                   ?? throw new ActiveLoanNotFoundException(bookId, borrowerEmail);

        loan.ReturnedAt = DateTime.UtcNow;
        await loans.SaveChangesAsync(cancellationToken);
        return loan;
    }
}