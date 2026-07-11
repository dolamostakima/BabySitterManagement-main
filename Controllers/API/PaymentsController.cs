using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBabySitter.Services;
using SmartBabySitter.Services.DTOs;

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