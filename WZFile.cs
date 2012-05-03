using System;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace reWZ
{
    public class WZFile : IDisposable
    {
        internal readonly WZAES _aes;
        internal readonly bool _encrypted;
        private readonly MemoryMappedFile _file;
        private readonly WZBinaryReader _r;
        private readonly WZVariant _variant;
        private WZDirectory _maindir;
        internal uint _fstart;

        public WZFile(string path, WZVariant variant, bool encrypted) : this(MemoryMappedFile.CreateFromFile(path, FileMode.Open), variant, encrypted)
        {}

        public WZFile(MemoryMappedFile input, WZVariant variant, bool encrypted)
        {
            _file = input;
            _variant = variant;
            _encrypted = encrypted;
            _aes = new WZAES(_variant);
            _r = new WZBinaryReader(_file.CreateViewStream(), _aes, 0);
            Parse();
        }

        public void Dispose()
        {
            _r.Dispose();
            _file.Dispose();
        }

        private void Parse()
        {
            if (_r.ReadASCIIString(4) != "PKG1") Die("WZ file has invalid header; file does not have magic \"PKG1\".");
            _r.Skip(8);
            _fstart = _r.ReadUInt32();
            _r.ReadASCIIZString();
            GuessVersion();
            _maindir = new WZDirectory("", null, this, _r, _fstart + 2);
        }

        private void GuessVersion()
        {
            _r.Jump(_fstart);
            short ver = _r.ReadInt16();
            int count = _r.ReadWZInt();
            if (count == 0) Die("WZ file has no entries!");
            long offset = 0;
            bool success = false;
            for (int i = 0; i < count; i++) {
                byte type = _r.ReadByte();
                switch (type) {
                    case 1:
                        _r.Skip(10);
                        continue;
                    case 2:
                        int x = _r.ReadInt32();
                        type = _r.PeekFor(() => {
                                              _r.Jump(x + _fstart);
                                              return _r.ReadByte();
                                          });
                        break;
                    case 3:
                    case 4:
                        _r.ReadWZString();
                        break;
                    default:
                        Die("Unknown object type in WzDirectory.");
                        break;
                }

                _r.ReadWZInt();
                _r.ReadWZInt();
                offset = _r.BaseStream.Position;
                _r.Skip(4);
                if (type == 4) {
                    success = true;
                    break;
                }
            }
            if (!success) Die("WZ file has no images!");
            success = false;
            uint vHash;
            for (ushort v = 0; v < ushort.MaxValue; v++) {
                vHash = v.ToString(CultureInfo.InvariantCulture).Aggregate<char, uint>(0, (current, t) => (32*current) + t + 1);
                if ((0xFF ^ (vHash >> 24) ^ (vHash << 8 >> 24) ^ (vHash << 16 >> 24) ^ (vHash << 24 >> 24)) != ver) continue;
                _r.Jump(offset);
                _r.VersionHash = vHash;
                _r.Jump(_r.ReadWZOffset(_fstart));
                try {
                    if (_r.ReadByte() == 0x73 && (_r.PeekFor(() => _r.ReadWZString()) == "Property" || _r.PeekFor(() => _r.ReadWZString(false)) == "Property")) {
                        success = true;
                        break;
                    }
                } catch {
                    success = false;
                }
            }
            if (!success) Die("Failed to guess WZ file version!");
            _r.Jump(_fstart);
        }

        internal Stream GetSubstream(long offset, long length)
        {
            return _file.CreateViewStream(offset, length);
        }

        internal static T Die<T>(string cause)
        {
            throw new WZException(cause);
        }

        internal static void Die(string cause)
        {
            throw new WZException(cause);
        }
    }
}