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

        public bool Checked
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public CameraListItem(int index, bool check, string name)
        {
            Index = index;
            Checked = check;
            Name = name;
        }
    }
}
