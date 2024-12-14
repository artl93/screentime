
internal class HiddenForm : Form, IDisposable
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Task?.Dispose();
        }
        base.Dispose(disposing);
    }
}
