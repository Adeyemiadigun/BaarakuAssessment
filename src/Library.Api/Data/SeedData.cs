using Library.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Data;

public static class SeedData
{
    public static void EnsureSeeded(LibraryDbContext context)
    {
        if (context.Books.Any()) return;

        context.Books.AddRange(
            new Book { Id = 1, Title = "Dune", Author = "Frank Herbert", TotalCopies = 2 },
            new Book { Id = 2, Title = "Clean Code", Author = "Robert C. Martin", TotalCopies = 1 },
            new Book { Id = 3, Title = "The Pragmatic Programmer", Author = "Hunt & Thomas", TotalCopies = 3 });

        context.SaveChanges();
    }

    public static async Task EnsureSeededAsync(LibraryDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Books.AnyAsync(cancellationToken)) return;

        context.Books.AddRange(
            new Book { Id = 1, Title = "Dune", Author = "Frank Herbert", TotalCopies = 2 },
            new Book { Id = 2, Title = "Clean Code", Author = "Robert C. Martin", TotalCopies = 1 },
            new Book { Id = 3, Title = "The Pragmatic Programmer", Author = "Hunt & Thomas", TotalCopies = 3 });

        await context.SaveChangesAsync(cancellationToken);
    }
}
