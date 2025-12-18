using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SkillService.Data;
using SkillService.Messaging;
using SkillService.Middleware;
using SkillService.Repositories;
using SkillService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "Logs/skillservice-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7);
});

builder.Services.AddDbContext<SkillDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SkillDatabase")));

builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IEmployeeSkillRepository, EmployeeSkillRepository>();
builder.Services.AddScoped<IEmployeeReferenceRepository, EmployeeReferenceRepository>();
builder.Services.AddScoped<ISkillService, SkillAppService>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

builder.Services.AddHostedService<EmployeeCreatedEventConsumer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionHandling();
app.UseCorrelationId();
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
