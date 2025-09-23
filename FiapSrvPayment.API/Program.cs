using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using AspNetCore.DataProtection.Aws.S3;

using FiapSrvAuthManager.Infrastructure.Configuration; 
using FiapSrvPayment.Application.Interface;
using FiapSrvPayment.Application.Services;
using FiapSrvPayment.Infrastructure.Configuration;
using FiapSrvPayment.Infrastructure.Mappings;
using FiapSrvPayment.Infrastructure.Middleware;
using FiapSrvPayment.Infrastructure.Repository;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Serilog;
using System.Diagnostics.CodeAnalysis;

[assembly: ExcludeFromCodeCoverage]

var builder = WebApplication.CreateBuilder(args);

Log.Logger = SerilogConfiguration.ConfigureSerilog();
builder.Host.UseSerilog();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSimpleSystemsManagement>();
builder.Services.AddAWSService<Amazon.S3.IAmazonS3>(); 

string mongoConnectionString;
string jwtSigningKey;

if (!builder.Environment.IsDevelopment())
{
    Log.Information("Ambiente de Produ��o. Buscando segredos do AWS Parameter Store.");
    var ssmClient = new AmazonSimpleSystemsManagementClient();

    var mongoParameterName = builder.Configuration["ParameterStore:MongoConnectionString"];
    var mongoResponse = await ssmClient.GetParameterAsync(new GetParameterRequest
    {
        Name = mongoParameterName,
        WithDecryption = true
    });
    mongoConnectionString = mongoResponse.Parameter.Value;

    var jwtParameterName = builder.Configuration["ParameterStore:JwtSigningKey"];
    var jwtResponse = await ssmClient.GetParameterAsync(new GetParameterRequest
    {
        Name = jwtParameterName,
        WithDecryption = true
    });
    jwtSigningKey = jwtResponse.Parameter.Value;

    var s3Bucket = builder.Configuration["DataProtection:S3BucketName"];
    var s3KeyPrefix = builder.Configuration["DataProtection:S3KeyPrefix"];
    var s3DataProtectionConfig = new S3XmlRepositoryConfig(s3Bucket)
    {
        KeyPrefix = s3KeyPrefix
    };

    builder.Services.AddDataProtection()
        .SetApplicationName("FiapSrvPayment")
        .PersistKeysToAwsS3(s3DataProtectionConfig); 
}
else
{
    Log.Information("Ambiente de Desenvolvimento. Usando appsettings.json.");
    mongoConnectionString = builder.Configuration.GetConnectionString("MongoDbConnection")!; // Ajuste para ler de ConnectionStrings
    jwtSigningKey = builder.Configuration["Jwt:DevKey"]!;
}


// 3. Configura��o do MongoDB e Reposit�rios
var databaseName = builder.Configuration["MongoDbSettings:DatabaseName"];
builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

MongoMappings.ConfigureMappings();

builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// 4. Configura��o de Autentica��o e Autoriza��o
// A classe JwtBearerConfiguration precisa receber a chave que buscamos
builder.Services.ConfigureJwtBearer(builder.Configuration, jwtSigningKey);
builder.Services.AddAuthorization();


// -- Resto da configura��o (Controllers, Swagger, etc.) --
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "FIAP Cloud Games - Payment API", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT no formato: Bearer {seu token}"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
       {
           new OpenApiSecurityScheme
           {
               Reference = new OpenApiReference
               {
                   Type = ReferenceType.SecurityScheme,
                   Id = "Bearer"
               }
           },
           Array.Empty<string>()
       }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandler>();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();