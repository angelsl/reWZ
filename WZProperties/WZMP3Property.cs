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

namespace reWZ.WZProperties
{
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