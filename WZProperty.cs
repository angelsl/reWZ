using System;
using System.Collections.Generic;
using System.Text;
using reWZ.WZProperties;

namespace reWZ
{
    public abstract class WZProperty<T> : WZChildContainer
    {
        private readonly WZImage _image;
        private bool _parsed;
        private T _value;
        private WZBinaryReader _reader;

        internal WZProperty(string name, WZObject parent, T value, WZImage container) : base(name, parent, container.File)
        {
            _value = value;
            _image = container;
            _parsed = true;
            _reader = null;
        }

        internal WZProperty(string name, WZObject parent, WZBinaryReader r, WZImage container) : base(name, parent, container.File)
        {
            _value = default(T);
            _image = container;
            _reader = r;
            Parse(r, true);
        }

        virtual protected T Parse(WZBinaryReader r, bool initial)
        {
            throw new NotSupportedException("This is not supposed to happen.");
        }

        public T Value
        {
            get
            {
                if (!_parsed) {
                    _value = Parse(_reader, false);
                    _parsed = true;
                }
                return _value;
            }
        }

        public WZImage Image
        {
            get { return _image; }
        }

        public override WZObject this[string childName]
        {
            get { throw new NotSupportedException("This WZProperty cannot contain children."); }
        }
    }

    public abstract class WZChildContainer : WZObject
    {
        private readonly Dictionary<String, WZObject> _backing;

        internal WZChildContainer(string name, WZObject parent, WZFile container) : base(name, parent, container)
        {
            _backing = new Dictionary<string, WZObject>();
        }

        public override WZObject this[string childName]
        {
            get { return GetChild(childName); }
        }

        protected WZObject GetChild(string childName)
        {
            if (!_backing.ContainsKey(childName)) throw new KeyNotFoundException("No such child in WZDirectory.");
            return _backing[childName];
        }

        internal void Add(WZObject o)
        {
            _backing.Add(o.Name, o);
        }
    }

    public abstract class WZObject
    {
        private readonly WZFile _file;
        private readonly string _name;
        private readonly WZObject _parent;
        private readonly string _path;

        internal WZObject(string name, WZObject parent, WZFile container)
        {
            _name = name;
            _parent = parent;
            _path = ConstructPath();
            _file = container;
        }

        public string Name
        {
            get { return _name; }
        }

        public WZObject Parent
        {
            get { return _parent; }
        }

        public string Path
        {
            get { return _path; }
        }

        public WZFile File
        {
            get { return _file; }
        }

        public virtual WZObject this[string childname]
        {
            get { throw new NotSupportedException("This WZObject cannot contain children."); }
        }

        public T ValueOrDie<T>()
        {
            return ((WZProperty<T>)this).Value;
        }

        public T ValueOrDefault<T>(T @default)
        {
            if (this is WZProperty<T>) return ((WZProperty<T>)this).Value;
            return @default;
        }

        internal string ConstructPath()
        {
            StringBuilder s = new StringBuilder(_name);
            WZObject p = this;
            while ((p = p.Parent) != null)
                s.Insert(0, "/").Insert(0, p.Name);
            return s.ToString();
        }
    }

    internal static class WZExtendedParser
    {
        internal static List<WZObject> ParsePropertyList(WZBinaryReader r, WZObject parent, WZImage f, bool encrypted)
        {
            lock (f.File) {
                int num = r.ReadWZInt();
                List<WZObject> ret = new List<WZObject>(num);
                for (int i = 0; i < num; ++i) {
                    string name = r.ReadWZStringBlock(encrypted);
                    switch (r.ReadByte()) {
                        case 0:
                            ret.Add(new WZNullProperty(name, parent, f));
                            break;
                        case 0x0B:
                        case 2:
                            ret.Add(new WZUInt16Property(name, parent, r, f));
                            break;
                        case 3:
                            ret.Add(new WZInt32Property(name, parent, r, f));
                            break;
                        case 4:
                            ret.Add(new WZSingleProperty(name, parent, r, f));
                            break;
                        case 5:
                            ret.Add(new WZDoubleProperty(name, parent, r, f));
                            break;
                        case 8:
                            ret.Add(new WZStringProperty(name, parent, r, f));
                            break;
                        case 9:
                            uint blockLen = r.ReadUInt32();
                            // TODO: read extended properties
                            r.Skip(blockLen);
                            break;
                        default:
                            throw new Exception("Unknown property type at ParsePropertyList");
                    }
                }
                return ret;
            }
        }
    }
}