using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TestcontainersAPI.Data;
using TestcontainersAPI.Models;

namespace TestcontainersAPI.Integration.Tests;

[Collection("Shared collection")]
public class CustomersControllerTests : IAsyncLifetime
{
    private readonly TestApiFactory _waf;
    private readonly HttpClient _client;

    private Faker<Customer> _customerFaker = new Faker<Customer>()
        .RuleFor(x => x.Name, f => f.Name.FullName())
        .RuleFor(x => x.Age, f => f.Random.Number(18, 55));

    public CustomersControllerTests(TestApiFactory waf)
    {
        _waf = waf;
        _client = waf.HttpClient;
    }

    [Fact]
    public async Task CreateCustomer_ShouldAddACustomerToTheDb()
    {
        // Arrange
        var customer = _customerFaker.Generate();

        // Act
        var response = await _client.PostAsJsonAsync("api/customers", customer);
        var customerResponse = await response.Content.ReadFromJsonAsync<Customer>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().Be($"api/customers/{customerResponse!.Id}");
        var savedCustomer = await GetCustomerFromContext(customerResponse!.Id);
        customerResponse.Should().BeEquivalentTo(savedCustomer);
    }

    [Fact]
    public async Task GetCustomers_ShouldReturnListOfCustomers()
    {
        // Arrange
        var customers = await CreateCustomers();

        // Act
        var response = await _client.GetAsync("api/customers");
        var customersResponse = await response.Content.ReadFromJsonAsync<Customer[]>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        customersResponse.Should().BeEquivalentTo(customers);
    }

    private async Task<IEnumerable<Customer>> CreateCustomers()
    {
        var customers = _customerFaker.Generate(10);

        using var scope = _waf.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return customers;
    }

    private async Task<Customer?> GetCustomerFromContext(int id)
    {
        using var scope = _waf.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        return await context.Customers.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _waf.ResetDb();
}