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
using ScreenTimeService.Models;

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


app.MapPut("/extensions/request", (HttpContext httpContext, ScreenTime.Common.ExtensionRequest request, UserContext db) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

    var user = GetOrEnsureUser(db, httpContext.User);
    if (user == null)
    {
        return Results.NotFound();
    }

    var record = new ScreenTimeService.Models.ExtensionRequest
    {
        UserId = user.Id,
        SubmissionDate = TimeProvider.System.GetUtcNow(),
        Duration = request.Duration,
        IsActive = true
    };
    db.ExtensionRequests.Add(record);
    db.SaveChanges();

    return Results.Ok($"Extension {record.Id} logged for {user.UserName}.");
})
    .WithOpenApi()
    .RequireAuthorization();

app.MapPut("/extensions/grant", (HttpContext httpContext, UserContext db) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
    var user = GetOrEnsureUser(db, httpContext.User);
    if (user == null)
    {
        return Results.NotFound();
    }


    return Results.Ok($" for {user.UserName}.");
})
    .WithOpenApi()
    .RequireAuthorization();

app.MapGet("/extensions/requests", (HttpContext httpContext, UserContext db) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

    // get all active requests 
    var requests = db.ExtensionRequests
        .Where(r => r.IsActive)
        .OrderBy(r => r.SubmissionDate);


    // get all users who have requested an extension
    var users = db.Users
        .Where(u => requests.Any(r => r.UserId == u.Id))
        .ToDictionary(u => u.Id, u => u);

    // generate all AdminExtensionRequests
    var adminRequests = requests
        .Select(r => new ScreenTime.Common.AdminExtensionRequest(
            new User(users[r.UserId].Id, users[r.UserId].Email, users[r.UserId].Email), r.SubmissionDate, r.Duration));


    return Results.Ok(adminRequests);

})
    .WithOpenApi()
    .RequireAuthorization();

app.MapGet("/extensions/approvals", (HttpContext httpContext) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
    return Results.InternalServerError();
})
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