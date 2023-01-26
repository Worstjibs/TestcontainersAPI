using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestcontainersAPI.Data;
using TestcontainersAPI.Models;

namespace TestcontainersAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly TestDbContext _context;

    public CustomersController(TestDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers()
    {
        return Ok(await _context.Customers.ToListAsync());
    }

    [HttpGet("id")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.Id == id);

        if (customer is null)
            return NotFound();

        return Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
    {
        _context.Customers.Add(customer);

        await _context.SaveChangesAsync();

        return Created($"api/customers/{customer.Id}", customer);
    }
}
