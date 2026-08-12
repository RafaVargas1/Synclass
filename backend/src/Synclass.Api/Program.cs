using Microsoft.EntityFrameworkCore;
using Serilog;
using Synclass.Api.Logging;
using Synclass.Api.Middleware;
using Synclass.Domain.Common;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
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

const string FrontendCorsPolicy = "FrontendCorsPolicy";
var origensFrontendPermitidas = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    // O frontend (Expo web) roda em origem diferente da Api durante o
    // desenvolvimento (porta 8081 vs 5005/8080), então o navegador bloqueia
    // o fetch sem essa liberação explícita — ver
    // backend/src/Synclass.Api/appsettings.Development.json.
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(origensFrontendPermitidas).AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<CadastroProfessorService>();
builder.Services.AddScoped<IHorarioRepository, HorarioRepository>();
builder.Services.AddScoped<HorarioService>();
builder.Services.AddScoped<IMatriculaRepository, MatriculaRepository>();
builder.Services.AddScoped<CadastroAlunoProvisorioService>();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
{
    app.Services.ApplyPendingMigrations();
}

app.UseSerilogRequestLogging();
app.UseTrackId();
app.UseCors(FrontendCorsPolicy);

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
