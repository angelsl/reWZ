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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using reWZ.WZProperties;

namespace reWZ
{
    /// <summary>
    ///   An abstract class representing a WZ property that contains a value of type <typeparamref name="T" /> .
    /// </summary>
    /// <typeparam name="T"> The type that this property contains. </typeparam>
    public abstract class WZProperty<T> : WZObject
    {
        private readonly WZImage _image;
        private long _offset;
        private bool _parsed;
        private WZBinaryReader _reader;
        private T _value;

        internal WZProperty(string name, WZObject parent, T value, WZImage container, bool children) : base(name, parent, container.File, children)
        {
            _value = value;
            _image = container;
            _parsed = true;
            _reader = null;
        }

        internal WZProperty(string name, WZObject parent, WZBinaryReader r, WZImage container, bool children) : base(name, parent, container.File, children)
        {
            _image = container;
            _reader = r;
            _offset = r.BaseStream.Position;
            if (File._parseAll) {
                _value = Parse(r, false);
                _parsed = true;
            } else {
                _value = default(T);
                Parse(r, true);
            }
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

        internal virtual T Parse(WZBinaryReader r, bool initial)
        {
            throw new NotSupportedException("This is not supposed to happen.");
        }
    }

    /// <summary>
    ///   An object in a WZ file.
    /// </summary>
    public abstract class WZObject : IEnumerable<WZObject>, IDictionary<String, WZObject>
    {
        private readonly Dictionary<String, WZObject> _backing;
        private readonly bool _canContainChildren;
        private readonly WZFile _file;
        private readonly string _name;
        private readonly WZObject _parent;
        private string _path;

        internal WZObject(string name, WZObject parent, WZFile container, bool children)
        {
            _name = name;
            _parent = parent;
            _path = null;
            _file = container;
            _canContainChildren = children;
            if (_canContainChildren) _backing = new Dictionary<string, WZObject>();
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
            get { return _path ?? ConstructPath(); }
        }

        /// <summary>
        ///   The WZ file containing this object.
        /// </summary>
        public WZFile File
        {
            get { return _file; }
        }

        /// <summary>
        ///   Returns the child with the name <paramref name="childName" /> .
        /// </summary>
        /// <param name="childName"> The name of the child to return. </param>
        /// <returns> The retrieved child. </returns>
        public virtual WZObject this[string childName]
        {
            get
            {
                if (!_canContainChildren) throw new NotSupportedException("This WZObject cannot contain children.");
                if (!_backing.ContainsKey(childName)) throw new KeyNotFoundException("No such child in WZDirectory.");
                return _backing[childName];
            }
        }

        #region IDictionary<string,WZObject> Members

        /// <summary>
        ///   Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key.
        /// </summary>
        /// <returns> true if the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the key; otherwise, false. </returns>
        /// <param name="key"> The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2" /> . </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="key" />
        ///   is null.</exception>
        public bool ContainsKey(string key)
        {
            ChildrenCheck();
            return _backing.ContainsKey(key);
        }

        /// <summary>
        ///   Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2" /> .
        /// </summary>
        /// <param name="key"> The object to use as the key of the element to add. </param>
        /// <param name="value"> The object to use as the value of the element to add. </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="key" />
        ///   is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the
        ///   <see cref="T:System.Collections.Generic.IDictionary`2" />
        ///   .</exception>
        /// <exception cref="T:System.NotSupportedException">The
        ///   <see cref="T:System.Collections.Generic.IDictionary`2" />
        ///   is read-only.</exception>
        public void Add(string key, WZObject value)
        {
            throw new NotSupportedException("You cannot modify a WZ property");
        }

        /// <summary>
        ///   Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2" /> .
        /// </summary>
        /// <returns> true if the element is successfully removed; otherwise, false. This method also returns false if <paramref
        ///    name="key" /> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2" /> . </returns>
        /// <param name="key"> The key of the element to remove. </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="key" />
        ///   is null.</exception>
        /// <exception cref="T:System.NotSupportedException">The
        ///   <see cref="T:System.Collections.Generic.IDictionary`2" />
        ///   is read-only.</exception>
        public bool Remove(string key)
        {
            throw new NotSupportedException("You cannot modify a WZ property");
        }

        /// <summary>
        ///   Gets the value associated with the specified key.
        /// </summary>
        /// <returns> true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key; otherwise, false. </returns>
        /// <param name="key"> The key whose value to get. </param>
        /// <param name="value"> When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref
        ///    name="value" /> parameter. This parameter is passed uninitialized. </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="key" />
        ///   is null.</exception>
        public bool TryGetValue(string key, out WZObject value)
        {
            ChildrenCheck();
            return _backing.TryGetValue(key, out value);
        }

        /// <summary>
        ///   Gets or sets the element with the specified key.
        /// </summary>
        /// <returns> The element with the specified key. </returns>
        /// <param name="key"> The key of the element to get or set. </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="key" />
        ///   is null.</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and
        ///   <paramref name="key" />
        ///   is not found.</exception>
        /// <exception cref="T:System.NotSupportedException">The property is set and the
        ///   <see cref="T:System.Collections.Generic.IDictionary`2" />
        ///   is read-only.</exception>
        WZObject IDictionary<string, WZObject>.this[string key]
        {
            get { return this[key]; }
            set { throw new NotSupportedException("You cannot modify a WZ property"); }
        }

        /// <summary>
        ///   Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the <see
        ///    cref="T:System.Collections.Generic.IDictionary`2" /> .
        /// </summary>
        /// <returns> An <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the object that implements <see
        ///    cref="T:System.Collections.Generic.IDictionary`2" /> . </returns>
        public ICollection<string> Keys
        {
            get
            {
                ChildrenCheck();
                return _backing.Keys;
            }
        }

        /// <summary>
        ///   Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the <see
        ///    cref="T:System.Collections.Generic.IDictionary`2" /> .
        /// </summary>
        /// <returns> An <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the object that implements <see
        ///    cref="T:System.Collections.Generic.IDictionary`2" /> . </returns>
        public ICollection<WZObject> Values
        {
            get
            {
                ChildrenCheck();
                return _backing.Values;
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the dictionary.
        /// </summary>
        /// <returns> A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the dictionary. </returns>
        IEnumerator<KeyValuePair<string, WZObject>> IEnumerable<KeyValuePair<string, WZObject>>.GetEnumerator()
        {
            ChildrenCheck();
            return _backing.GetEnumerator();
        }

        /// <summary>
        ///   Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" /> .
        /// </summary>
        /// <param name="item"> The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" /> . </param>
        /// <exception cref="T:System.NotSupportedException">The
        ///   <see cref="T:System.Collections.Generic.ICollection`1" />
        ///   is read-only.</exception>
        public void Add(KeyValuePair<string, WZObject> item)
        {
            throw new NotSupportedException("You cannot modify a WZ property");
        }

        /// <summary>
        ///   Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" /> .
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The
        ///   <see cref="T:System.Collections.Generic.ICollection`1" />
        ///   is read-only.</exception>
        public void Clear()
        {
            throw new NotSupportedException("You cannot modify a WZ property");
        }

        /// <summary>
        ///   Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <returns> true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" /> ; otherwise, false. </returns>
        /// <param name="item"> The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" /> . </param>
        public bool Contains(KeyValuePair<string, WZObject> item)
        {
            return _canContainChildren && _backing.Contains(item);
        }

        /// <summary>
        ///   Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" /> , starting at a particular <see
        ///    cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array"> The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see
        ///    cref="T:System.Collections.Generic.ICollection`1" /> . The <see cref="T:System.Array" /> must have zero-based indexing. </param>
        /// <param name="arrayIndex"> The zero-based index in <paramref name="array" /> at which copying begins. </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="array" />
        ///   is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="arrayIndex" />
        ///   is less than 0.</exception>
        /// <exception cref="T:System.ArgumentException">
        ///   <paramref name="array" />
        ///   is multidimensional.-or-The number of elements in the source
        ///   <see cref="T:System.Collections.Generic.ICollection`1" />
        ///   is greater than the available space from
        ///   <paramref name="arrayIndex" />
        ///   to the end of the destination
        ///   <paramref name="array" />
        ///   .</exception>
        public void CopyTo(KeyValuePair<string, WZObject>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, WZObject>>)_backing).CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///   Throws a NotSupportedException.
        /// </summary>
        /// <returns> Does not return. </returns>
        /// <param name="item"> Does nothing. </param>
        /// <exception cref="T:System.NotSupportedException">This method is called.</exception>
        public bool Remove(KeyValuePair<string, WZObject> item)
        {
            throw new NotSupportedException("You cannot modify a WZ property.");
        }

        /// <summary>
        ///   Gets the number of children contained in this property.
        /// </summary>
        /// <returns> The number of children contained in this property. </returns>
        public int Count
        {
            get
            {
                if (!_canContainChildren) return 0;
                return _backing.Count;
            }
        }

        /// <summary>
        ///   Returns true.
        /// </summary>
        /// <returns> true </returns>
        public bool IsReadOnly
        {
            get { return true; }
        }

        #endregion

        #region IEnumerable<WZObject> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the children in this property.
        /// </summary>
        /// <returns> A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the children in this property. </returns>
        public IEnumerator<WZObject> GetEnumerator()
        {
            ChildrenCheck();
            return _backing.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            ChildrenCheck();
            return GetEnumerator();
        }

        #endregion

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

        /// <summary>
        ///   Resolves a path in the form "/a/b/c/.././d/e/f/".
        /// </summary>
        /// <param name="path"> The path to resolve. </param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The path has an invalid node.</exception>
        /// <returns> The object located at the path. </returns>
        public WZObject ResolvePath(string path)
        {
            return (path.StartsWith("/") ? path.Substring(1) : path).Split('/').Where(node => node != ".").Aggregate(this, (current, node) => node == ".." ? current.Parent : current[node]);
        }

        internal void Add(WZObject o)
        {
            ChildrenCheck();
            _backing.Add(o.Name, o);
        }

        private string ConstructPath()
        {
            StringBuilder s = new StringBuilder(_name);
            WZObject p = this;
            while ((p = p.Parent) != null)
                s.Insert(0, "/").Insert(0, p.Name);
            _path = s.ToString();
            return _path;
        }

        private void ChildrenCheck()
        {
            if (!_canContainChildren) throw new NotSupportedException("This WZObject cannot contain children.");
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
                        return new WZPointProperty(name, parent, r, f);
                    case "Shape2D#Convex2D":
                        return new WZConvexProperty(name, parent, r, f);
                    case "Sound_DX8":
                        return new WZMP3Property(name, parent, r, f);
                    case "UOL":
                        r.Skip(1);
                        return new WZUOLProperty(name, parent, r, f);
                    default:
                        return WZFile.Die<WZObject>(String.Format("Unknown ExtendedProperty type \"{0}\"", type));
                }
            }
        }
    }
}