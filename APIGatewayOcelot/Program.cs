using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Load ocelot.json + env vars
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Ocelot
builder.Services.AddOcelot(builder.Configuration);

// JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = System.Text.Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication("Bearer")
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
        ValidateLifetime = true
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("🔍 OnMessageReceived called for path: {Path}", context.Request.Path);
                
            if (context.Request.Cookies.ContainsKey("jwt"))
            {
                context.Token = context.Request.Cookies["jwt"];
                logger.LogInformation("Token extracted from cookie in OnMessageReceived");
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("JWT Authentication FAILED: {Error}", context.Exception.Message);
            logger.LogError("Exception details: {Exception}", context.Exception);
            return Task.CompletedTask;
        },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("JWT Token VALIDATED successfully!");
                
                // Log ALL claims
                logger.LogInformation("All claims in token:");
                foreach (var claim in context.Principal.Claims)
                {
                    logger.LogInformation("  Claim: {Type} = {Value}", claim.Type, claim.Value);
                }
                
                var userId = context.Principal?.FindFirst("sub")?.Value;
                logger.LogInformation("User ID from token (sub claim): {UserId}", userId ?? "(null)");
                
                return Task.CompletedTask;
            },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("⚠️ OnChallenge triggered. Error: {Error}", context.Error);
            logger.LogWarning("Error description: {Description}", context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

// Optional: Swagger & controllers if you want a tiny API on the gateway itself.
// Not required for Ocelot, but harmless if you keep it.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS – allow Angular dev origin(s)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy
            .WithOrigins(
                "http://127.0.0.1:63028",
                "http://localhost:4200",
                "http://127.0.0.1:4200",
                "http://localhost:5139",  // for testing gateway directly    
                "http://localhost:63028"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("*");  // Expose all headers to client
    });
});

var app = builder.Build();

// Dev swagger (only if you actually use controllers on gateway)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IMPORTANT: No HTTPS redirection here in dev,
// so http://localhost:5139 works as expected.
// app.UseHttpsRedirection();

// Use CORS *before* Ocelot - THIS IS CRITICAL
app.UseCors("AllowAngularDev");

// IMPORTANT: Handle OPTIONS requests for CORS preflight BEFORE Ocelot
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }
    await next();
});

// CRITICAL: Convert JWT cookie to Authorization header BEFORE Ocelot processes the request
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    
    // Log all cookies for debugging
    logger.LogInformation("📊 Request to {Path} - Total cookies: {Count}", 
        context.Request.Path, 
        context.Request.Cookies.Count);
    
    foreach (var cookie in context.Request.Cookies)
    {
        logger.LogInformation("  🍪 Cookie: {Name}", cookie.Key);
    }
    
    // Only add Authorization header for paths that need authentication
    // Don't add it for login/register/logout
    var publicPaths = new[] { "/auth/login", "/auth/register", "/auth/logout" };
    var isPublicPath = publicPaths.Any(p => context.Request.Path.StartsWithSegments(p));
    
    if (!isPublicPath && context.Request.Cookies.TryGetValue("jwt", out var token))
    {
        // Log token details for debugging
        logger.LogInformation("📝 Raw JWT cookie value (first 100 chars): {Token}", 
            token.Length > 100 ? token.Substring(0, 100) + "..." : token);
        logger.LogInformation("📏 JWT cookie length: {Length}", token.Length);
        logger.LogInformation("🔍 Contains dots: {HasDots}, Dot count: {DotCount}", 
            token.Contains('.'), 
            token.Count(c => c == '.'));
        
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Request.Headers["Authorization"] = $"Bearer {token}";
            logger.LogInformation("✅ JWT cookie found and converted to Authorization header for path: {Path}", context.Request.Path);
        }
    }
    else if (!isPublicPath)
    {
        logger.LogWarning("⚠️ No JWT cookie found for path: {Path}", context.Request.Path);
    }
    else
    {
        logger.LogInformation("ℹ️ Skipping JWT for public path: {Path}", context.Request.Path);
    }
    
    await next();
});

// Intercept login/logout responses to manage JWT cookies at Gateway level
app.Use(async (context, next) =>
{
    var originalBodyStream = context.Response.Body;
    
    using var responseBody = new MemoryStream();
    context.Response.Body = responseBody;
    
    await next();
    
    if (context.Request.Path == "/auth/login" && context.Response.StatusCode == 200)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("🔐 Login response intercepted. Body length: {Length}", body.Length);
        logger.LogInformation("📄 Response body: {Body}", body);
        
        try
        {
            var loginResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body);
            if (loginResponse.TryGetProperty("token", out var tokenElement))
            {
                var token = tokenElement.GetString();
                
                logger.LogInformation("🎫 Token extracted from response (first 100 chars): {Token}", 
                    token.Length > 100 ? token.Substring(0, 100) + "..." : token);
                logger.LogInformation("📏 Token length: {Length}, Has dots: {HasDots}, Dot count: {DotCount}", 
                    token.Length, 
                    token.Contains('.'), 
                    token.Count(c => c == '.'));
                
                var expiresAt = loginResponse.TryGetProperty("expiresAt", out var expiresElement) 
                    ? DateTime.Parse(expiresElement.GetString()!) 
                    : DateTime.UtcNow.AddHours(1);
                
                context.Response.Cookies.Append("jwt", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Lax,
                    Expires = expiresAt,
                    Path = "/"
                });
                
                logger.LogInformation("✅ JWT cookie set successfully. Expires: {Expires}", expiresAt);
            }
            else
            {
                logger.LogWarning("⚠️ No 'token' property found in login response");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to parse login response");
        }
    }
    
    if (context.Request.Path == "/auth/logout" && context.Response.StatusCode == 200)
    {
        context.Response.Cookies.Delete("jwt");
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("🚪 JWT cookie deleted on logout");
    }
    
    context.Response.Body.Seek(0, SeekOrigin.Begin);
    await responseBody.CopyToAsync(originalBodyStream);
});

app.UseAuthentication();
app.UseAuthorization();

// Add custom middleware to extract claims and add as headers AFTER authentication but BEFORE Ocelot
app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("🔑 User authenticated, extracting claims for headers");
        
        // Extract User ID from claims
        var userIdClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        var emailClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
        
        if (userIdClaim != null)
        {
            context.Request.Headers["X-User-Id"] = userIdClaim.Value;
            logger.LogInformation("✅ Added X-User-Id header: {UserId}", userIdClaim.Value);
        }
        else
        {
            logger.LogWarning("⚠️ No User ID claim found");
        }
        
        if (emailClaim != null)
        {
            context.Request.Headers["X-User-Email"] = emailClaim.Value;
            logger.LogInformation("✅ Added X-User-Email header: {Email}", emailClaim.Value);
        }
    }
    
    await next();
});

// If you have any controllers on the gateway itself:
app.MapControllers();

// Ocelot as terminal middleware
await app.UseOcelot();

app.Run();