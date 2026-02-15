using OrderFlow.Api.DependencyInjection;
using OrderFlow.Api.Middleware;
using OrderFlow.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddApiSwagger();

builder.Services.AddUseCases();
builder.Services.AddInfrastructure(builder.Configuration);

var corsPolicyName = "OrderFlowWeb";

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors(corsPolicyName);

app.MapControllers();

app.Run();
