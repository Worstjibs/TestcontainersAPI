using Microsoft.EntityFrameworkCore;
using TestcontainersAPI.Models;

namespace TestcontainersAPI.Data;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Customer>  Customers { get; set; }
}
