using System.Text;
using MediTurno.Api.Common;
using MediTurno.Api.Data;
using MediTurno.Api.Entities;
using MediTurno.Api.Middleware;
using MediTurno.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SeccionConfiguracion));

builder.Services.AddDbContext<MediTurnoDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("MediTurno")));

builder.Services.AddSingleton<IRelojSistema, RelojSistema>();
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var jwt = builder.Configuration
    .GetSection(JwtOptions.SeccionConfiguracion)
    .Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            RoleClaimType = "role",
            NameClaimType = "email",
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
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

    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegue aquí el token obtenido en POST /api/auth/login."
    });

    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
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
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

    await db.Database.EnsureCreatedAsync();
    await DataSeeder.SembrarAsync(db, hasher);
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
