//    This file is part of Dipol-3 Camera Manager.

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.Enums
{
    /// <summary>
    /// Available vertical clock voltage amplitudes to set. 
    /// Not all camera support this feature, not all amplitudes may be available.
    /// </summary>
    public enum VSAmplitude : int
    {
        /// <summary>
        /// 0, default
        /// </summary>
        Normal = 0,

        /// <summary>
        /// +1
        /// </summary>
        Plus1 = 1,

        /// <summary>
        /// +2
        /// </summary>
        Plus2 = 2,

        /// <summary>
        /// +3
        /// </summary>
        Plus3 = 3,

        /// <summary>
        /// +4
        /// </summary>
        Plus4 = 4
    }
}
