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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace reWZ.WZProperties
{
    /// <summary>
    ///   A bitmap property, containing an image, and children.
    /// </summary>
    public class WZCanvasProperty : WZProperty<Bitmap>
    {
        internal WZCanvasProperty(string name, WZObject parent, WZBinaryReader br, WZImage container)
            : base(name, parent, br, container, true)
        {}

        internal override Bitmap Parse(WZBinaryReader br, bool initial)
        {
            br.Skip(1);
            if (br.ReadByte() == 1) {
                br.Skip(2);
                List<WZObject> l = WZExtendedParser.ParsePropertyList(br, this, Image, Image._encrypted);
                if (ChildCount == 0) l.ForEach(Add);
            }
            int width = br.ReadWZInt(); // width
            int height = br.ReadWZInt(); // height
            int format1 = br.ReadWZInt(); // format 1
            int format2 = br.ReadByte(); // format 2
            br.Skip(4);
            int blockLen = br.ReadInt32();
            if (initial) br.Skip(blockLen); // block Len & png data
            else {
                byte n = br.ReadByte();
                ushort header = br.PeekFor(() => br.ReadUInt16());
                Debug.Assert((n != 0) == (header != 0x9C78 && header != 0xDA78));
                //Debug.Assert(n == 0);
                byte[] pngData = br.ReadBytes(blockLen - 1);
                return ParsePNG(width, height, format1 + format2, (header != 0x9C78 && header != 0xDA78) ? DecryptPNG(pngData) : pngData);
            }
            return null;
        }

        private byte[] DecryptPNG(byte[] @in)
        {
            using (MemoryStream @sIn = new MemoryStream(@in, false))
            using (BinaryReader @sBr = new BinaryReader(@sIn))
            using (MemoryStream @sOut = new MemoryStream(@in.Length)) {
                while (@sIn.Position < @sIn.Length) {
                    int blockLen = @sBr.ReadInt32();
                    @sOut.Write(File._aes.DecryptBytes(@sBr.ReadBytes(blockLen)), 0, blockLen);
                }
                return @sOut.ToArray();
            }
        }

        private Bitmap ParsePNG(int width, int height, int format, byte[] data)
        {
            byte[] dec;
#if ZLIB
            using (MemoryStream @in = new MemoryStream(data, 0, data.Length))
#else
            using (MemoryStream @in = new MemoryStream(data, 2, data.Length - 2))
#endif
                dec = WZBinaryReader.Inflate(@in);

            switch (format) {
                case 1:
                    byte[] argb = new byte[width * height * 4];
                    for (int i = 0; i < dec.Length; i++) {
                        argb[i*2] = (byte)((dec[i] & 0x0F)*0x11);
                        argb[i*2 + 1] = (byte)(((dec[i] & 0xF0) >> 4)*0x11);
                    }
                    dec = argb;
                    goto case 2;
                case 2:
                    if (dec.Length != width * height * 4) {
                        Debug.WriteLine("Warning; dec.Length != 4wh; 32BPP");
                        byte[] proper = new byte[width*height*4];
                        Buffer.BlockCopy(dec, 0, proper, 0, Math.Min(proper.Length, dec.Length));
                        dec = proper;
                    }
                    return new Bitmap(width, height, 4*width, PixelFormat.Format32bppArgb, GCHandle.Alloc(dec, GCHandleType.Pinned).AddrOfPinnedObject());
                case 513:
                    if (dec.Length != width * height * 2) {
                        Debug.WriteLine("Warning; dec.Length != 2wh; 16BPP");
                        byte[] proper = new byte[width*height*2];
                        Buffer.BlockCopy(dec, 0, proper, 0, Math.Min(proper.Length, dec.Length));
                        dec = proper;
                    }
                    return new Bitmap(width, height, dec.Length / height, PixelFormat.Format16bppRgb565, GCHandle.Alloc(dec, GCHandleType.Pinned).AddrOfPinnedObject());
                case 517:
                    width >>= 4;
                    height >>= 4;
                    goto case 513;
                default:
                    return WZFile.Die<Bitmap>(String.Format("Unknown bitmap format {0}.", format));
            }
        }
    }
}