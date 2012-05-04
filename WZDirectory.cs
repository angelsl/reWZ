using System;
using System.Collections.Generic;

namespace reWZ
{
    public class WZDirectory : WZChildContainer
    {
        internal WZDirectory(string name, WZObject parent, WZFile file, WZBinaryReader wzbr, long offset) : base(name, parent, file)
        {
            Parse(wzbr, offset);
        }

        internal void Parse(WZBinaryReader wzbr, long offset)
        {
            lock (File) {
                wzbr.Jump(offset);
                int entryCount = wzbr.ReadWZInt();
                for (int i = 0; i < entryCount; ++i) {
                    byte type = wzbr.ReadByte();
                    string name = null;
                    switch (type) {
                        case 1:
                            wzbr.Skip(10);
                            continue;
                        case 2:
                            wzbr.PeekFor(() => {
                                             wzbr.Jump(wzbr.ReadInt32() + File._fstart);
                                             type = wzbr.ReadByte();
                                             name = wzbr.ReadWZString(File._encrypted);
                                         });
                            wzbr.Skip(4);
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
                    wzbr.PeekFor(() =>
                    {
                        switch (type) {
                            case 3:
                                Add(new WZDirectory(name, this, File, wzbr, woffset));
                                break;
                            case 4:
                                Add(new WZImage(name, this, File, new WZBinaryReader(File.GetSubstream(woffset, size), File._aes, wzbr.VersionHash)));
                                break;
                        }
                    });
                    
                }
            }
        }
    }
}