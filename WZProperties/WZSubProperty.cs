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

using System.Globalization;

namespace reWZ.WZProperties
{
    public class WZSubProperty : WZProperty<WZNothing>
    {
        internal WZSubProperty(string name, WZObject parent, WZBinaryReader r, WZImage container) : base(name, parent, default(WZNothing), container)
        {
            WZExtendedParser.ParsePropertyList(r, this, Image, Image._encrypted).ForEach(Add);
        }
    }

    public class WZConvexProperty : WZProperty<WZNothing>
    {
        internal WZConvexProperty(string name, WZObject parent, WZBinaryReader r, WZImage container)
            : base(name, parent, default(WZNothing), container)
        {
            int count = r.ReadWZInt();
            for (int i = 0; i < count; ++i)
                Add(WZExtendedParser.ParseExtendedProperty(i.ToString(CultureInfo.InvariantCulture), r, this, Image, Image._encrypted));
        }
    }
}