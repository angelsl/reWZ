using System;
using System.Collections.Generic;

namespace reWZ
{
    public class WZDirectory : WZObject
    {
        private readonly Dictionary<String, WZObject> _backing;

        internal WZDirectory(string name, WZObject parent, WZFile file, WZBinaryReader wzbr, long offset) : base(name, WZObjectType.Directory, parent, file)
        {
            _backing = new Dictionary<string, WZObject>();
            Parse(wzbr, offset);
        }

        public override WZObject this[string childName]
        {
            get
            {
                if (!_backing.ContainsKey(childName)) throw new KeyNotFoundException("No such child in WZDirectory.");
                return _backing[childName];
            }
        }

        internal void Add(WZObject o)
        {
            _backing.Add(o.Name, o);
        }

        internal void Parse(WZBinaryReader wzbr, long offset)
        {
            long orig = wzbr.Jump(offset);
            int entryCount = wzbr.ReadWZInt();
            for(int i = 0; i < entryCount; ++i) {
                byte type = wzbr.ReadByte();
                string name = null;
                switch(type) {
                    case 1:
                        wzbr.Skip(10);
                        continue;
                    case 2:
                        wzbr.PeekFor(() => {
                                         wzbr.Jump(wzbr.ReadInt32() + File._fstart);
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
                if(name == null) WZFile.Die("Failed to read WZDirectory entry name.");
                int size = wzbr.ReadWZInt();
                int checksum = wzbr.ReadWZInt();
                uint woffset = wzbr.ReadWZOffset(File._fstart);
                if(type == 3) {
                    Add(new WZDirectory(name, this, File, wzbr, woffset));
                } else if(type == 4) {
                    Add(new WZImage(name, this, File, new WZBinaryReader(File.GetSubstream(woffset, size), File._aes, wzbr.VersionHash)));
                }
            }
        }
    }
}