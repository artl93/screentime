using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Microsoft.Extensions.Logging;
using Humanizer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<UserContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// options.UseInMemoryDatabase("UserStates"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var scopeRequiredByApi = app.Configuration["AzureAd:Scopes"] ?? "";


app.MapPut("/events/start/{user}", async (string user, UserContext db) =>
{
    try
    {
        var userEvent = new UserEvent
        {
            Id = Guid.NewGuid(),
            Name = user,
            DateTime = DateTimeOffset.Now,
            Event = EventKind.Start
        };
        db.UserEvents.Add(userEvent);
        await db.SaveChangesAsync();
        return Results.Ok($"Started event for {user}");

    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error starting event for user {user}", user);
        return Results.Problem("An error occurred while starting the event.");
    }
});

app.MapPut("/events/end/{user}", async (string user, UserContext db) =>
{
    try
    {
        var userEvent = new UserEvent
        {
            Id = Guid.NewGuid(),
            Name = user,
            DateTime = DateTimeOffset.Now,
            Event = EventKind.End
        };
        db.UserEvents.Add(userEvent);
        await db.SaveChangesAsync();
        return Results.Ok($"Ended event for {user}");

    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error ending event for user {user}", user);
        return Results.Problem("An error occurred while ending the event.");
    }
});

app.MapGet("/status/{name}", async (string name, UserContext db) =>
{
    try
    {
        var userConfiguration = await db.UserConfigurations
            .Where(u => u.Name == name)
            .FirstOrDefaultAsync();
        var interactiveTime = await GetTimeInteractiveTodayAsync(name, db, DateTimeOffset.Now);
        var dailyTimeLimit = TimeSpan.FromMinutes(userConfiguration.DailyLimitMinutes);
        var warningTime = TimeSpan.FromMinutes(userConfiguration.WarningTimeMinutes);
        var graceTime = TimeSpan.FromMinutes(userConfiguration.GraceMinutes);

        var userStatus = GetUserStatus(interactiveTime, dailyTimeLimit, warningTime, graceTime);

        return Results.Ok(userStatus);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error getting time for user {name}", name);
        return Results.Problem("An error occurred while getting the time.");
    }
})
    .WithDescription("Gets the time the user has been logged in today.")
    .WithOpenApi()
    .WithName("GetStatus");

app.MapPut("/events/reset/{name}", async (string name, UserContext db) =>
{
    try
    {
        var userEvents = await db.UserEvents
             .Where(u => u.Name == name)
             .ToListAsync();
        db.UserEvents.RemoveRange(userEvents);
        await db.SaveChangesAsync();
        return Results.Ok($"Reset events for {name}");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error resetting events for user {name}", name);
        return Results.Problem("An error occurred while resetting the events.");
    }
})
    .WithDescription("Resets the events for the user.")
    .WithOpenApi()
    .WithName("ResetEvents");

app.MapPut("/configuration/{name}", async (string name, int minutesLimit, int minutesWarning, int secondsBetweenWarnings, int graceMinutes, UserContext db) =>
{
    try
    {
        var dailyTimeLimit = TimeSpan.FromMinutes(minutesLimit);
        var dailyTimeWarning = TimeSpan.FromMinutes(minutesWarning);
        var warningInterval = TimeSpan.FromSeconds(secondsBetweenWarnings);

        var userConfiguration = await db.UserConfigurations
            .Where(u => u.Name == name)
            .FirstOrDefaultAsync();
        if (userConfiguration == null)
        {
            userConfiguration = new UserConfiguration
            {
                Name = name,
                DailyLimitMinutes = minutesLimit,
                WarningTimeMinutes = minutesWarning,
                WarningIntervalSeconds = secondsBetweenWarnings,
                GraceMinutes = graceMinutes

            };
            db.UserConfigurations.Add(userConfiguration);
        }
        else
        {
            userConfiguration.DailyLimitMinutes = minutesLimit;
            userConfiguration.WarningTimeMinutes = minutesWarning;
            userConfiguration.WarningIntervalSeconds = secondsBetweenWarnings;
            userConfiguration.GraceMinutes = graceMinutes;
        }
        await db.SaveChangesAsync();


        return Results.Ok($"Configured daily time limit for {name}");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error configuring daily time limit for user {name}", name);
        return Results.Problem("An error occurred while configuring the daily time limit.");
    }
})
    .WithDescription("Configures the daily time limit for the user.")
    .WithOpenApi()
    .WithName("ConfigureDailyTimeLimit");

app.MapGet("/configuration/{name}", async (string name, UserContext db) =>
{
    try
    {
        var userConfiguration = await db.UserConfigurations
            .Where(u => u.Name == name)
            .FirstOrDefaultAsync();
        if (userConfiguration == null)
        {
            // if no configuration is found, return default configuration
            userConfiguration = new UserConfiguration
            {
                Name = name,
                DailyLimitMinutes = 60,
                WarningTimeMinutes = 10,
                WarningIntervalSeconds = 60,
                GraceMinutes = 5
            };
        }
        return Results.Ok(userConfiguration);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error getting configuration for user {name}", name);
        return Results.Problem("An error occurred while getting the configuration.");
    }
})
    .WithDescription("Gets the configuration for the user.")
    .WithOpenApi()
    .WithName("GetConfiguration");

UserStatus GetUserStatus(TimeSpan interactiveTime, TimeSpan dailyTimeLimit, TimeSpan warningTime, TimeSpan gracePeriod)
{

    // get user status based on time logged in
    // if the user has gone over the limit + the grade period, log them off
    if (interactiveTime >= dailyTimeLimit + gracePeriod)
    {
        return new UserStatus(interactiveTime, "🛡️", "logout", Status.Lock, dailyTimeLimit);
    }
    else if (interactiveTime >= dailyTimeLimit)
    {
        return new UserStatus(interactiveTime, "🛑", "logout", Status.Error, dailyTimeLimit);
    }
    else if (dailyTimeLimit - interactiveTime <= warningTime && dailyTimeLimit - interactiveTime > TimeSpan.Zero)
    {
        return new UserStatus(interactiveTime, "⚠️", "none", Status.Warn, dailyTimeLimit);
    }
    else
    {
        return new UserStatus(interactiveTime, "⏳", "none", Status.Okay, dailyTimeLimit);
    }
}

app.MapGet("/message/{name}", async (string name, UserContext db) =>
{
    try
    {
        var userConfiguration = await db.UserConfigurations
            .Where(u => u.Name == name)
            .FirstOrDefaultAsync();

        var interactiveTime = await GetTimeInteractiveTodayAsync(name, db, DateTimeOffset.Now);

        var dailyTimeLimit = TimeSpan.FromMinutes(userConfiguration.DailyLimitMinutes);
        var warningTime = TimeSpan.FromMinutes(userConfiguration.WarningTimeMinutes);
        var graceTime = TimeSpan.FromMinutes(userConfiguration.GraceMinutes);

        var interactiveTimeString = TimeSpanHumanizeExtensions.Humanize(interactiveTime);
        var allowedTimeString = TimeSpanHumanizeExtensions.Humanize(dailyTimeLimit);

        // when time is up, log them off
        if (interactiveTime >= dailyTimeLimit)
        {
            if (interactiveTime > dailyTimeLimit + graceTime)
            {
                // if they have gone over the limit, log them off
                return Results.Ok(new UserMessage("Hey, you're done.", $"You have been logged for {interactiveTimeString} today. You're allowed {allowedTimeString} today. You have gone over by {TimeSpanHumanizeExtensions.Humanize(interactiveTime - dailyTimeLimit)}", "🛡️", "lock"));
            }
            return Results.Ok(new UserMessage("Time to log out.", $"You have been logged for {interactiveTimeString} today. You're allowed {allowedTimeString} today.", "🛑", "logout"));
        }

        // when they have 10 minutes left, warn them every one minute
        if (dailyTimeLimit - interactiveTime <= warningTime && dailyTimeLimit - interactiveTime > TimeSpan.Zero)
        {
            if ((dailyTimeLimit - interactiveTime).Minutes % 1 == 0)
            {
                var remainingTimeString = TimeSpanHumanizeExtensions.Humanize(dailyTimeLimit - interactiveTime);
                return Results.Ok(new UserMessage("Time Warning", $"You have {remainingTimeString} left out of {allowedTimeString}", "⏳", "warn"));
            }
        }

        return Results.Ok(new UserMessage("Time Logged", $"You have been logged for {interactiveTimeString} today out of {allowedTimeString}", "🕒", "none"));

    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error getting message for user {name}", name);
        return Results.Problem("An error occurred while getting the message.");
    }
})
    .WithDescription("Gets messages and instructions to the client to do stuff. Terrible stuff.")
    .WithOpenApi()
    .WithName("GetMessage");

async Task<TimeSpan> GetTimeInteractiveTodayAsync(string name, UserContext db, DateTimeOffset now)
{
    // compute the time the user has been logged in from the events
    // only look at events from today
    var today = DateTimeOffset.Now.Date;
    var userEvents = await db.UserEvents
        .Where(u => u.Name == name)
        .Where(u => u.DateTime.Date == today)
        .OrderBy(u => u.DateTime)
        .ToListAsync();


    var lastEvent = EventKind.Invalid;
    var lastEventTime = DateTimeOffset.MinValue;
    var totalLoggedInTime = TimeSpan.Zero;

    foreach (var userEvent in userEvents)
    {
        if (userEvent.Event == EventKind.Start)
        {
            if (lastEvent == EventKind.Start)
            {
                // if the last event was also a start event, then we have a problem
                // we should have had an end event before this start event
                // so we'll just ignore this start event
                continue;
            }
            else if (lastEvent == EventKind.End)
            {
                // if the last event was an end event, then we have a valid start event
                // we'll ignore the time between the last end event and this start event
            }
            else if (lastEvent == EventKind.Invalid)
            {
                // if the last event was invalid, then this is the first event
                // we'll just ignore the time between the start of the day and this start event
            }
            lastEvent = EventKind.Start;
            lastEventTime = userEvent.DateTime;
        }
        else if (userEvent.Event == EventKind.End)
        {
            if (lastEvent == EventKind.Start)
            {
                // if the last event was a start event, then we have a valid end event
                // we'll add the time between the last start event and this end event
                totalLoggedInTime += userEvent.DateTime - lastEventTime;
            }
            else if (lastEvent == EventKind.End)
            {
                // if the last event was an end event, then we have a problem
                // we should have had a start event before this end event
                // so we'll just ignore this end event
                continue;
            }
            else if (lastEvent == EventKind.Invalid)
            {
                // if the last event was invalid, then this is the first event
                // we'll just ignore the time between the start of the day and this end event
            }
            lastEvent = EventKind.End;
            lastEventTime = userEvent.DateTime;
        }
        else
        {
            // if the event is invalid, then we have a problem
            // we'll just ignore this event
            continue;
        }
    }
    if (lastEvent == EventKind.Start)
    {
        // if the last event was a start event, then add the time between the last start event and now
        totalLoggedInTime += now - lastEventTime;
    }

    return totalLoggedInTime;
}

app.Run();

