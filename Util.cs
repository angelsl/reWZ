using System;

namespace reWZ
{
    internal static class Util
    {
        internal static bool IsSet(this WZReadSelection options, WZReadSelection flag)
        {
            return (options & flag) == flag;
        }

    }
}