using System;
using System.IO;
using System.Text;

namespace reWZ
{
    public class WZBinaryReader : BinaryReader
    {
        private readonly WZAES _aes;
        private uint _versionHash;

        public WZBinaryReader(Stream inStream, WZAES aes, uint versionHash) : base(inStream, Encoding.ASCII)
        {
            _aes = aes;
            _versionHash = versionHash;
        }

        internal uint VersionHash
        {
            get { return _versionHash; }
            set { _versionHash = value; }
        }

        /// <summary>
        ///   Sets the position within the backing stream to the specified value.
        /// </summary>
        /// <param name="offset"> The new position within the backing stream. This is relative to the <paramref name="loc" /> parameter, and can be positive or negative. </param>
        /// <param name="loc"> A value of type <see cref="T:System.IO.SeekOrigin" /> , which acts as the seek reference point. This defaults to <code>SeekOrigin.Begin</code> . </param>
        /// <returns> The old position within the backing stream. </returns>
        public long Jump(long offset, SeekOrigin loc = SeekOrigin.Begin)
        {
            long ret = BaseStream.Position;
            BaseStream.Seek(offset, loc);
            return ret;
        }

        /// <summary>
        ///   Advances the position within the backing stream by <paramref name="count" /> .
        /// </summary>
        /// <param name="count"> The amount of bytes to skip. </param>
        public void Skip(long count)
        {
            BaseStream.Position += count;
        }

        /// <summary>
        ///   Executes a delegate of type <see cref="System.Func{TResult}" /> , then sets the position of the backing stream back to the original value.
        /// </summary>
        /// <typeparam name="T"> The return type of the delegate. </typeparam>
        /// <param name="result"> The delegate to execute. </param>
        /// <returns> The object returned by the delegate. </returns>
        public T PeekFor<T>(Func<T> result)
        {
            long orig = BaseStream.Position;
            T ret = result();
            BaseStream.Position = orig;
            return ret;
        }

        /// <summary>
        ///   Reads a string encoded in WZ format.
        /// </summary>
        /// <param name="encrypted"> Whether the string is encrypted. </param>
        /// <returns> The read string. </returns>
        public string ReadWZString(bool encrypted = true)
        {
            int length = ReadSByte();
            if (length == 0) return "";
            if (length > 0) {
                length = length == 127 ? ReadInt32() : length;
                if (length == 0) return "";
                ushort[] raw = new ushort[length];
                for (int i = 0; i < length; ++i)
                    raw[i] = ReadUInt16();
                return _aes.DecryptUnicodeString(raw, encrypted);
            } else { // !(length >= 0), i think we can assume length < 0, but the compiler can't seem to see that
                length = length == -128 ? ReadInt32() : -length;
                if (length == 0) return "";
                return _aes.DecryptASCIIString(ReadBytes(length), encrypted);
            }
        }

        /// <summary>
        ///   Reads a string encoded in WZ format at a specific offset, then returns the backing stream's position to its original value.
        /// </summary>
        /// <param name="offset"> The offset where the string is located. </param>
        /// <param name="encrypted"> Whether the string is encrypted. </param>
        /// <returns> The read string. </returns>
        public string ReadWZStringAtOffset(long offset, bool encrypted = true)
        {
            return PeekFor(() => {
                               BaseStream.Position = offset;
                               return ReadWZString(encrypted);
                           });
        }

        /// <summary>
        ///   Reads a raw and unencrypted ASCII string.
        /// </summary>
        /// <param name="length"> The length of the string. </param>
        /// <returns> The read string. </returns>
        public string ReadASCIIString(int length)
        {
            return Encoding.ASCII.GetString(ReadBytes(length));
        }

        /// <summary>
        ///   Reads a raw and unencrypted null-terminated ASCII string.
        /// </summary>
        /// <returns> The read string. </returns>
        public string ReadASCIIZString()
        {
            StringBuilder sb = new StringBuilder();
            byte b;
            while ((b = ReadByte()) != 0)
                sb.Append((char)b);
            return sb.ToString();
        }

        /// <summary>
        ///   Reads a WZ-compressed 32-bit integer.
        /// </summary>
        /// <returns> The read integer. </returns>
        public int ReadWZInt()
        {
            sbyte s = ReadSByte();
            return s == -128 ? ReadInt32() : s;
        }

        public uint ReadWZOffset(uint fstart)
        {
            unchecked {
                uint ret = ((((uint)BaseStream.Position - fstart) ^ 0xFFFFFFFF)*_versionHash) - WZAES.OffsetKey;
                return (((ret << (int)ret) | (ret >> (int)(32 - ret))) ^ ReadUInt32()) + (fstart*2);
            }
        }
    }
}