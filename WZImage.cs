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

namespace reWZ
{
    /// <summary>
    ///   A WZ image in a WZ file.
    /// </summary>
    public sealed class WZImage : WZObject
    {
        private readonly WZBinaryReader _r;
        internal bool _encrypted;
        private bool _parsed;

        internal WZImage(string name, WZObject parent, WZFile file, WZBinaryReader reader) : base(name, parent, file, true)
        {
            _r = reader;
            if (file._flag.IsSet(WZReadSelection.EagerParseImage)) Parse();
        }

        /// <summary>
        ///   Returns the child with the name <paramref name="childName" /> .
        /// </summary>
        /// <param name="childName"> The name of the child to return. </param>
        /// <returns> The retrieved child. </returns>
        public override WZObject this[string childName]
        {
            get
            {
                Parse();
                return base[childName];
            }
        }

        /// <summary>
        ///   Returns the number of children this property contains.
        /// </summary>
        public override int ChildCount
        {
            get
            {
                Parse();
                return base.ChildCount;
            }
        }

        /// <summary>
        ///   Checks if this property has a child with name <paramref name="name" /> .
        /// </summary>
        /// <param name="name"> The name of the child to locate. </param>
        /// <returns> true if this property has such a child, false otherwise or if this property cannot contain children. </returns>
        public override bool HasChild(string name)
        {
            Parse();
            return base.HasChild(name);
        }

        private void Parse()
        {
            if (_parsed) return;
            lock (File) {
                _r.Seek(0);
                if (_r.ReadByte() != 0x73) WZFile.Die("WZImage with invalid header (not beginning with 0x73!)");
                if ((int)File._variant == 2) _encrypted = false;
                else if (_r.PeekFor(() => _r.ReadWZString()) == "Property") _encrypted = true;
                else if (_r.PeekFor(() => _r.ReadWZString(false)) == "Property") _encrypted = false;
                else WZFile.Die("WZImage with invalid header (no Property string! check your WZVariant)");
                if (_r.ReadWZString(_encrypted) != "Property") WZFile.Die("Failed to determine image encryption!");
                if (_r.ReadUInt16() != 0) WZFile.Die("WZImage with invalid header (no zero UInt16!)");
                WZExtendedParser.ParsePropertyList(_r, this, this, _encrypted).ForEach(Add);
                _parsed = true;
            }
        }
    }
}