// reWZ is copyright angelsl, 2011 to 2015 inclusive.
// 
// This file (WZAudioProperty.cs) is part of reWZ.
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
// Linking reWZ statically or dynamically with other modules
// is making a combined work based on reWZ. Thus, the terms and
// conditions of the GNU General Public License cover the whole combination.
// 
// As a special exception, the copyright holders of reWZ give you
// permission to link reWZ with independent modules to produce an
// executable, regardless of the license terms of these independent modules,
// and to copy and distribute the resulting executable under terms of your
// choice, provided that you also meet, for each linked independent module,
// the terms and conditions of the license of that module. An independent
// module is a module which is not derived from or based on reWZ.

using System;

namespace reWZ.WZProperties {
    /// <summary>
    ///     A sound property.
    /// </summary>
    public sealed class WZAudioProperty : WZDelayedProperty<byte[]>, IDisposable {
        private Guid[] _guid1;
        private Guid[] _guid2;
        private Guid[] _guid3;
        private byte[] _header;

        internal WZAudioProperty(string name, WZObject parent, WZImage container)
            : base(name, parent, container, false, WZObjectType.Audio) {}

        public byte[] Header {
            get {
                if (!_parsed)
                    CheckParsed();
                return _header;
            }
        }

        public Guid[] Guid1 {
            get {
                if (!_parsed)
                    CheckParsed();
                return _guid1;
            }
        }

        public Guid[] Guid2 {
            get {
                if (!_parsed)
                    CheckParsed();
                return _guid2;
            }
        }

        public Guid[] Guid3 {
            get {
                if (!_parsed)
                    CheckParsed();
                return _guid3;
            }
        }

        public int Length { get; private set; }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose() {
            _parsed = false;
            _value = null;
        }

        internal override bool Parse(WZBinaryReader r, bool initial, out byte[] result) {
            r.Skip(1);
            int blockLen = r.ReadWZInt(); // sound data length
            Length = r.ReadWZInt(); // sound duration
            if (!initial || (File._flag & WZReadSelection.EagerParseAudio) == WZReadSelection.EagerParseAudio) {
                _guid1 = r.ReadGuidArray();
                _guid2 = r.ReadGuidArray();
                _guid3 = r.ReadGuidArray();
                _header = r.ReadBytes(r.ReadByte());
                result = r.ReadBytes(blockLen);
                return true;
            } else {
                r.Skip(16*r.ReadByte());
                r.Skip(16*r.ReadByte());
                r.Skip(16*r.ReadByte());
                r.Skip(r.ReadByte());
                r.Skip(blockLen);
                result = null;
                return false;
            }
        }
    }
}
