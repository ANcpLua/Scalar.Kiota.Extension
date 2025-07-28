using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Scalar.Kiota.Extension.Tests;

public class TodosWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}

[ArgumentDisplayFormatter<UniversalArgumentFormatter>]
public class TodosApiIntegrationTests
{
    private readonly TodosWebAppFactory _factory = new();

    [Test]
    public async Task TodosEndpoint_ReturnsSuccess_WhenScalarKiotaConfigured()
    {
        await using var sut = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddScalarWithKiota(options =>
                    {
                        options.WithTitle("Test API")
                            .WithSdkName("TestClient");
                    });
                });
            });

        var client = sut.CreateClient();

        var response = await client.GetAsync("/todos");

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    [DisplayName("GET /todos - Returns all todos")]
    public async Task GetTodos_ReturnsAllTodos()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/todos", TestContext.Current!.CancellationToken);

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        var todos = await response.Content.ReadFromJsonAsync<Todo[]>(
            TestContext.Current.CancellationToken);

        await Assert.That(todos).IsNotNull();
        await Assert.That(todos!.Length).IsGreaterThanOrEqualTo(5);
        await Assert.That(todos[^1].Title).IsEqualTo("Clean the car");
    }

    [Test]
    [Arguments(1, "Walk the dog")]
    [Arguments(5, "Clean the car")]
    [DisplayName("GET /todos/$id - Returns correct todo (id: $id)")]
    public async Task GetTodoById_ReturnsCorrectTodo(int id, string expectedTitle)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/todos/{id}", TestContext.Current!.CancellationToken);

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        var todo = await response.Content.ReadFromJsonAsync<Todo>(
            TestContext.Current.CancellationToken);

        await Assert.That(todo).IsNotNull();
        await Assert.That(todo!.Id).IsEqualTo(id);
        await Assert.That(todo.Title).IsEqualTo(expectedTitle);
    }

    [Test]
    [Arguments(9999)]
    [DisplayName("GET /todos/$id - Non-existent todo returns 404 (id: $id)")]
    public async Task GetTodoById_NonExistent_ReturnsNotFound(int id)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/todos/{id}", TestContext.Current!.CancellationToken);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);