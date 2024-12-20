using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace ScreenTime
{
    /// <summary>
    /// A client for the ScreenTimeService
    /// 
    /// </summary>
    internal class ScreenTimeServiceClient(HttpClient client) : IScreenTimeStateClient, IDisposable
    {
        enum State
        {
            active,
            inactive
        }

        State currentState = State.inactive;

        readonly JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        private bool disposedValue;
        private readonly HttpClient _client = client;

        public event EventHandler<MessageEventArgs>? OnDayRollover;
        public event EventHandler<UserStatusEventArgs>? OnTimeUpdate;
        public event EventHandler<UserStatusEventArgs>? OnUserStatusChanged;
        public event EventHandler<MessageEventArgs>? OnMessageUpdate;

        public async void StartSessionAsync()
        {
            if (currentState == State.active)
            {
                return;
            }

            currentState = State.active;
            _ = await _client.PutAsync($"events/start/{Environment.UserName}", null);
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
            _ = await _client.PutAsync($"events/end/{Environment.UserName}", null);
        }

        public async Task<UserStatus?> GetInteractiveTimeAsync()
        {
            var response = await _client.GetAsync($"status/{Environment.UserName}");
            var message = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserStatus>(message, options);
        }

        public async Task<UserMessage?> GetMessage()
        {
            var messageResponse = await _client.GetAsync($"message/{Environment.UserName}");
            var message = await messageResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserMessage>(message, options);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    _client.Dispose();
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ScreenTimeServiceClient()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal IScreenTimeStateClient SetBaseAddress(string v)
        {
            _client.BaseAddress = new Uri(v);
            return this;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
