using System;

namespace reWZ.WZProperties {
    /// <summary>
    ///     A sound property.
    /// </summary>
    public sealed class WZAudioProperty : WZDelayedProperty<byte[]>, IDisposable {
        private byte[] _header;
        private Guid[] _guid1;
        private Guid[] _guid2;
        private Guid[] _guid3;

        public byte[] Header {
            get {
                if(!_parsed)
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

        internal WZAudioProperty(string name, WZObject parent, WZImage container)
            : base(name, parent, container, false, WZObjectType.Audio) {}

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
                r.Skip(16 * r.ReadByte());
                r.Skip(16 * r.ReadByte());
                r.Skip(16 * r.ReadByte());
                r.Skip(r.ReadByte());
                r.Skip(blockLen);
                result = null;
                return false;
            }
        }
    }
}