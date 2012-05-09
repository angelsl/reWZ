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
// 
// Linking this library statically or dynamically with other modules
// is making a combined work based on this library. Thus, the terms and
// conditions of the GNU General Public License cover the whole combination.
// 
// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent modules,
// and to copy and distribute the resulting executable under terms of your
// choice, provided that you also meet, for each linked independent module,
// the terms and conditions of the license of that module. An independent
// module is a module which is not derived from or based on this library.
// If you modify this library, you may extend this exception to your version
// of the library, but you are not obligated to do so. If you do not wish to
// do so, delete this exception statement from your version.

using System;
using System.Drawing;
using System.Linq;

namespace reWZ.WZProperties
{
    /// <summary>
    ///   A struct used to represent nothing.
    /// </summary>
    public struct WZNothing
    {}

    /// <summary>
    ///   Null.
    /// </summary>
    public class WZNullProperty : WZProperty<WZNothing>
    {
        internal WZNullProperty(string name, WZObject parent, WZImage container) : base(name, parent, default(WZNothing), container, false)
        {}
    }

    /// <summary>
    ///   An unsigned 16-bit integer property.
    /// </summary>
    public class WZUInt16Property : WZProperty<ushort>
    {
        internal WZUInt16Property(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadUInt16(), container, false)
        {}
    }

    /// <summary>
    ///   A compressed signed 32-bit integer property.
    /// </summary>
    public class WZInt32Property : WZProperty<int>
    {
        internal WZInt32Property(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadWZInt(), container, false)
        {}
    }

    /// <summary>
    ///   A floating point number with single precision property.
    /// </summary>
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

    /// <summary>
    ///   A floating point number with double precision property.
    /// </summary>
    public class WZDoubleProperty : WZProperty<Double>
    {
        internal WZDoubleProperty(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadDouble(), container, false)
        {}
    }

    /// <summary>
    ///   A string property.
    /// </summary>
    public class WZStringProperty : WZProperty<String>
    {
        internal WZStringProperty(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader, container, false)
        {}

        internal override string Parse(WZBinaryReader r, bool initial)
        {
            if (initial) {
                r.SkipWZStringBlock();
                return null;
            }

            return r.ReadWZStringBlock(Image._encrypted);
        }
    }

    /// <summary>
    ///   A point property, containing an X and Y value pair.
    /// </summary>
    public class WZPointProperty : WZProperty<Point>
    {
        internal WZPointProperty(string name, WZObject parent, WZBinaryReader wzbr, WZImage container)
            : base(name, parent, new Point(wzbr.ReadWZInt(), wzbr.ReadWZInt()), container, false)
        {}
    }

    /// <summary>
    ///   A link property, used to link to other properties in the WZ file.
    /// </summary>
    public class WZUOLProperty : WZProperty<String>
    {
        internal WZUOLProperty(string name, WZObject parent, WZBinaryReader reader, WZImage container)
            : base(name, parent, reader.ReadWZStringBlock(container._encrypted), container, false)
        {}

        /// <summary>
        ///   Resolves the link once.
        /// </summary>
        /// <returns> The WZ object that this link refers to. </returns>
        public WZObject Resolve()
        {
            return Value.Split('/').Where(node => node != ".").Aggregate(Parent, (current, node) => node == ".." ? current.Parent : current[node]);
        }

        /// <summary>
        ///   Resolves the link recursively, repeatedly resolving links until an object is reached.
        /// </summary>
        /// <returns> The non-link WZ object that this link refers to. </returns>
        public WZObject ResolveFully()
        {
            WZObject ret = this;
            while (ret is WZUOLProperty)
                ret = ((WZUOLProperty)ret).Resolve();
            return ret;
        }
    }

    /// <summary>
    ///   A sound property.
    /// </summary>
    public class WZMP3Property : WZProperty<byte[]>
    {
        internal WZMP3Property(string name, WZObject parent, WZBinaryReader r, WZImage container)
            : base(name, parent, r, container, false)
        {}

        internal override byte[] Parse(WZBinaryReader r, bool initial)
        {
            r.Skip(1);
            int blockLen = r.ReadWZInt(); // sound data length
            r.ReadWZInt(); // sound duration
            r.Skip(82); // header [82 bytes]
            if (initial) r.Skip(blockLen);
            else return r.ReadBytes(blockLen); // sound data 
            return null;
        }
    }
}