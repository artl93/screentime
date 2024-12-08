
internal class HiddenForm : Form
{
    public HiddenForm(Task thingToRun)
    {
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.ShowIcon = false;
        this.Visible = false;
        this.Task = thingToRun;
    }

    public Task Task { get; }


}