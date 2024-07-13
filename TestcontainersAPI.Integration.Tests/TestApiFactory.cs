using Bogus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Respawn;
using System.Data.Common;
using Testcontainers.MsSql;
using TestcontainersAPI.Data;

namespace TestcontainersAPI.Integration.Tests;

public class TestApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer
        = new MsSqlBuilder()
            .WithPassword("password123$")
            .WithPortBinding(1433, 1433)
            .WithName("sql1")
            .WithBindMount($"{AppDomain.CurrentDomain.BaseDirectory}/sql/data", "/var/opt/mssql/data")
            .Build();

    private Respawner _respawner = default!;
    private DbConnection _dbConnection = default!;

    public HttpClient HttpClient { get; set; } = default!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            var connectionString = _dbContainer.GetConnectionString();

            services.RemoveAll<TestDbContext>();
            services.RemoveAll<DbContextOptions<TestDbContext>>();

            services.AddDbContext<TestDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
        });
    }

    public async Task InitializeAsync()
    {
        Randomizer.Seed = new Random(10);

        await _dbContainer.StartAsync();

        HttpClient = CreateClient();

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        _dbConnection = new SqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["dbo"]
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    public async Task ResetDb()
    {
        await _respawner.ResetAsync(_dbConnection);
    }
}