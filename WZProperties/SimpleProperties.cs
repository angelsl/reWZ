using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace reWZ.WZProperties
{
    public class WZNullProperty : WZProperty<object>
    {
        public WZNullProperty(string name, WZObject parent, WZFile container) : base(name, parent, null, container)
        {}
    }

    public class WZUInt16Property : WZProperty<ushort>
    {
        public WZUInt16Property(string name, WZObject parent, ushort value, WZFile container) : base(name, parent, value, container)
        {}
    }

    public class WZInt32Property : WZProperty<int>
    {
        public WZInt32Property(string name, WZObject parent, int value, WZFile container) : base(name, parent, value, container)
        {}
    }

    public class WZSingleProperty : WZProperty<Single>
    {
        public WZSingleProperty(string name, WZObject parent, float value, WZFile container) : base(name, parent, value, container)
        {}
    }

    public class WZDoubleProperty : WZProperty<Double>
    {
        public WZDoubleProperty(string name, WZObject parent, double value, WZFile container) : base(name, parent, value, container)
        {}
    }

    public class WZStringProperty : WZProperty<String>
    {
        public WZStringProperty(string name, WZObject parent, string value, WZFile container) : base(name, parent, value, container)
        {}
    }

    public class WZUOLProperty : WZProperty<String>
    {
        public WZUOLProperty(string name, WZObject parent, string value, WZFile container) : base(name, parent, value, container)
        {}
    }
}
