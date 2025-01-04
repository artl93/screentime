using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Microsoft.OpenApi.Models;
using ScreenTime.Common;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using Scalar.AspNetCore;
using ScreenTimeService;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UserContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapOpenApi();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapScalarApiReference();

}
// else
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseHttpsRedirection();


var scopeRequiredByApi = app.Configuration["AzureAd:Scopes"] ?? "";


var requests = new Queue<(ClaimsPrincipal, int)>
{
};
app.MapPut("/request/{minutes}", (int minutes, HttpContext httpContext) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
    requests.Enqueue((httpContext.User, minutes));
    var result = httpContext.User.GetNameIdentifierId();

    // get total pending requests for this user in terms of minutes
    var totalMinutes = requests.Where(r => r.Item1 == httpContext.User).Sum(r => r.Item2);

    return Results.Ok(totalMinutes);
})
    .WithOpenApi()
    .RequireAuthorization();

app.MapPut("/grant/{userId}/{minutes}", (string userId, int minutes, HttpContext httpContext) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
    var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userId) }));
    var request = requests.FirstOrDefault(r => r.Item1 == user);
    if (request != default)
    {
        requests = new Queue<(ClaimsPrincipal, int)>(requests.Where(r => r != request));
        // get the total of all requests 
        var totalMinutes = requests.Sum(r => r.Item2);
        return Results.Ok(totalMinutes);
    }
    return Results.NotFound();
})
    .WithOpenApi()
    .RequireAuthorization();


app.MapPut("/reject/{userId}", (string userId, HttpContext httpContext) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
    var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userId) }));
    var request = requests.FirstOrDefault(r => r.Item1 == user);
    if (request != default)
    {
        requests = new Queue<(ClaimsPrincipal, int)>(requests.Where(r => r != request));
        // get the total of all requests 
        var totalMinutes = requests.Sum(r => r.Item2);
        return Results.Ok(totalMinutes);
    }
    return Results.NotFound();
})
    .WithOpenApi()
    .RequireAuthorization();


app.MapGet("/pending/{userId}", (string userId, HttpContext httpContext) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
    var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userId) }));
    var totalMinutes = requests.Where(r => r.Item1 == user).Sum(r => r.Item2);
    return Results.Ok(totalMinutes);
})
    .WithOpenApi()
    .RequireAuthorization();

app.MapGet("/pending", (HttpContext httpContext) => requests)
    .WithOpenApi()
    .RequireAuthorization();

app.MapGet("/configuration", () => Results.Ok(scopeRequiredByApi))
    .WithOpenApi()
    .RequireAuthorization();


app.MapPut("/heartbeat", async (HttpContext httpContext, Heartbeat heartbeat, UserContext db) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

    var user = GetOrEnsureUser(db, httpContext.User);
    if (user == null)
    {
        return Results.NotFound();
    }

    var record = new HeartbeatRecord
    {
        UserId = user.Id,
        DateTime = heartbeat.Timestamp,
        Duration = heartbeat.Duration,
        UserState = heartbeat.UserState
    };

    db.Heartbeats.Add(record);
    await db.SaveChangesAsync();

    return Results.Ok($"Event added for {user}");
})
    .WithOpenApi()
    .RequireAuthorization();

//app.MapPost("/logout", async (SignInManager<IdentityUser> signInManager,
//    [Microsoft.AspNetCore.Mvc.FromBody] object empty) =>
//{
//    if (empty != null)
//    {
//        await signInManager.SignOutAsync();
//        return Results.Ok();
//    }
//    return Results.Unauthorized();
//})
//.WithOpenApi()
//.RequireAuthorization();

app.Run();

UserRecord? GetOrEnsureUser(UserContext db, ClaimsPrincipal principal)
{
    var displayName = principal.GetDisplayName();
    var nameIdentifier = principal.GetNameIdentifierId();
    if (nameIdentifier == null || displayName == null)
    {
        throw new InvalidOperationException("Invalid user");
    }
    // var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, nameIdentifier) }));

    var user = db.Users.FirstOrDefault(u => u.NameIdentifier == nameIdentifier);
    if (user == null)
    {
        user = new UserRecord
        {
            NameIdentifier = nameIdentifier,
            UserName = displayName,
            Email = displayName,
            CreatedAt = DateTime.Now
        };
        db.Users.Add(user);
        db.SaveChanges();
    }
    return user;
}