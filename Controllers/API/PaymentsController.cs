using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBabySitter.Services;
using SmartBabySitter.Services.DTOs;
using System.Security.Claims;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments)
    {
        _payments = payments;
    }


    [Authorize]
    [HttpGet("booking/{bookingId:int}")]
    public async Task<IActionResult> GetByBooking(int bookingId)
    {
        var payment = await _payments.GetPaymentByBookingAsync(bookingId);

        if (payment == null)
            return NotFound();

        return Ok(payment);
    }
    [Authorize(Roles = "Parent")]
    [HttpGet("parent/history")]
    public async Task<IActionResult> ParentHistory()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _payments.GetParentPaymentHistoryAsync(userId);

        return Ok(result);
    }

    [Authorize(Roles = "BabySitter")]
    [HttpGet("sitter/history")]
    public async Task<IActionResult> SitterHistory()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _payments.GetSitterPaymentHistoryAsync(userId);

        return Ok(result);
    }

    // Usually Parent/Admin creates payment record (depends on your flow)
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(PaymentCreateDto dto)
    {
        try
        {
            var id = await _payments.CreatePaymentAsync(dto);
            return Ok(new { paymentId = id });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Admin updates payment status (Paid/Failed etc.)
    [Authorize(Roles = "Admin")]
    [HttpPut("{paymentId:int}")]
    public async Task<IActionResult> Update(int paymentId, PaymentUpdateDto dto)
    {
        try
        {
            await _payments.UpdatePaymentAsync(paymentId, dto);
            return Ok(new { updated = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}