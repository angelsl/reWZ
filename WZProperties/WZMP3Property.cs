using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace reWZ.WZProperties
{
    public class WZMP3Property : WZProperty<byte[]>
    {
        public WZMP3Property(string name, WZObject parent, WZBinaryReader r, WZImage container) : base(name, parent, r, container)
        {}

        protected override byte[] Parse(WZBinaryReader r, bool initial)
        {
            r.Skip(1);
            int blockLen = r.ReadWZInt(); // sound data length
            r.ReadWZInt(); // sound duration
            r.Skip(82); // header [82 bytes]
            if(initial) r.Skip(blockLen);
            else return r.ReadBytes(blockLen); // sound data 
            return null;
        }
    }
}
