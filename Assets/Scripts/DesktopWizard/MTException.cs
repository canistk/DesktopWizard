using System;

namespace DesktopWizard
{
    /// <summary>
    /// Multi-thread exception log message.
    /// Captures exception information from background threads to be logged on Unity's main thread.
    /// </summary>
    public class MTException : MTBase
    {
        public Exception exception;
        public string msg;

        public MTException(string msg, Exception ex)
        {
            this.msg = msg;
            this.exception = ex;
        }
    }
}
