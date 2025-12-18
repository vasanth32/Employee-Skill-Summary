using Microsoft.EntityFrameworkCore;
using SearchService.Data;
using SearchService.Messaging;
using SearchService.Middleware;
using SearchService.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "Logs/searchservice-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7);
});

// Add Entity Framework
builder.Services.AddDbContext<SearchDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SearchDatabase")));

// Add Repositories
builder.Services.AddScoped<IEmployeeSummaryRepository, EmployeeSummaryRepository>();
builder.Services.AddScoped<IEmployeeSummarySkillRepository, EmployeeSummarySkillRepository>();

// Add Event Consumers (Background Services)
builder.Services.AddHostedService<EmployeeCreatedEventConsumer>();
builder.Services.AddHostedService<SkillRatedEventConsumer>();

// Add Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
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
