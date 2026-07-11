using Microsoft.AspNetCore.Mvc;
using SmartBabySitter.Data;
using SmartBabySitter.Models;
using System;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/organizations")]
public class OrganizationRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrganizationRequestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 🔹 POST: api/organizations
    [HttpPost]
    public IActionResult Create([FromBody] OrganizationRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _context.OrganizationRequests.Add(model);
        _context.SaveChanges();

        return Ok(new { message = "Request submitted successfully" });
    }
}