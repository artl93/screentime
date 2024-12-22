namespace ScreenTime
{
    public class ComputerStateEventArgs
    {
        public ComputerStateEventArgs(UserState state)
        {
            State = state;
        }
        public UserState State { get; }
    }
}