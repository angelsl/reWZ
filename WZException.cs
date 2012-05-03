using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace reWZ
{
    public class WZException : Exception
    {
        internal WZException(string message = "", Exception innerException = null) : base(message, innerException)
        {}
    }
}
