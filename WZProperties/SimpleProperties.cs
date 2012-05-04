// This file is part of reWZ.
// 
// reWZ is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// reWZ is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with reWZ. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Linq;

namespace reWZ.WZProperties
{
    public struct WZNothing
    {}

    public class WZNullProperty : WZProperty<WZNothing>
    {
        internal WZNullProperty(string name, WZObject parent, WZImage container) : base(name, parent, default(WZNothing), container, false)
        {}
    }

    public class WZUInt16Property : WZProperty<ushort>
    {
        internal WZUInt16Property(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadUInt16(), container, false)
        {}
    }

    public class WZInt32Property : WZProperty<int>
    {
        internal WZInt32Property(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadWZInt(), container, false)
        {}
    }

    public class WZSingleProperty : WZProperty<Single>
    {
        internal WZSingleProperty(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, ReadSingle(reader), container, false)
        {}

        private static Single ReadSingle(WZBinaryReader reader)
        {
            byte t = reader.ReadByte();
            return t == 0x80 ? reader.ReadSingle() : (t == 0 ? 0f : WZFile.Die<float>("Unknown byte while reading WZSingleProperty."));
        }
    }

    public class WZDoubleProperty : WZProperty<Double>
    {
        internal WZDoubleProperty(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadDouble(), container, false)
        {}
    }

    public class WZStringProperty : WZProperty<String>
    {
        internal WZStringProperty(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadWZStringBlock(container.File._encrypted), container, false)
        {}
    }

    public class WZVectorProperty : WZProperty<Point>
    {
        internal WZVectorProperty(string name, WZObject parent, WZBinaryReader wzbr, WZImage container)
            : base(name, parent, new Point(wzbr.ReadWZInt(), wzbr.ReadWZInt()), container, false)
        {}
    }

    public class WZUOLProperty : WZProperty<String>
    {
        internal WZUOLProperty(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadWZStringBlock(container._encrypted), container, false)
        {}

        public WZObject Resolve()
        {
            return Value.Split('/').Where(node => node != ".").Aggregate(Parent, (current, node) => node == ".." ? current.Parent : current[node]);
        }

        public WZObject ResolveFully()
        {
            WZObject ret = this;
            while (ret is WZUOLProperty)
                ret = ((WZUOLProperty)ret).Resolve();
            return ret;
        }
    }
}