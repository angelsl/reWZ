using System;

namespace reWZ
{
    public class WZException : Exception
    {
        internal WZException(string message = "", Exception innerException = null) : base(message, innerException)
        {}
    }
}