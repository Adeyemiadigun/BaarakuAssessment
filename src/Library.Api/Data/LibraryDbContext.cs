using Microsoft.EntityFrameworkCore;
using Library.Api.Models;

namespace Library.Api.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var book = modelBuilder.Entity<Book>();
        book.Property(b => b.Title).IsRequired().HasMaxLength(200);
        book.Property(b => b.Author).IsRequired().HasMaxLength(200);

        var loan = modelBuilder.Entity<Loan>();
        loan.Property(l => l.BorrowerEmail).IsRequired().HasMaxLength(512);
        loan.HasIndex(l => l.BookId);
        loan.HasIndex(l => new { l.BookId, l.BorrowerEmail });
    }
}