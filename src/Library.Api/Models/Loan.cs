namespace Library.Api.Models;

public class Loan
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BorrowerEmail { get; set; } = string.Empty;
    public DateTime BorrowedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
}