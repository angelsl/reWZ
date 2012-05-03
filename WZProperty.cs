using System;
using System.Text;

namespace reWZ
{
    public abstract class WZProperty<T> : WZObject
    {
        private readonly T _value;

        internal WZProperty(string name, WZObject parent, T value) : base(name, WZObjectType.Property, parent)
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

        internal WZObject(string name, WZObjectType type, WZObject parent)
        {
            _name = name;
            _type = type;
            _parent = parent;
            _path = ConstructPath();
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

        public WZObject this[String childName]
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