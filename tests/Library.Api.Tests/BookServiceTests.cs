using Library.Api.Data;
using Library.Api.Domain;
using Library.Api.Models;
using Library.Api.Repositories;
using Library.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Tests;

public sealed class BookServiceTests : IDisposable
{
    private readonly LibraryDbContext _context;
    private readonly BookService _service;

    public BookServiceTests()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new LibraryDbContext(options);
        _context.Books.AddRange(
            new Book { Id = 1, Title = "Dune", Author = "Frank Herbert", TotalCopies = 3 },
            new Book { Id = 2, Title = "Clean Code", Author = "Robert C. Martin", TotalCopies = 1 });
        _context.SaveChanges();

        var bookRepo = new BookRepository(_context);
        var loanRepo = new LoanRepository(_context);
        _service = new BookService(bookRepo, loanRepo);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task BorrowBook_WithCopiesAvailable_CreatesLoan()
    {
        var loan = await _service.BorrowBookAsync(1, "alice@example.com");
        Assert.NotNull(loan);
        Assert.Equal(1, loan.BookId);
        Assert.Equal("alice@example.com", loan.BorrowerEmail);
        Assert.Null(loan.ReturnedAt);
        Assert.True((DateTime.UtcNow - loan.BorrowedAt).Duration() < TimeSpan.FromSeconds(5));
        Assert.Equal(1, await _context.Loans.CountAsync());
    }

    [Fact]
    public async Task BorrowBook_WhenBookDoesNotExist_ThrowsBookNotFound()
    {
        var ex = await Assert.ThrowsAsync<BookNotFoundException>(() => _service.BorrowBookAsync(999, "alice@example.com"));
        Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task BorrowBook_WhenNoCopiesAvailable_ThrowsNoCopiesAvailable()
    {
        await _service.BorrowBookAsync(2, "alice@example.com");
        var ex = await Assert.ThrowsAsync<NoCopiesAvailableException>(() => _service.BorrowBookAsync(2, "bob@example.com"));
        Assert.Equal(StatusCodes.Status409Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task BorrowBook_WhenSameBorrowerHasActiveLoan_ThrowsDuplicateLoan()
    {
        await _service.BorrowBookAsync(1, "alice@example.com");
        var ex = await Assert.ThrowsAsync<DuplicateActiveLoanException>(() => _service.BorrowBookAsync(1, "alice@example.com"));
        Assert.Equal(StatusCodes.Status409Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task BorrowBook_WithMultipleCopies_DifferentBorrowersAreAllowed()
    {
        await _service.BorrowBookAsync(1, "alice@example.com");
        await _service.BorrowBookAsync(1, "bob@example.com");
        Assert.Equal(2, await _context.Loans.CountAsync());
    }

    [Fact]
    public async Task BorrowBook_ComparesBorrowerEmailsCaseInsensitively()
    {
        await _service.BorrowBookAsync(1, "Alice@Example.com");
        await Assert.ThrowsAsync<DuplicateActiveLoanException>(() => _service.BorrowBookAsync(1, "alice@example.com"));
    }

    [Fact]
    public async Task ReturnBook_MarksMostRecentUnreturnedLoanAsReturned()
    {
        var now = DateTime.UtcNow;
        _context.Loans.AddRange(
            new Loan { BookId = 1, BorrowerEmail = "alice@example.com", BorrowedAt = now.AddDays(-5) },
            new Loan { BookId = 1, BorrowerEmail = "alice@example.com", BorrowedAt = now.AddDays(-2) },
            new Loan { BookId = 1, BorrowerEmail = "bob@example.com", BorrowedAt = now.AddDays(-1) });
        await _context.SaveChangesAsync();
        var mostRecentAliceLoan = await _context.Loans.Where(l => l.BorrowerEmail == "alice@example.com").OrderByDescending(l => l.BorrowedAt).FirstAsync();
        var returned = await _service.ReturnBookAsync(1, "alice@example.com");
        Assert.Equal(mostRecentAliceLoan.Id, returned.Id);
        Assert.NotNull(returned.ReturnedAt);
        Assert.Equal(2, await _context.Loans.CountAsync(l => l.ReturnedAt == null));
    }

    [Fact]
    public async Task ReturnBook_WhenBookDoesNotExist_ThrowsBookNotFound()
    {
        var ex = await Assert.ThrowsAsync<BookNotFoundException>(() => _service.ReturnBookAsync(999, "alice@example.com"));
        Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task ReturnBook_WhenNoActiveLoan_ThrowsActiveLoanNotFound()
    {
        var ex = await Assert.ThrowsAsync<ActiveLoanNotFoundException>(() => _service.ReturnBookAsync(1, "alice@example.com"));
        Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
    }
}
