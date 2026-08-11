using Microsoft.EntityFrameworkCore;
using Serilog;
using Synclass.Api.Logging;
using Synclass.Api.Middleware;
using Synclass.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.UseStructuredJsonLogging();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SynclassDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseTrackId();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Necessário para o WebApplicationFactory<Program> usado nos testes de integração.
public partial class Program
{
}
