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
using System.Linq;
using System.Text;
using reWZ.WZProperties;

namespace reWZ
{
    public abstract class WZProperty<T> : WZChildContainer
    {
        private readonly WZImage _image;
        private long _offset;
        private bool _parsed;
        private WZBinaryReader _reader;
        private T _value;

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
            _offset = r.BaseStream.Position;
            Parse(r, true);
        }

        /// <summary>
        ///   The value held by this WZ property.
        /// </summary>
        public T Value
        {
            get
            {
                if (!_parsed) {
                    lock (File)
                        _value = _reader.PeekFor(() => {
                                                     _reader.Seek(_offset);
                                                     return Parse(_reader, false);
                                                 });
                    _parsed = true;
                }
                return _value;
            }
        }

        /// <summary>
        ///   The image that this property resides in.
        /// </summary>
        public WZImage Image
        {
            get { return _image; }
        }

        /// <summary>
        ///   Throws a <see cref="System.NotSupportedException" /> .
        /// </summary>
        /// <param name="childName"> Does nothing. </param>
        /// <exception cref="System.NotSupportedException">This method was called.</exception>
        /// <returns> Does not return. </returns>
        public override WZObject this[string childName]
        {
            get { throw new NotSupportedException("This WZProperty cannot contain children."); }
        }

        internal virtual T Parse(WZBinaryReader r, bool initial)
        {
            throw new NotSupportedException("This is not supposed to happen.");
        }
    }

    public abstract class WZChildContainer : WZObject
    {
        private readonly Dictionary<String, WZObject> _backing;

        internal WZChildContainer(string name, WZObject parent, WZFile container) : base(name, parent, container)
        {
            _backing = new Dictionary<string, WZObject>();
        }

        /// <summary>
        ///   Returns the child with the name <paramref name="childName" /> .
        /// </summary>
        /// <param name="childName"> The name of the child to return. </param>
        /// <returns> The retrieved child. </returns>
        public override WZObject this[string childName]
        {
            get { return GetChild(childName); }
        }

        /// <summary>
        ///   Resolves a path in the form "/a/b/c/.././d/e/f/".
        /// </summary>
        /// <param name="path"> The path to resolve. </param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The path has an invalid node.</exception>
        /// <returns> The object located at the path. </returns>
        public WZObject ResolvePath(string path)
        {
            return (path.StartsWith("/") ? path.Substring(1) : path).Split('/').Where(node => node != ".").Aggregate<string, WZObject>(this, (current, node) => node == ".." ? current.Parent : current[node]);
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

        /// <summary>
        ///   The name of the WZ object.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        ///   The parent of this WZ object, or <code>null</code> if this is the main WZ directory.
        /// </summary>
        public WZObject Parent
        {
            get { return _parent; }
        }

        /// <summary>
        ///   The absolute path to this object.
        /// </summary>
        public string Path
        {
            get { return _path; }
        }

        /// <summary>
        ///   The WZ file containing this object.
        /// </summary>
        public WZFile File
        {
            get { return _file; }
        }

        /// <summary>
        ///   Throws a <see cref="System.NotSupportedException" /> .
        /// </summary>
        /// <param name="childname"> Does nothing. </param>
        /// <exception cref="System.NotSupportedException">This method was called.</exception>
        /// <returns> Does not return. </returns>
        public virtual WZObject this[string childname]
        {
            get { throw new NotSupportedException("This WZObject cannot contain children."); }
        }

        /// <summary>
        ///   Tries to cast this to a <see cref="WZProperty{T}" /> and returns its value, or throws an exception if the cast is invalid.
        /// </summary>
        /// <typeparam name="T"> The type of the value to return. </typeparam>
        /// <exception cref="System.InvalidCastException">This WZ object is not a
        ///   <see cref="WZProperty{T}" />
        ///   .</exception>
        /// <returns> The value enclosed by this WZ property. </returns>
        public T ValueOrDie<T>()
        {
            return ((WZProperty<T>)this).Value;
        }

        /// <summary>
        ///   Tries to cast this to a <see cref="WZProperty{T}" /> and returns its value, or returns a default value if the cast is invalid.
        /// </summary>
        /// <param name="default"> The value to return if the cast is unsuccessful. </param>
        /// <typeparam name="T"> The type of the value to return. </typeparam>
        /// <returns> The value enclosed by this WZ property, or the default value. </returns>
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
                            ret.Add(r.PeekFor(() => ParseExtendedProperty(name, r, parent, f, encrypted)));
                            r.Skip(blockLen);
                            break;
                        default:
                            throw new Exception("Unknown property type at ParsePropertyList");
                    }
                }
                return ret;
            }
        }

        internal static WZObject ParseExtendedProperty(string name, WZBinaryReader r, WZObject parent, WZImage f, bool encrypted)
        {
            lock (f.File) {
                string type = r.ReadWZStringBlock(encrypted);
                switch (type) {
                    case "Property":
                        r.Skip(2);
                        return new WZSubProperty(name, parent, r, f);
                    case "Canvas":
                        return new WZCanvasProperty(name, parent, r, f);
                    case "Shape2D#Vector2D":
                        return new WZVectorProperty(name, parent, r, f);
                    case "Shape2D#Convex2D":
                        return new WZConvexProperty(name, parent, r, f);
                    case "Sound_DX8":
                        return new WZMP3Property(name, parent, r, f);
                    case "UOL":
                        // TODO: resolve UOLs
                        r.Skip(1);
                        return new WZUOLProperty(name, parent, r, f);
                    default:
                        return WZFile.Die<WZObject>(String.Format("Unknown ExtendedProperty type \"{0}\"", type));
                }
            }
        }
    }
}