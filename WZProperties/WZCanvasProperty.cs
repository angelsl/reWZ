using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace reWZ.WZProperties
{
    public class WZCanvasProperty : WZProperty<Bitmap>
    {
        private readonly long _offset;
        public WZCanvasProperty(string name, WZObject parent, WZBinaryReader br, WZImage container) 
            : base(name, parent, br, container)
        {
            _offset = br.BaseStream.Position;
        }

        protected override Bitmap Parse(WZBinaryReader br, bool initial)
        {
            br.Jump(_offset);
            int width = br.ReadWZInt(); // width
            int height = br.ReadWZInt(); // height
            int format1 = br.ReadWZInt(); // format 1
            int format2 = br.ReadByte(); // format 2
            br.Skip(4);
            int blockLen = br.ReadInt32();
            if (initial) br.Skip(blockLen); // block Len & png data
            else {
                br.Skip(1);
                byte[] pngData = br.ReadBytes(blockLen - 1);
            }
            return null;
        }

        public override WZObject this[string childName]
        {
            get { return GetChild(childName); }
        }
    }
}
