using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Synclass.Api.Controllers;
using Synclass.Api.Logging;
using Synclass.Api.Middleware;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Alunos;
using Synclass.Domain.Aulas;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.CodigosEntradaTurma;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Common;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Convites;
using Synclass.Domain.Frequencias;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Pagamentos;
using Synclass.Domain.Usuarios;
using Synclass.Infrastructure.Alunos;
using Synclass.Infrastructure.Autenticacao;
using Synclass.Infrastructure.Common;
using Synclass.Infrastructure.Convites;
using Synclass.Infrastructure.Http;
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

// Credenciais do provedor de WhatsApp (Twilio por padrão, issue #193) — falha
// explícita no startup, mesmo padrão de Jwt:SigningKey acima, para o serviço
// não arrancar sem as chaves que WhatsAppHttpClient/WhatsAppNotificador leem de
// IConfiguration. Ponto único de validação (as classes de Infrastructure não
// revalidam); exigidas sempre, inclusive em ModoDev, para que a troca de DI
// condicional abaixo nunca dependa de config ausente. AccountSid/AuthToken
// (não um único "ApiKey") porque a Basic Auth do Twilio exige os dois valores
// separados por ":" (dev-review do PR #206) — AccountSid também compõe a URL
// da Messages API, eliminando o placeholder "ACCOUNT_SID" fixo que nunca
// falhava explicitamente no startup. Ver
// docs/specs/193-login-whatsapp-real/implementation.md.
var whatsAppAccountSid = builder.Configuration["WhatsApp:AccountSid"]
    ?? throw new InvalidOperationException("Configuração ausente: WhatsApp:AccountSid.");
var whatsAppAuthToken = builder.Configuration["WhatsApp:AuthToken"]
    ?? throw new InvalidOperationException("Configuração ausente: WhatsApp:AuthToken.");
var whatsAppNumeroRemetente = builder.Configuration["WhatsApp:NumeroRemetente"]
    ?? throw new InvalidOperationException("Configuração ausente: WhatsApp:NumeroRemetente.");

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<CadastroUsuarioService>();

// Identificador único e human-readable de Aluno (issue #70) — gerado em
// tres pontos (cadastro de Aluno #61, aceite de convite #63 e cadastro de
// Aluno provisório, via Matriculas), com unicidade permanente checada contra
// Usuario.IdentificadorAluno e Matricula.IdentificadorAluno. Ver
// docs/specs/70-identificador-aluno/implementation.md.
builder.Services.AddSingleton<IGeradorDeIdentificadorAluno, GeradorDeIdentificadorAluno>();
builder.Services.AddScoped<IIdentificadorAlunoUnicidadeChecker, IdentificadorAlunoUnicidadeChecker>();
builder.Services.AddScoped<IdentificadorAlunoService>();
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

// Cancelamento de aula com antecedência configurável (issue #10) — ver
// docs/specs/10-cancelamento-aula/implementation.md.
builder.Services.AddScoped<IAulaRepository, AulaRepository>();
builder.Services.AddScoped<ICancelamentoAulaRepository, CancelamentoAulaRepository>();
builder.Services.AddScoped<AulaService>();

// Registro de frequência pelo Professor (issue #14) e confirmação de
// presença pelo Aluno (issue #15) — ver
// docs/specs/14-registro-frequencia/implementation.md e
// docs/specs/15-aluno-confirma-presenca/implementation.md. Novas
// dependências de FrequenciaService (AlocacaoHorarioService,
// ICancelamentoAulaRepository) já registradas acima.
builder.Services.AddScoped<IRegistroFrequenciaRepository, RegistroFrequenciaRepository>();
builder.Services.AddScoped<FrequenciaService>();

// Login por OTP (issue #18) — ver docs/specs/18-login-otp/implementation.md.
builder.Services.AddScoped<ICodigoOtpRepository, CodigoOtpRepository>();
builder.Services.AddSingleton<IGeradorDeCodigoOtp, GeradorDeCodigoOtp>();
// Notificador do login por OTP (issue #193): em produção registra o
// WhatsAppNotificador real (via IWhatsAppHttpClient, Twilio por padrão);
// NotificadorDeLog (loga o código em claro) só em ModoDev (DI condicional,
// comportamento de dev da issue #18) — nunca em produção. A fronteira de troca
// de provedor é IWhatsAppHttpClient (fábrica aqui), sem tocar INotificador.
if (builder.Configuration.GetValue<bool>("AssinaturaDigital:ModoDev"))
{
    builder.Services.AddScoped<INotificador, NotificadorDeLog>();
}
else
{
    // BaseAddress montado a partir de WhatsApp:AccountSid (validado acima) —
    // sem fallback de URL fixa: se a config estiver ausente, a exceção já
    // aconteceu antes desta linha (dev-review do PR #206: o placeholder
    // anterior "ACCOUNT_SID" nunca falhava explicitamente no startup).
    builder.Services.AddHttpClient<IWhatsAppHttpClient, WhatsAppHttpClient>(client =>
    {
        client.BaseAddress = new Uri($"https://api.twilio.com/2010-04-01/Accounts/{whatsAppAccountSid}/Messages.json");
    });
    builder.Services.AddScoped<INotificador, WhatsAppNotificador>();
}
builder.Services.AddSingleton<IGeradorDeTokenSessao>(sp => new GeradorDeTokenSessaoJwt(
    jwtSigningKey,
    LerExpiracaoDiasObrigatoria(builder.Configuration),
    sp.GetRequiredService<IClock>()));
builder.Services.AddScoped<LoginService>();

// Login via idToken do Google (issue #65) — fluxo paralelo ao login OTP,
// não o substitui. Ver docs/specs/65-login-google/implementation.md.
builder.Services.AddScoped<IValidadorDeIdTokenGoogle, ValidadorDeIdTokenGoogle>();
builder.Services.AddScoped<LoginComGoogleService>();

// Convite de Aluno via WhatsApp (issue #2) — ver
// docs/specs/2-convite-whatsapp/implementation.md.
builder.Services.AddScoped<IConviteRepository, ConviteRepository>();
builder.Services.AddSingleton<IGeradorDeTokenConvite, GeradorDeTokenConvite>();
// Código curto de convite (issue #62) — ver
// docs/specs/62-codigo-convite-curto/implementation.md.
builder.Services.AddSingleton<IGeradorDeCodigoConvite, GeradorDeCodigoConvite>();
builder.Services.AddScoped(sp => new ConviteService(
    sp.GetRequiredService<IConviteRepository>(),
    sp.GetRequiredService<IMatriculaRepository>(),
    sp.GetRequiredService<IUsuarioRepository>(),
    sp.GetRequiredService<IGeradorDeTokenConvite>(),
    sp.GetRequiredService<IGeradorDeCodigoConvite>(),
    sp.GetRequiredService<IClock>(),
    LerDiasValidadeConviteObrigatoria(builder.Configuration),
    sp.GetRequiredService<IdentificadorAlunoService>()));

// Código de entrada de turma: Professor gera, qualquer Aluno autenticado
// aceita — sem vínculo a contato e sem uso único, diferente de Convite.
// Reaproveita o mesmo IGeradorDeCodigoConvite já registrado acima (mesmo
// formato de código, 5 dígitos numéricos).
builder.Services.AddScoped<ICodigoEntradaTurmaRepository, CodigoEntradaTurmaRepository>();
builder.Services.AddScoped(sp => new CodigoEntradaTurmaService(
    sp.GetRequiredService<ICodigoEntradaTurmaRepository>(),
    sp.GetRequiredService<IMatriculaRepository>(),
    sp.GetRequiredService<IUsuarioRepository>(),
    sp.GetRequiredService<IGeradorDeCodigoConvite>(),
    sp.GetRequiredService<IClock>()));

// Conectar conta Mercado Pago do Professor (issue #203) — ver
// docs/specs/203-professor-conecta-mercado-pago/implementation.md.
// O HttpClient é registrado via AddHttpClient (primeiro no repo, ver
// implementation.md#integração-http); a redirect_uri é uma constante fixa
// lida de MercadoPago:RedirectUri (mesmo padrão de falha explícita no
// startup dos demais segredos do Mercado Pago).
var redirectUriMercadoPago = LerRedirectUriMercadoPagoObrigatoria(builder.Configuration);
builder.Services.AddHttpClient<IClienteOAuthMercadoPago, ClienteOAuthMercadoPago>(client =>
{
    client.BaseAddress = new Uri("https://api.mercadopago.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IConexaoMercadoPagoRepository, ConexaoMercadoPagoRepository>();
builder.Services.AddScoped(sp => new ConexaoMercadoPagoService(
    sp.GetRequiredService<IConexaoMercadoPagoRepository>(),
    sp.GetRequiredService<IUsuarioRepository>(),
    sp.GetRequiredService<IClienteOAuthMercadoPago>(),
    sp.GetRequiredService<IClock>(),
    redirectUriMercadoPago));

// Rate limiting dos endpoints anônimos de aceite de convite (issue #89) —
// ver docs/specs/89-rate-limit-convites/implementation.md. Política
// "ConvitesAnonimos" fixa janela fixa (fixed window) particionada por IP de
// origem, configurada em RateLimiting:ConvitesAnonimos.
var (permissoesPorJanela, janelaEmSegundos) = LerConfiguracaoRateLimitConvitesObrigatoria(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ConvitesAnonimos", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = permissoesPorJanela,
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(janelaEmSegundos),
            }));

    // Corpo/header/log da rejeição 429 — ver
    // docs/specs/89-rate-limit-convites/implementation.md. Usa o mesmo
    // contrato ConviteErrorResponse dos endpoints de aceite (issue #2/issue
    // #63), o header Retry-After (segundos restantes da janela, vindos do
    // metadata da partição) e log estruturado sem IP/payload (ver
    // security-rules.md).
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString("0");
        }

        var jsonOptions = httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;
        await httpContext.Response.WriteAsJsonAsync(
            new ConviteErrorResponse("Muitas tentativas. Tente novamente em instantes."),
            jsonOptions.JsonSerializerOptions,
            cancellationToken);

        var trackId = httpContext.Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("ConvitesRateLimiting");
        logger.LogWarning(
            "ConviteAceiteBloqueadoPorLimite {TrackId} {Rota}",
            trackId, httpContext.Request.Path.ToString());
    };
});

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

// Mesmo padrão (falha explícita no startup) das funções acima — sem isso, a
// redirect_uri ausente/vazia quebraria só no primeiro usuário a conectar a
// conta, não no boot da Api. Ver implementation.md#rota-fixa-e-url-de-redirecionamento.
static string LerRedirectUriMercadoPagoObrigatoria(IConfiguration configuration)
{
    var redirectUri = configuration["MercadoPago:RedirectUri"];
    if (string.IsNullOrWhiteSpace(redirectUri))
    {
        throw new InvalidOperationException(
            "Configuração ausente: MercadoPago:RedirectUri. Esperada a URL do endpoint de callback do Mercado Pago.");
    }

    return redirectUri;
}

// Mesmo padrão (falha explícita no startup) das funções acima — sem isso,
// GetValue<int> devolveria 0 silenciosamente quando o limite estivesse
// ausente/mal formatado, e a política de rate limit aceitaria 0 requisições.
// Ver docs/specs/89-rate-limit-convites/implementation.md.
static (int PermissoesPorJanela, int JanelaEmSegundos) LerConfiguracaoRateLimitConvitesObrigatoria(IConfiguration configuration)
{
    var permissoesBruto = configuration["RateLimiting:ConvitesAnonimos:PermissoesPorJanela"];
    var janelaBruto = configuration["RateLimiting:ConvitesAnonimos:JanelaEmSegundos"];

    if (!int.TryParse(permissoesBruto, out var permissoes) || permissoes <= 0)
    {
        throw new InvalidOperationException(
            $"Configuração inválida: RateLimiting:ConvitesAnonimos:PermissoesPorJanela = \"{permissoesBruto}\". Esperado um inteiro positivo.");
    }

    if (!int.TryParse(janelaBruto, out var janela) || janela <= 0)
    {
        throw new InvalidOperationException(
            $"Configuração inválida: RateLimiting:ConvitesAnonimos:JanelaEmSegundos = \"{janelaBruto}\". Esperado um inteiro positivo.");
    }

    return (permissoes, janela);
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
// Rate limiter antes de MapControllers — middleware mapeado por policy
// aplica RejectionStatusCode (429) quando a contagem da janela é excedida
// (issue #89).
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Necessário para o WebApplicationFactory<Program> usado nos testes de integração.
public partial class Program
{
}
