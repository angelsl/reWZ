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

namespace reWZ
{
    public sealed class WZImage : WZObject
    {
        private readonly WZBinaryReader _r;
        internal bool _encrypted;
        private bool _parsed;

        internal WZImage(string name, WZObject parent, WZFile file, WZBinaryReader reader) : base(name, parent, file, true)
        {
            _r = reader;
            if (file._parseAll) Parse();
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
                if (!_parsed) Parse();
                return base[childName];
            }
        }

        private void Parse()
        {
            lock (File) {
                _r.Seek(0);
                if (_r.ReadByte() != 0x73) WZFile.Die("WZImage with invalid header (not beginning with 0x73!)");
                if (_r.PeekFor(() => _r.ReadWZString()) == "Property") _encrypted = true;
                else if (_r.PeekFor(() => _r.ReadWZString(false)) == "Property") _encrypted = false;
                else WZFile.Die("WZImage with invalid header (no Property string! check your WZVariant)");
                if (_r.ReadWZString(_encrypted) != "Property") WZFile.Die("Failed to deduce encryption of image.");
                if (_r.ReadUInt16() != 0) WZFile.Die("WZImage with invalid header (no zero UInt16!)");
                WZExtendedParser.ParsePropertyList(_r, this, this, _encrypted).ForEach(Add);
                _parsed = true;
            }
        }
    }
}