using Microsoft.EntityFrameworkCore;
using SearchService.Data;
using SearchService.Messaging;
using SearchService.Repositories;

var builder = WebApplication.CreateBuilder(args);

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

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
