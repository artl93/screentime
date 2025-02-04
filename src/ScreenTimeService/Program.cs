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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UserContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

var app = builder.Build();

app.MapOpenApi();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapScalarApiReference();

}
else
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseHttpsRedirection();

// Removed minimal HTTP endpoints:
// /extensions/request, /extensions/deny, /extensions/approve, /extensions/requests, /extensions/approvals,
// /configuration, /message, /heartbeat

app.MapHub<ScreenTimeHub>("/hub");

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