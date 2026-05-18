using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Net.Web.Api.Sdk.Initialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebApiSdk("Net.Web.Api.Sdk");

var app = builder.Build();

app.UseWebApiSdk();

app.MapControllers();

app.Run();
