using Common.Context;
using Common.DTO;
using Common.DMO;
using Common.Library.Models;
using Common.Library.DTOs;
using Common.Library.Data;
using Common.Library.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using System.Linq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// DB Config
var connectionString = builder.Configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27018";
var databaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "CommonDB";

Console.WriteLine("====================================================");
Console.WriteLine($"[STARTUP] Using MongoDB Connection: {connectionString}");
Console.WriteLine($"[STARTUP] Using Database Name: {databaseName}");
Console.WriteLine("====================================================");

builder.Services.AddSingleton<CommonRepository>(sp => new CommonRepository(connectionString, databaseName, builder.Configuration));
builder.Services.AddSingleton<UserRepository>(sp => new UserRepository(connectionString, databaseName));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsAVerySecretKeyForJwtAuthenticationWhichNeedsToBeLongEnough";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddCommonJwtAuthentication(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAngularDev");

app.UseAuthentication();
app.UseAuthorization();

// Login Endpoint
app.MapPost("/api/auth/login", async (LoginRequest request, [FromServices] UserRepository userRepo) =>
{
    var user = await userRepo.GetUserAsync(request.Username, request.Password);
    if (user == null) return Results.Unauthorized();

    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        Issuer = builder.Configuration["Jwt:Issuer"] ?? "Common.ServiceHub",
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new AuthResponse { Token = tokenHandler.WriteToken(token), Role = user.Role, Username = user.Username, UserId = user.Id ?? "" });
});

// Register Endpoint
app.MapPost("/api/auth/register", async (User registerUser, [FromServices] UserRepository userRepo) =>
{
    var existing = await userRepo.GetUserByUsernameAsync(registerUser.Username);
    if (existing != null) return Results.BadRequest(new { message = "Username already exists" });

    if (string.IsNullOrWhiteSpace(registerUser.Role))
    {
        registerUser.Role = "User";
    }

    if (string.IsNullOrWhiteSpace(registerUser.PasswordHash) && !string.IsNullOrWhiteSpace(registerUser.Password))
    {
        registerUser.PasswordHash = registerUser.Password;
    }

    await userRepo.CreateUserAsync(registerUser);

    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, registerUser.Username),
            new Claim(ClaimTypes.Role, registerUser.Role)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        Issuer = builder.Configuration["Jwt:Issuer"] ?? "Common.ServiceHub",
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new AuthResponse { Token = tokenHandler.WriteToken(token), Role = registerUser.Role, Username = registerUser.Username, UserId = registerUser.Id ?? "" });
});

// User Management Endpoints (Admin Only)
app.MapGet("/api/users", async ([FromServices] UserRepository userRepo) =>
{
    var users = await userRepo.GetAllUsersAsync();
    return Results.Ok(users.Select(u => new { u.Id, u.Username, u.Email, u.Role, u.CreatedAt }));
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/users", async (User user, [FromServices] UserRepository userRepo) =>
{
    await userRepo.CreateUserAsync(user);
    return Results.Ok(user);
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/users/{id}", async (string id, [FromServices] UserRepository userRepo) =>
{
    await userRepo.DeleteUserAsync(id);
    return Results.Ok(new { message = "User deleted" });
}).RequireAuthorization("AdminOnly");

// Sync Endpoint for Cross-Service Live Replication (Dairy, Grocery, Stationary)
app.MapPost("/api/common/sync", async (ProductSyncPayload payload, [FromServices] CommonRepository repository) =>
{
    await repository.SyncProductAsync(payload);
    return Results.Ok(new { message = "Synced successfully" });
});

// Get Items (Accessible by both Admin and User, but returns different fields)
app.MapGet("/api/common/items", async ([FromServices] CommonRepository repository, ClaimsPrincipal user) =>
{
    var items = await repository.GetAllItemsAsync();
    bool isAdmin = user.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type.Equals("role", StringComparison.OrdinalIgnoreCase)) && c.Value.Equals("admin", StringComparison.OrdinalIgnoreCase));

    if (isAdmin)
    {
        var response = items.Select(i => new CommonItemAdminResponse
        {
            ItemId = i.Id.ToString(),
            OriginalId = i.OriginalId,
            SourceService = string.IsNullOrEmpty(i.SourceService) ? "Common" : i.SourceService,
            Name = i.Name ?? "Unknown Item",
            Category = i.Category,
            Price = i.Price,
            StockQuantity = i.StockQuantity
        }).ToList();
        return Results.Ok(response);
    }
    else
    {
        var response = items.Select(i => new CommonItemResponse
        {
            ItemId = i.Id.ToString(),
            OriginalId = i.OriginalId,
            SourceService = string.IsNullOrEmpty(i.SourceService) ? "Common" : i.SourceService,
            Name = i.Name ?? "Unknown Item",
            Category = i.Category,
            Price = i.Price
        }).ToList();
        return Results.Ok(response);
    }
}).AllowAnonymous();

// Add Item (Admin Only)
app.MapPost("/api/common/items", async (CommonItemRequest request, [FromServices] CommonRepository repository) =>
{
    var item = new CommonItem
    {
        Name = request.Name,
        Category = request.Category,
        Price = request.Price,
        StockQuantity = request.StockQuantity,
        CreatedAt = DateTime.UtcNow
    };
    await repository.AddItemAsync(item);
    var response = new CommonItemAdminResponse
    {
        ItemId = item.Id.ToString(),
        OriginalId = item.OriginalId ?? "",
        SourceService = item.SourceService ?? "Common",
        Name = item.Name,
        Category = item.Category,
        Price = item.Price,
        StockQuantity = item.StockQuantity
    };
    return Results.Ok(response);
}).RequireAuthorization("AdminOnly");

// Update Item (Admin Only)
app.MapPut("/api/common/items/{id}", async (string id, CommonItemRequest request, [FromServices] CommonRepository repository) =>
{
    var existing = await repository.GetItemAsync(id);
    if (existing == null) return Results.NotFound();
                                        
    existing.Name = request.Name;
    existing.Category = request.Category;
    existing.Price = request.Price;
    existing.StockQuantity = request.StockQuantity;

    await repository.UpdateItemAsync(id, existing);
    return Results.Ok(new { message = "Item updated successfully" });
}).RequireAuthorization("AdminOnly");

// Delete Item (Admin Only)
app.MapDelete("/api/common/items/{id}", async (string id, [FromServices] CommonRepository repository) =>
{
    var existing = await repository.GetItemAsync(id);
    if (existing == null) return Results.NotFound();

    await repository.DeleteItemAsync(existing.Id.ToString());
    return Results.Ok(new { message = "Item deleted successfully" });
}).RequireAuthorization("AdminOnly");

app.MapFallbackToFile("index.html");

app.Run();
