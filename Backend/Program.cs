using LifeHub.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationContext>();


var app = builder.Build();

app.MapGet("/ef/tables", (ApplicationContext db) =>
{
    var tables = db.Model
        .GetEntityTypes()
        .Select(t => t.GetTableName())
        .Distinct();

    return Results.Ok(tables);
});

app.UseHttpsRedirection();
app.Run();
