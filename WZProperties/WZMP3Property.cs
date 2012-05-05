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

namespace reWZ.WZProperties
{
    /// <summary>
    /// A sound property.
    /// </summary>
    public class WZMP3Property : WZProperty<byte[]>
    {
        internal WZMP3Property(string name, WZObject parent, WZBinaryReader r, WZImage container) : base(name, parent, r, container, false)
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