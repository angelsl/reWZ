using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace reWZ.WZProperties
{
    public struct WZNothing {}

    public class WZNullProperty : WZProperty<WZNothing>
    {
        public WZNullProperty(string name, WZObject parent, WZImage container) : base(name, parent, default(WZNothing), container)
        {}
    }

    public class WZUInt16Property : WZProperty<ushort>
    {
        public WZUInt16Property(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadUInt16(), container)
        {} 
    }

    public class WZInt32Property : WZProperty<int>
    {
        public WZInt32Property(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadWZInt(), container)
        {}
    }

    public class WZSingleProperty : WZProperty<Single>
    {
        public WZSingleProperty(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, ReadSingle(reader), container)
        {
        }

        private static Single ReadSingle(WZBinaryReader reader)
        {
            byte t = reader.ReadByte();
            return t == 0x80 ? reader.ReadSingle() : (t == 0 ? 0f : WZFile.Die<float>("Unknown byte while reading WZSingleProperty."));
        }
    }

    public class WZDoubleProperty : WZProperty<Double>
    {
        public WZDoubleProperty(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadDouble(), container)
        {}
    }

    public class WZStringProperty : WZProperty<String>
    {
        public WZStringProperty(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadWZStringBlock(container.File._encrypted), container)
        {}
    }

    public class WZVectorProperty : WZProperty<Point>
    {
        public WZVectorProperty(string name, WZObject parent, WZBinaryReader wzbr, WZImage container)
            : base(name, parent, new Point(wzbr.ReadWZInt(), wzbr.ReadWZInt()), container)
        { }
    }

    /*public class WZUOLProperty : WZProperty<WZObject>
    {
        public WZUOLProperty(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, null, container)
        {}
    }*/
}
