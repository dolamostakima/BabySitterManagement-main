using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBabySitter.Services;
using SmartBabySitter.Services.DTOs;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;

    public ReviewsController(IReviewService reviews)
    {
        _reviews = reviews;
    }

    // GET: /api/reviews/can-review?bookingId=1
    [HttpGet("can-review")]
    public async Task<IActionResult> CanReview([FromQuery] int bookingId)
        => Ok(await _reviews.CanReviewAsync(bookingId));

    // POST: /api/reviews
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReviewCreateDto dto)
    {
        try
        {
            var id = await _reviews.CreateAsync(dto);
            return Ok(new { reviewId = id });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> My()
    => Ok(await _reviews.GetMyReviewsAsync());

    // GET: /api/reviews/my-received?page=1&pageSize=50
    [Authorize(Roles = "BabySitter")]
    [HttpGet("my-received")]
    public async Task<IActionResult> MyReceived([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            return Ok(await _reviews.GetMyReceivedReviewsAsync(page, pageSize));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }


    [Authorize(Roles = "BabySitter")]
    [HttpPost("{reviewId:int}/reply")]
    public async Task<IActionResult> Reply(int reviewId, [FromBody] ReviewReplyDto dto)
    {
        try
        {
            await _reviews.ReplyAsync(reviewId, dto);
            return Ok(new { replied = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
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