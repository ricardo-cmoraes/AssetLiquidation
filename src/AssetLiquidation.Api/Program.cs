using Asp.Versioning;
using AssetLiquidation.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Adicionar suporte a Controllers REST
builder.Services.AddControllers();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

//conection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Configurar o DbContext com Banco de Dados Em Memória
builder.Services.AddDbContext<LiquidateDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Documentação Swagger/OpenAPI (nativo do .NET)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();