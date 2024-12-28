namespace ScreenTime
{
    public class ComputerStateEventArgs(UserState state) : EventArgs
    {
        public UserState State { get; } = state;
    }
}