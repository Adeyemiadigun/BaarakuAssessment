namespace Library.Api.Domain;

public abstract class LibraryDomainException : Exception
{
    protected LibraryDomainException(int statusCode, string title, string message) : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public int StatusCode { get; }
    public string Title { get; }
}

public sealed class BookNotFoundException(int bookId)
    : LibraryDomainException(StatusCodes.Status404NotFound, "Book not found", $"Book with id {bookId} was not found.")
{
}

public sealed class NoCopiesAvailableException(int bookId, int totalCopies)
    : LibraryDomainException(
        StatusCodes.Status409Conflict,
        "No copies available",
        $"Book with id {bookId} has no available copies (all {totalCopies} on loan).")
{
}

public sealed class DuplicateActiveLoanException(int bookId, string borrowerEmail)
    : LibraryDomainException(
        StatusCodes.Status409Conflict,
        "Duplicate loan",
        $"Borrower '{borrowerEmail}' already has an unreturned loan for book with id {bookId}.")
{
}

public sealed class ActiveLoanNotFoundException(int bookId, string borrowerEmail)
    : LibraryDomainException(
        StatusCodes.Status404NotFound,
        "No active loan",
        $"Borrower '{borrowerEmail}' has no unreturned loan for book with id {bookId}.")
{
}