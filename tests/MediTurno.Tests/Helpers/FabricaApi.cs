using System.Net.Http.Headers;
using System.Net.Http.Json;
using MediTurno.Api.Data;
using MediTurno.Api.Dtos;
using MediTurno.Api.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediTurno.Tests.Helpers;

public class FabricaApi : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string CorreoAdministrador = "admin@mediturno.do";
    public const string ClaveAdministrador = "Admin123*";
    public const string CorreoRecepcionista = "recepcion@mediturno.do";
    public const string ClaveRecepcionista = "Recepcion123*";
    public const string CorreoMedico = "carmen.reyes@mediturno.do";
    public const string ClaveMedico = "Medico123*";

    private readonly string _cadenaConexion =
        $"DataSource=file:mediturno-{Guid.NewGuid():N}?mode=memory&cache=shared";

    private readonly SqliteConnection _conexion;

    public FabricaApi()
    {
        _conexion = new SqliteConnection(_cadenaConexion);
        _conexion.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(servicios =>
        {
            var descriptores = servicios
                .Where(d => d.ServiceType == typeof(DbContextOptions<MediTurnoDbContext>)
                            || d.ServiceType == typeof(DbContextOptions)
                            || d.ServiceType == typeof(MediTurnoDbContext)
                            || (d.ServiceType.IsGenericType
                                && d.ServiceType.GetGenericTypeDefinition().Name
                                    .StartsWith("IDbContextOptionsConfiguration")))
                .ToList();

            foreach (var descriptor in descriptores)
            {
                servicios.Remove(descriptor);
            }

            servicios.AddDbContext<MediTurnoDbContext>(opciones => opciones.UseSqlite(_cadenaConexion));
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MediTurnoDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

        await db.Database.EnsureCreatedAsync();
        await DataSeeder.SembrarAsync(db, hasher);
    }

    public async Task<HttpClient> ClienteComoAsync(string correo, string password)
    {
        var cliente = CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Correo = correo,
            Password = password
        });

        respuesta.EnsureSuccessStatusCode();

        var login = await respuesta.Content.ReadFromJsonAsync<LoginResponse>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);

        return cliente;
    }

    public Task<HttpClient> ClienteAdministradorAsync() =>
        ClienteComoAsync(CorreoAdministrador, ClaveAdministrador);

    public Task<HttpClient> ClienteRecepcionistaAsync() =>
        ClienteComoAsync(CorreoRecepcionista, ClaveRecepcionista);

    public Task<HttpClient> ClienteMedicoAsync() =>
        ClienteComoAsync(CorreoMedico, ClaveMedico);

    public MediTurnoDbContext CrearContexto()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MediTurnoDbContext>();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _conexion.DisposeAsync();
        await base.DisposeAsync();
    }
}
