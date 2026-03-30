using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AuthService.Data;
using AuthService.Models;
using System.Text;
using AuthService.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Microsoft.AspNetCore.DataProtection;


var builder = WebApplication.CreateBuilder(args);
// Ensure the app listens on all network interfaces inside the container on port 8080 (IPv4 and IPv6)

// Database (EF Core + MSSQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Persist DataProtection keys to the container file system (mount this folder in docker-compose to persist across restarts)
var dpKeysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");
builder.Services.AddDataProtection()
    .SetApplicationName("AuthService")
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT Authentication
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();




builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "http://127.0.0.1:4200",
                "http://localhost:63028",
                "http://127.0.0.1:63028",
                "http://localhost:5139",  // Gateway
                "http://apigateway:80"     // Gateway i Docker
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    for (int i = 0; i < 10; i++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch
        {
            await Task.Delay(3000);
        }
    }
}



app.UseCors("Frontend");

// SEED ROLES
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Member", "Employee", "Admin" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//// Kun brug HTTPS redirect udenfor Docker
//if (!app.Environment.IsDevelopment())
//{
//    app.UseHttpsRedirection();
//}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// simple health check to verify routing from host/container
app.MapGet("/ping", () => Results.Ok(new { status = "pong" })).WithDisplayName("Ping");

app.MapControllers();

// Log registered endpoints for debugging (after mapping)
var logger = app.Services.GetRequiredService<ILogger<Program>>();
try
{
    var endpointDataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
    foreach (var endpoint in endpointDataSource.Endpoints)
    {
        logger.LogInformation("Endpoint: {Endpoint}", endpoint.DisplayName ?? endpoint.ToString());
    }
}
catch (Exception ex)
{
    logger.LogWarning(ex, "Failed to enumerate endpoints");
}

app.Run();
