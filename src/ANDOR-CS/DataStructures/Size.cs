﻿//    This file is part of Dipol-3 Camera Manager.

//    Dipol-3 Camera Manager is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    Dipol-3 Camera Manager is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with Dipol-3 Camera Manager.  If not, see<http://www.gnu.org/licenses/>.
//
//    Copyright 2017, Ilia Kosenkov, Tuorla Observatory, Finland

using System;
using System.Runtime.Serialization;

namespace ANDOR_CS.DataStructures
{
    [DataContract]
    public struct Size
    {
        [DataMember(IsRequired = true)]
        public int Horizontal
        {
            get;
            set;
        }
        [DataMember(IsRequired = true)]
        public int Vertical
        {
            get;
            set;
        }

        public Size(int width, int height)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException($"{nameof(width)} should be greater than or equal to {0} ({width} >= {0})");

            if (height < 0)
                throw new ArgumentOutOfRangeException($"{nameof(height)} should be greater than or equal to {0} ({height} >= {0})");

            Horizontal = width;
            Vertical = height;
        }

        public override string ToString()
        {
            return $"({Horizontal}, {Vertical})";
        }
    }
}
