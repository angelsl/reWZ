﻿// This file is part of reWZ.
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

using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace reWZ
{
    public class WZFile : IDisposable
    {
        internal readonly WZAES _aes;
        private readonly BufferedStream _buffer;
        internal readonly bool _encrypted;
        internal readonly bool _parseAll;
        private readonly Stream _file;
        private readonly WZBinaryReader _r;
        private readonly WZVariant _variant;
        private bool _disposed;
        internal uint _fstart;
        private WZDirectory _maindir;

        /// <summary>
        ///   Creates and loads a WZ file.
        /// </summary>
        /// <param name="path"> The path where the WZ file is located. </param>
        /// <param name="variant"> The variant of this WZ file. </param>
        /// <param name="encrypted"> Whether the WZ file is encrypted outside a WZ image. </param>
        /// <param name="parseAll"> Whether to parse the WZ file completely, or on demand. </param>
        public WZFile(string path, WZVariant variant, bool encrypted, bool parseAll = false) : this(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read), variant, encrypted, parseAll)
        {}

        /// <summary>
        ///   Creates and loads a WZ file.
        /// </summary>
        /// <param name="input"> The stream containing the WZ file. </param>
        /// <param name="variant"> The variant of this WZ file. </param>
        /// <param name="encrypted"> Whether the WZ file is encrypted outside a WZ image. </param>
        /// <param name="parseAll"> Whether to parse the WZ file completely, or on demand. </param>
        public WZFile(Stream input, WZVariant variant, bool encrypted, bool parseAll = false)
        {
            _file = input;
            _variant = variant;
            _encrypted = encrypted;
            _parseAll = parseAll;
            _aes = new WZAES(_variant);
            _r = new WZBinaryReader(_file, _aes, 0);
            Parse();
        }

        public WZDirectory MainDirectory
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("WZ file");
                return _maindir;
            }
        }

        #region IDisposable Members

        /// <summary>
        ///   Disposes this WZ file.
        /// </summary>
        public void Dispose()
        {
            _r.Dispose();
            _file.Dispose();
            _disposed = true;
        }

        #endregion

        /// <summary>
        ///   Resolves a path in the form "/a/b/c/.././d/e/f/".
        /// </summary>
        /// <param name="path"> The path to resolve. </param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The path has an invalid node.</exception>
        public WZObject ResolvePath(string path)
        {
            return (path.StartsWith("/") ? path.Substring(1) : path).Split('/').Where(node => node != ".").Aggregate<string, WZObject>(_maindir, (current, node) => node == ".." ? current.Parent : current[node]);
        }

        private void Parse()
        {
            lock (this) {
                if (_r.ReadASCIIString(4) != "PKG1") Die("WZ file has invalid header; file does not have magic \"PKG1\".");
                _r.Skip(8);
                _fstart = _r.ReadUInt32();
                _r.ReadASCIIZString();
                GuessVersion();
                _maindir = new WZDirectory("", null, this, _r, _fstart + 2);
            }
        }

        private void GuessVersion()
        {
            _r.Seek(_fstart);
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
                                              _r.Seek(x + _fstart);
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
                _r.Seek(offset);
                _r.VersionHash = vHash;
                _r.Seek(_r.ReadWZOffset(_fstart));
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
            _r.Seek(_fstart);
        }

        internal Stream GetSubstream(long offset, long length)
        {
            return new Substream(_file, offset, length);
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