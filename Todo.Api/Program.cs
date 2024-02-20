using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Todo.Api;
using Todo.Api.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    // .AddCheck<SqlServerHealthCheck>("sqlserver")
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddCheck<RedisHealthCheck>("redis");

builder.Services.AddDbContext<TodoDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    );
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetService<TodoDbContext>();
db?.Database.EnsureCreatedAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/todo", async (TodoDbContext context) =>
    await context.Todos.ToListAsync()
);

app.MapGet("/todo/{id:int}", async (int id, TodoDbContext context) =>
    await context.Todos.FindAsync(id)
    is { } todo
    ? Results.Ok(todo)
    : Results.NotFound());

app.MapPost("/todo", async (TodoItem todo, TodoDbContext context) =>
{
    await context.Todos.AddAsync(todo);
    await context.SaveChangesAsync();

    return Results.Created($"/todo/{todo.Id}", todo);
});

app.MapHealthChecks(pattern: "_health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();