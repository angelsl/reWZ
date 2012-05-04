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

using System.Collections.Generic;
using System.Drawing;

namespace reWZ.WZProperties
{
    public class WZCanvasProperty : WZProperty<Bitmap>
    {
        internal WZCanvasProperty(string name, WZObject parent, WZBinaryReader br, WZImage container)
            : base(name, parent, br, container)
        {}

        /// <summary>
        ///   Returns the child with the name <paramref name="childName" /> .
        /// </summary>
        /// <param name="childName"> The name of the child to return. </param>
        /// <returns> The retrieved child. </returns>
        public override WZObject this[string childName]
        {
            get { return GetChild(childName); }
        }

        internal override Bitmap Parse(WZBinaryReader br, bool initial)
        {
            br.Skip(1);
            if (br.ReadByte() == 1) {
                br.Skip(2);
                List<WZObject> l = WZExtendedParser.ParsePropertyList(br, this, Image, Image._encrypted);
                if (initial) l.ForEach(Add);
            }
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
    }
}