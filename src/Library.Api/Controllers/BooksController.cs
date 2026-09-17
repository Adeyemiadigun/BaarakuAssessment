using Library.Api.Models;
using Library.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController(IBookService books) : ControllerBase
{
    [HttpPost("{id:int}/borrow")]
    public async Task<IActionResult> Borrow(int id, [FromBody] BorrowRequest request, CancellationToken cancellationToken)
    {
        var loan = await books.BorrowBookAsync(id, request.BorrowerEmail, cancellationToken);
        return Ok(loan);
    }

    [HttpPost("{id:int}/return")]
    public async Task<IActionResult> Return(int id, [FromBody] BorrowRequest request, CancellationToken cancellationToken)
    {
        var loan = await books.ReturnBookAsync(id, request.BorrowerEmail, cancellationToken);
        return Ok(loan);
    }
}