﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    internal class FitsKeyAttribute : Attribute
    {
        public string Header { get; }
        public string Comment { get; }
        public int? Index { get; }

        public FitsKeyAttribute(string header, string comment = "", int index = -1)
        {
            Header = header;
            Index = index == -1 ? null : (int?)index;
            Comment = comment;
        }
    }
}
