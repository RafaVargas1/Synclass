using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Synclass.Api.Logging;
using Synclass.Api.Middleware;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Common;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Convites;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;
using Synclass.Infrastructure.Autenticacao;
using Synclass.Infrastructure.Common;
using Synclass.Infrastructure.Convites;
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

// Autenticação/autorização por papel (issue #4) — validação do mesmo token
// JWT já emitido por GeradorDeTokenSessaoJwt (login por OTP, issue #18),
// deixada propositalmente de fora até esta issue por não haver, até então,
// endpoint algum que precisasse validar o token (ver
// docs/specs/4-usuario-acumula-papeis/implementation.md).
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Configuração ausente: Jwt:SigningKey.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<CadastroProfessorService>();
builder.Services.AddScoped<AtualizacaoNomeUsuarioService>();
builder.Services.AddScoped<IConfiguracaoProfessorRepository, ConfiguracaoProfessorRepository>();
builder.Services.AddScoped<ConfiguracaoProfessorService>();
builder.Services.AddScoped<IHorarioRepository, HorarioRepository>();
builder.Services.AddScoped<HorarioService>();
builder.Services.AddScoped<IMatriculaRepository, MatriculaRepository>();
builder.Services.AddScoped<CadastroAlunoProvisorioService>();
builder.Services.AddScoped<IRegraDeCobrancaRepository, RegraDeCobrancaRepository>();
builder.Services.AddScoped<RegraDeCobrancaService>();

// Consulta de valor devido por Aluno (issue #12) — ver
// docs/specs/12-valor-devido-professor/implementation.md.
builder.Services.AddScoped<ConsultaCobrancaService>();

// Alocação de Aluno a horário específico (issue #8) — ver
// docs/specs/8-aluno-horario/implementation.md.
builder.Services.AddScoped<IAlocacaoHorarioRepository, AlocacaoHorarioRepository>();
builder.Services.AddScoped<AlocacaoHorarioService>();

// Login por OTP (issue #18) — ver docs/specs/18-login-otp/implementation.md.
builder.Services.AddScoped<ICodigoOtpRepository, CodigoOtpRepository>();
builder.Services.AddSingleton<IGeradorDeCodigoOtp, GeradorDeCodigoOtp>();
builder.Services.AddScoped<INotificador, NotificadorDeLog>();
builder.Services.AddSingleton<IGeradorDeTokenSessao>(sp => new GeradorDeTokenSessaoJwt(
    jwtSigningKey,
    LerExpiracaoDiasObrigatoria(builder.Configuration),
    sp.GetRequiredService<IClock>()));
builder.Services.AddScoped<LoginService>();

// Convite de Aluno via WhatsApp (issue #2) — ver
// docs/specs/2-convite-whatsapp/implementation.md.
builder.Services.AddScoped<IConviteRepository, ConviteRepository>();
builder.Services.AddSingleton<IGeradorDeTokenConvite, GeradorDeTokenConvite>();
builder.Services.AddScoped(sp => new ConviteService(
    sp.GetRequiredService<IConviteRepository>(),
    sp.GetRequiredService<IMatriculaRepository>(),
    sp.GetRequiredService<IUsuarioRepository>(),
    sp.GetRequiredService<IGeradorDeTokenConvite>(),
    sp.GetRequiredService<IClock>(),
    LerDiasValidadeConviteObrigatoria(builder.Configuration)));

var app = builder.Build();

// Falha explícita no startup, mesmo padrão de Jwt:SigningKey acima — sem
// isso, GetValue<int> devolvia 0 silenciosamente quando a config estava
// ausente ou mal formatada (dev-review do PR #25, issue #18).
static int LerExpiracaoDiasObrigatoria(IConfiguration configuration)
{
    var valorBruto = configuration["Jwt:ExpiracaoDias"];
    if (!int.TryParse(valorBruto, out var dias) || dias <= 0)
    {
        throw new InvalidOperationException(
            $"Configuração inválida: Jwt:ExpiracaoDias = \"{valorBruto}\". Esperado um inteiro positivo.");
    }

    return dias;
}

// Mesmo padrão (falha explícita no startup) de LerExpiracaoDiasObrigatoria
// acima — ver docs/specs/2-convite-whatsapp/implementation.md.
static int LerDiasValidadeConviteObrigatoria(IConfiguration configuration)
{
    var valorBruto = configuration["Convites:DiasValidade"];
    if (!int.TryParse(valorBruto, out var dias) || dias <= 0)
    {
        throw new InvalidOperationException(
            $"Configuração inválida: Convites:DiasValidade = \"{valorBruto}\". Esperado um inteiro positivo.");
    }

    return dias;
}

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

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Necessário para o WebApplicationFactory<Program> usado nos testes de integração.
public partial class Program
{
}
