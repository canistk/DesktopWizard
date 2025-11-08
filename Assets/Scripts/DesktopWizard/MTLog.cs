namespace DesktopWizard
{
    /// <summary>
    /// Multi-thread log message.
    /// Captures log information from background threads to be logged on Unity's main thread.
    /// </summary>
    public class MTLog : MTBase
    {
        public string msg;

        public MTLog(string msg)
        {
            this.msg = msg;
        }
    }
}
