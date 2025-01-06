using ScreenTime.Common;

namespace ScreenTimeClient
{
    public class ComputerStateEventArgs(UserState state) : EventArgs
    {
        public UserState State { get; } = state;
    }
}