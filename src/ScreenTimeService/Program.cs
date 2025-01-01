using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using ScreenTime.Common;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

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
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
    app.MapSwagger().RequireAuthorization("access_as_user");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

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


var heartbeats = new List<(string, Heartbeat)>() { };

app.MapPut("/heartbeat", (HttpContext httpContext, Heartbeat heartbeat) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
    var nameIdentifier = httpContext.User.GetNameIdentifierId();
    if (nameIdentifier == null)
        return Results.Unauthorized();

   
    heartbeats.Add((nameIdentifier, heartbeat));

    return Results.Ok();
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

