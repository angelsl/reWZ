// reWZ is copyright angelsl, 2011 to 2012 inclusive.
// 
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
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using reWZ.WZProperties;

namespace reWZ
{
    /// <summary>
    ///   A WZ file.
    /// </summary>
    public sealed class WZFile : IDisposable
    {
        internal WZAES _aes;
        internal readonly bool _encrypted;
        internal Stream _file;
        internal readonly WZReadSelection _flag;
        private WZBinaryReader _r;
        internal readonly WZVariant _variant;
        private bool _disposed;
        private readonly bool _disposeStream;
        internal uint _fstart;
        internal readonly object _lock = new object();
        private WZDirectory _maindir;

        /// <summary>
        ///   Creates and loads a WZ file from a path. The Stream created will be disposed when the WZ file is disposed.
        /// </summary>
        /// <param name="path"> The path where the WZ file is located. </param>
        /// <param name="variant"> The variant of this WZ file. </param>
        /// <param name="encrypted"> Whether the WZ file is encrypted outside a WZ image. </param>
        /// <param name="flag"> WZ parsing flags. </param>
        public WZFile(string path, WZVariant variant, bool encrypted, WZReadSelection flag = WZReadSelection.None)
            : this(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 6144, FileOptions.RandomAccess), variant, encrypted, flag)
        {
            _disposeStream = true;
        }

        /// <summary>
        ///   Creates and loads a WZ file. The Stream passed will not be closed when the WZ file is disposed.
        /// </summary>
        /// <param name="input"> The stream containing the WZ file. </param>
        /// <param name="variant"> The variant of this WZ file. </param>
        /// <param name="encrypted"> Whether the WZ file is encrypted outside a WZ image. </param>
        /// <param name="flag"> WZ parsing flags. </param>
        public WZFile(Stream input, WZVariant variant, bool encrypted, WZReadSelection flag = WZReadSelection.None)
        {
            _file = input;
            _variant = variant;
            _encrypted = encrypted;
            _flag = flag;
            _aes = new WZAES(_variant);
            _r = new WZBinaryReader(_file, _aes, 0);
            Parse();
        }

        /// <summary>
        /// Disposer.
        /// </summary>
        ~WZFile()
        {
            Dispose(false);
        }

        /// <summary>
        ///   The root directory of the WZ file.
        /// </summary>
        public WZDirectory MainDirectory
        {
            get
            {
                CheckDisposed();
                return _maindir;
            }
        }

        #region IDisposable Members

        /// <summary>
        ///   Disposes this WZ file.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true); 
        }

        private void Dispose(bool disposing)
        {
            CheckDisposed();
            _r.Close(disposing && _disposeStream);
            _maindir = null;
            _aes = null;
            _r = null;
            _file = null;
            _disposed = true;
        }

        #endregion

        internal void CheckDisposed()
        {
            if(_disposed) throw new ObjectDisposedException("WZ file");
        }

        /// <summary>
        ///   Resolves a path in the form "/a/b/c/.././d/e/f/".
        /// </summary>
        /// <param name="path"> The path to resolve. </param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The path has an invalid node.</exception>
        public WZObject ResolvePath(string path)
        {
            CheckDisposed();
            return (path.StartsWith("/") ? path.Substring(1) : path).Split('/').Where(node => node != ".").Aggregate<string, WZObject>(_maindir, (current, node) => node == ".." ? current.Parent : current[node]);
        }

        private void Parse()
        {
            _r.Seek(0, SeekOrigin.Begin);
            lock (_lock) {
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
                        _r.SkipWZString();
                        break;
                    default:
                        Die("Unknown object type in WzDirectory.");
                        break;
                }

                _r.ReadWZInt();
                _r.ReadWZInt();
                offset = _r.BaseStream.Position;
                _r.Skip(4);
                if (type != 4) continue;
                success = true;
                break;
            }
            if (!success) Die("WZ file has no images!");
            success = false;
            for (ushort v = 0; v < ushort.MaxValue; v++) {
                uint vHash = v.ToString(CultureInfo.InvariantCulture).Aggregate<char, uint>(0, (current, t) => (32*current) + t + 1);
                if ((0xFF ^ (vHash >> 24) ^ (vHash << 8 >> 24) ^ (vHash << 16 >> 24) ^ (vHash << 24 >> 24)) != ver) continue;
                _r.Seek(offset);
                _r.VersionHash = vHash;
                try {
                    _r.Seek(_r.ReadWZOffset(_fstart));
                    if (_r.ReadByte() != 0x73 || (_r.PeekFor(() => _r.ReadWZString()) != "Property" && _r.PeekFor(() => _r.ReadWZString(false)) != "Property")) continue;
                    success = true;
                    break;
                } catch {
                    success = false;
                }
            }
            if (!success) Die("Failed to guess WZ file version!");
            _r.Seek(_fstart);
        }

        internal Stream GetSubbytes(long offset, long length)
        {
            CheckDisposed();
            byte[] @out = new byte[length];
            long p = _file.Position;
            _file.Position = offset;
            _file.Read(@out, 0, (int)length);
            _file.Position = p;
            return new MemoryStream(@out, false);
        }

        internal Stream GetSubstream(long offset, long length)
        {
            CheckDisposed();
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

    /// <summary>
    ///   WZ reading flags.
    /// </summary>
    [Flags]
    public enum WZReadSelection : byte
    {
        /// <summary>
        ///   No flags are enabled, that is, lazy loading of properties and WZ images is enabled.
        /// </summary>
        None = 0,

        /// <summary>
        ///   Set this flag to disable lazy loading of string properties.
        /// </summary>
        EagerParseStrings = 1,

        /// <summary>
        ///   Set this flag to disable lazy loading of MP3 properties.
        /// </summary>
        EagerParseMP3 = 2,

        /// <summary>
        ///   Set this flag to disable lazy loading of canvas properties.
        /// </summary>
        EagerParseCanvas = 4,

        /// <summary>
        ///   Set this flag to completely disable loading of canvas properties.
        /// </summary>
        NeverParseCanvas = 8,

        /// <summary>
        ///   Set this flag to disable lazy loading of string, MP3 and canvas properties.
        /// </summary>
        EagerParseAll = EagerParseCanvas | EagerParseMP3 | EagerParseStrings,

        /// <summary>
        ///   Set this flag to disable lazy loading of WZ images.
        /// </summary>
        EagerParseImage = 16,

        /// <summary>
        ///   Set this flag to disable reading entire WZ images into memory when any of the eager load flags are set.
        /// </summary>
        LowMemory = 32
    }
}