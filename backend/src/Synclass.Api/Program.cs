using Microsoft.EntityFrameworkCore;
using Serilog;
using Synclass.Api.Logging;
using Synclass.Api.Middleware;
using Synclass.Domain.Common;
using Synclass.Domain.Usuarios;
using Synclass.Infrastructure.Common;
using Synclass.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.UseStructuredJsonLogging();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SynclassDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<CadastroProfessorService>();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
{
    app.Services.ApplyPendingMigrations();
}

app.UseSerilogRequestLogging();
app.UseTrackId();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

// Necessário para o WebApplicationFactory<Program> usado nos testes de integração.
public partial class Program
{
}
