using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SkillService.Data;
using SkillService.Messaging;
using SkillService.Repositories;
using SkillService.Services;

var builder = WebApplication.CreateBuilder(args);

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

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
