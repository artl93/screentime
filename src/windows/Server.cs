using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace screentime
{

    internal class Server
    {
        enum State
        {
            active,
            inactive
        } // this is the current state of the user
        State currentState = State.inactive;



        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public Server()
        {
            // create the http connection
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://localhost:7186/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "screentime");
            _client = client;
        }



        private HttpClient _client; // this is the http client that will be used to make requests

        public async void StartSessionAsync()
        {
            if (currentState == State.active)
            {
                return;
            }

            currentState = State.active;
            // send username and time since last update to status to the server at https://localhost:7186/log/{Environment.UserName}/{time.TotalSeconds}
            var response = await _client.PutAsync($"event/start/{Environment.UserName}", null);

        }

        public async void EndSessionAsync()
        {
            if (currentState == State.inactive)
            {
                return;
            }
            currentState = State.inactive;
            // send username and time since last update to status to the server at https://localhost:7186/log/{Environment.UserName}/{time.TotalSeconds}
            var response = await _client.PutAsync($"event/end/{Environment.UserName}", null);
        }

        public async Task<UserStatus?> GetInteractiveTimeAsync()
        {
            // get the time the user has been active for from the server at https://localhost:7186/interactive/{Environment.UserName}
            var response = await _client.GetAsync($"status/{Environment.UserName}");
            var message = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserStatus>(message, options);
        }

        public async Task<ServerMessage?> GetMessage()
        {
            // get any messages from the server at https://localhost:7186/messages/{Environment.UserName} and show them in a notification
            var messageResponse = await _client.GetAsync($"message/{Environment.UserName}");
            var message = await messageResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ServerMessage>(message, options);
        }

    }
}
