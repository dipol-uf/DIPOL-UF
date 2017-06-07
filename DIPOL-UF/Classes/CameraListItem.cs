using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIPOL_UF.Classes
{
    public class CameraListItem
    {
        public int Index
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public CameraListItem(int index, string name)
        {
            Index = index;
            Name = name;
        }
    }
}
