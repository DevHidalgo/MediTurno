using MediTurno.Api.Data;
using MediTurno.Api.Middleware;
using MediTurno.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MediTurnoDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("MediTurno")));

builder.Services.AddSingleton<IRelojSistema, RelojSistema>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MediTurno API",
        Version = "v1",
        Description = "API de gestión de citas médicas para centros de atención ambulatoria."
    });
});

var app = builder.Build();

app.UseMiddleware<ManejadorExcepciones>();

app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "MediTurno API v1");
    opciones.DocumentTitle = "MediTurno API";
});

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MediTurnoDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapControllers();

app.Run();

public partial class Program;
