using System;
using System.Collections.Generic;
using System.Text;
using reWZ.WZProperties;

namespace reWZ
{
    public abstract class WZProperty<T> : WZObject
    {
        private readonly T _value;

        internal WZProperty(string name, WZObject parent, T value, WZFile container) : base(name, WZObjectType.Property, parent, container)
        {
            _value = value;
        }

        public T Value
        {
            get { return _value; }
        }
    }

    public abstract class WZObject
    {
        private readonly string _name;
        private readonly WZObject _parent;
        private readonly string _path;
        private readonly WZObjectType _type;
        private readonly WZFile _file;

        internal WZObject(string name, WZObjectType type, WZObject parent, WZFile container)
        {
            _name = name;
            _type = type;
            _parent = parent;
            _path = ConstructPath();
            _file = container;
        }

        public string Name
        {
            get { return _name; }
        }

        public WZObjectType ObjectType
        {
            get { return _type; }
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

        public virtual WZObject this[String childName]
        {
            get { throw new NotSupportedException("This WZObject cannot contain children."); }
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
        internal static List<WZObject> ParsePropertyList(WZBinaryReader r, WZObject parent, WZFile f, bool encrypted)
        {
            int num = r.ReadWZInt();
            List<WZObject> ret = new List<WZObject>(num);
            for (int i = 0; i < num; ++i)
            {
                string name = r.ReadWZStringBlock(encrypted);
                switch (r.ReadByte())
                {
                    case 0:
                        ret.Add(new WZNullProperty(name, parent, f));
                        break;
                    case 0x0B:
                    case 2:
                        ret.Add(new WZUInt16Property(name, parent, r.ReadUInt16(), f));
                        break;
                    case 3:
                        ret.Add(new WZInt32Property(name, parent, r.ReadWZInt(), f));
                        break;
                    case 4:
                        byte t = r.ReadByte();
                        ret.Add(new WZSingleProperty(name, parent, t == 0x80 ? r.ReadSingle() : (t == 0 ? 0f : WZFile.Die<float>("Unknown byte while reading WZSingleProperty.")), f));
                        break;
                    case 5:
                        ret.Add(new WZDoubleProperty(name, parent, r.ReadDouble(), f));
                        break;
                    case 8:
                        ret.Add(new WZStringProperty(name, parent, r.ReadWZStringBlock(encrypted), f));
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

    public enum WZObjectType
    {
        Directory,
        Image,
        Property
    }

    public enum WZPropertyType
    {
        Null,
        UInt16,
        Int32,
        Single,
        Double,
        String,
        Subproperty,
        Canvas,
        Vector,
        Sound,
        Link
    }
}