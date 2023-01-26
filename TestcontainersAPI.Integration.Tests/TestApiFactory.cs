using Bogus;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Respawn;
using System.Data.Common;
using TestcontainersAPI.Data;

namespace TestcontainersAPI.Integration.Tests;

public class TestApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    private readonly TestcontainerDatabase _dbContainer
        = new TestcontainersBuilder<MsSqlTestcontainer>()
            .WithDatabase(new MsSqlTestcontainerConfiguration
            {
                Password = "password123$"
            })
            .WithPortBinding(1433, 1433)
            .WithName("sql1")
            .Build();

    private Respawner _respawner = default!;
    private DbConnection _dbConnection = default!;

    private string ConnectionString => $"{_dbContainer.ConnectionString}Encrypt=false;TrustServerCertificate=true";

    public HttpClient HttpClient { get; set; } = default!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var connectionString = _dbContainer.ConnectionString + "Encrypt=false;TrustServerCertificate=true";

            services.RemoveAll<TestDbContext>();
            services.RemoveAll<DbContextOptions<TestDbContext>>();

            services.AddDbContext<TestDbContext>(options =>
            {
                options.UseSqlServer(ConnectionString);
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
            await context.Database.MigrateAsync();
        }

        _dbConnection = new SqlConnection(ConnectionString);
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = new[] { "dbo" }
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