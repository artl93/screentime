using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace screentime
{
    internal class ScreenTimeLocalStateClient : IScreenTimeStateClient
    {
        public void EndSessionAsync()
        {
            throw new NotImplementedException();
        }

        public Task<UserStatus?> GetInteractiveTimeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ServerMessage?> GetMessage()
        {
            throw new NotImplementedException();
        }

        public Task<UserConfiguration?> GetUserConfigurationAsync()
        {
            throw new NotImplementedException();
        }

        public void StartSessionAsync()
        {
            throw new NotImplementedException();
        }
    }
}
