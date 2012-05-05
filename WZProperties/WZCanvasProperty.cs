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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace reWZ.WZProperties
{
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
                if (Count == 0) l.ForEach(Add);
            }
            int width = br.ReadWZInt(); // width
            int height = br.ReadWZInt(); // height
            int format1 = br.ReadWZInt(); // format 1
            int format2 = br.ReadByte(); // format 2
            br.Skip(4);
            int blockLen = br.ReadInt32();
            if (initial) br.Skip(blockLen); // block Len & png data
            else {
                br.Skip(1);
                ushort header = br.PeekFor(() => br.ReadUInt16());
                byte[] pngData = br.ReadBytes(blockLen - 1);
                return ParsePNG(width, height, format1 + format2, header != 0x9C78 && header != 0xDA78 ? DecryptPNG(pngData) : pngData);
            }
            return null;
        }

        private byte[] DecryptPNG(byte[] @in)
        {
            using(MemoryStream @sIn = new MemoryStream(@in, false))
            using(BinaryReader @sBr = new BinaryReader(@sIn))
            using(MemoryStream @sOut = new MemoryStream(@in.Length)){
                while(@sIn.Position < @sIn.Length) {
                    int blockLen = @sBr.ReadInt32();
                    @sOut.Write(Image.File._aes.DecryptBytes(@sBr.ReadBytes(blockLen)), 0, blockLen);
                }
                return @sOut.ToArray();
            }
        }

        private Bitmap ParsePNG(int width, int height, int format, byte[] data)
        {
            byte[] dec;
            using (MemoryStream @in = new MemoryStream(data, 2, data.Length - 2))
                dec = WZBinaryReader.Inflate(@in);

            switch (format) {
                case 1:
                {
                    Bitmap ret = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    BitmapData bmpData = ret.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Debug.Assert(dec.Length == width*height*2);
                    byte[] argb = new byte[dec.Length*2];
                    for (int i = 0; i < dec.Length; i++) {
                        int b = dec[i] & 0x0F;
                        argb[i*2] = (byte)(b | (b << 4));
                        b = dec[i] & 0xF0;
                        argb[i*2 + 1] = (byte)(b | ((b >> 4)));
                    }
                    Marshal.Copy(argb, 0, bmpData.Scan0, argb.Length);
                    ret.UnlockBits(bmpData);
                    return ret;
                }
                case 2:
                {
                    Bitmap ret = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    BitmapData bmpData = ret.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Debug.Assert(dec.Length == width*height*4);
                    Marshal.Copy(dec, 0, bmpData.Scan0, dec.Length);
                    ret.UnlockBits(bmpData);
                    return ret;
                }
                case 513:
                {
                    Bitmap ret = new Bitmap(width, height, PixelFormat.Format16bppRgb565);
                    BitmapData bmpData = ret.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb565);
                    Debug.Assert(dec.Length == width * height * 2);
                    Marshal.Copy(dec, 0, bmpData.Scan0, dec.Length);
                    ret.UnlockBits(bmpData);
                    return ret;
                }

                case 517:
                {
                    Bitmap ret = new Bitmap(width, height);
                    Debug.Assert(dec.Length == width * height / 128);
                    int x = 0, y = 0;
                    unchecked {
                        foreach (byte t in dec)
                            for (byte j = 0; j < 8; j++) {
                                byte iB = (byte)(((t & (0x01 << (7 - j))) >> (7 - j))*0xFF);
                                for (int k = 0; k < 16; k++) {
                                    if (x == width) {
                                        x = 0;
                                        y++;
                                    }
                                    ret.SetPixel(x, y, Color.FromArgb(0xFF, iB, iB, iB));
                                    x++;
                                }
                            }
                    }
                    return ret;
                }
                default:
                    return WZFile.Die<Bitmap>(String.Format("Unknown bitmap format {0}.", format));
            }
        }
    }
}