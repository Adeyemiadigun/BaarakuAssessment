using System.ComponentModel.DataAnnotations;

namespace Library.Api.Models;

public sealed class BorrowRequest
{
    [Required(ErrorMessage = "BorrowerEmail is required.")]
    [EmailAddress(ErrorMessage = "BorrowerEmail must be a valid email address.")]
    public string BorrowerEmail { get; set; } = string.Empty;
}