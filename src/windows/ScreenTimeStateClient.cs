using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace screentime
{

    internal class ScreenTimeStateClient : IScreenTimeStateClient
    {
        enum State
        {
            active,
            inactive
        }

        State currentState = State.inactive;

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly HttpClient _client;

        public ScreenTimeStateClient(string baseUri)
        {

            var services = new ServiceCollection();
            services.AddHttpClient("screentimeClient", client =>
            {
                client.BaseAddress = new Uri(baseUri);
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "screentime");
            })
                .AddStandardResilienceHandler();

            var serviceProvider = services.BuildServiceProvider();
            _client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("screentimeClient");
        }

        public async void StartSessionAsync()
        {
            if (currentState == State.active)
            {
                return;
            }

            currentState = State.active;
            var response = await _client.PutAsync($"events/start/{Environment.UserName}", null);
        }

        public async Task<UserConfiguration?> GetUserConfigurationAsync()
        {
            try
            {
                var response = await _client.GetAsync($"configuration/{Environment.UserName}");
                var message = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UserConfiguration>(message, options);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return null;
            }
        }

        public async void EndSessionAsync()
        {
            if (currentState == State.inactive)
            {
                return;
            }
            currentState = State.inactive;
            var response = await _client.PutAsync($"events/end/{Environment.UserName}", null);
        }

        public async Task<UserStatus?> GetInteractiveTimeAsync()
        {
            var response = await _client.GetAsync($"status/{Environment.UserName}");
            var message = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserStatus>(message, options);
        }

        public async Task<ServerMessage?> GetMessage()
        {
            var messageResponse = await _client.GetAsync($"message/{Environment.UserName}");
            var message = await messageResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ServerMessage>(message, options);
        }
    }
}
