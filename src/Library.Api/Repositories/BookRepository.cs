using Library.Api.Data;
using Library.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Repositories;

public sealed class BookRepository(LibraryDbContext context) : IBookRepository
{
    public Task<Book?> GetByIdWithLoansAsync(int id, CancellationToken cancellationToken = default)
        => context.Books
            .Include(b => b.Loans)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        => context.Books.AnyAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Books.AsNoTracking().ToListAsync(cancellationToken);
}