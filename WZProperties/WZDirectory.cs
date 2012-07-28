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

using System.IO;

namespace reWZ.WZProperties
{
    /// <summary>
    ///   A directory in a WZ file. This cannot be located inside an Image file.
    /// </summary>
    public sealed class WZDirectory : WZObject
    {
        internal WZDirectory(string name, WZObject parent, WZFile file, WZBinaryReader wzbr, long offset) : base(name, parent, file, true)
        {
            Parse(wzbr, offset);
        }

        private void Parse(WZBinaryReader wzbr, long offset)
        {
            lock (File._lock) {
                wzbr.Seek(offset);
                int entryCount = wzbr.ReadWZInt();
                for (int i = 0; i < entryCount; ++i) {
                    byte type = wzbr.ReadByte();
                    string name = null;
                    switch (type) {
                        case 1:
                            wzbr.Skip(10);
                            continue;
                        case 2:
                            int x = wzbr.ReadInt32();
                            wzbr.PeekFor(() => {
                                             wzbr.Seek(x + File._fstart);
                                             type = wzbr.ReadByte();
                                             name = wzbr.ReadWZString(File._encrypted);
                                         });

                            break;
                        case 3:
                        case 4:
                            name = wzbr.ReadWZString(File._encrypted);
                            break;
                        default:
                            WZFile.Die("Unknown object type in WzDirectory.");
                            break;
                    }
                    if (name == null) WZFile.Die("Failed to read WZDirectory entry name.");
                    int size = wzbr.ReadWZInt();
                    wzbr.ReadWZInt();
                    uint woffset = wzbr.ReadWZOffset(File._fstart);
                    wzbr.PeekFor(() => {
                                     switch (type) {
                                         case 3:
                                             Add(new WZDirectory(name, this, File, wzbr, woffset));
                                             break;
                                         case 4:
                                             if ((File._flag.IsSet(WZReadSelection.EagerParseCanvas) || File._flag.IsSet(WZReadSelection.EagerParseMP3) || File._flag.IsSet(WZReadSelection.EagerParseStrings)) && !(File._file is MemoryStream) && !File._flag.IsSet(WZReadSelection.LowMemory))
                                                 Add(new WZImage(name, this, File, new WZBinaryReader(File.GetSubbytes(woffset, size), File._aes, wzbr.VersionHash),
                                                                 () => new WZBinaryReader(File.GetSubstream(woffset, size), File._aes, wzbr.VersionHash)));
                                             else
                                                 Add(new WZImage(name, this, File, new WZBinaryReader(File.GetSubstream(woffset, size), File._aes, wzbr.VersionHash)));

                                             break;
                                     }
                                 });
                }
            }
        }
    }
}