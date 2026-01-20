using LifeHub.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationContext>();
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/ef/tables", (ApplicationContext db) =>
{
    var tables = db.Model
        .GetEntityTypes()
        .Select(t => t.GetTableName())
        .Distinct();

    return Results.Ok(tables);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
