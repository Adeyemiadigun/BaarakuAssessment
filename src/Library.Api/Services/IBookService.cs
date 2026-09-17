using Library.Api.Models;

namespace Library.Api.Services;

public interface IBookService
{
    Task<Loan> BorrowBookAsync(int bookId, string borrowerEmail, CancellationToken cancellationToken = default);
    Task<Loan> ReturnBookAsync(int bookId, string borrowerEmail, CancellationToken cancellationToken = default);
}