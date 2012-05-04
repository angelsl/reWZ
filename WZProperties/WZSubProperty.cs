using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace reWZ.WZProperties
{
    public class WZSubProperty : WZProperty<WZNothing>
    {
        public WZSubProperty(string name, WZObject parent, WZImage container, WZBinaryReader r) : base(name, parent, default(WZNothing), container)
        {
            WZExtendedParser.ParsePropertyList(r, this, container, container._encrypted).ForEach(Add);
        }
    }
}
