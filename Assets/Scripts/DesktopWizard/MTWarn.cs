namespace DesktopWizard
{
    /// <summary>
    /// Multi-thread warning log message.
    /// Captures warning information from background threads to be logged on Unity's main thread.
    /// </summary>
    public class MTWarn : MTBase
    {
        public string msg;

        public MTWarn(string msg)
        {
            this.msg = msg;
        }
    }
}
