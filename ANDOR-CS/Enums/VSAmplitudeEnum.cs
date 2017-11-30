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

using System.Runtime.Serialization;
using System.ComponentModel;

namespace ANDOR_CS.Enums
{
    /// <summary>
    /// Available vertical clock voltage amplitudes to set. 
    /// Not all camera support this feature, not all amplitudes may be available.
    /// </summary>
    [DataContract]
    public enum VSAmplitude : int
    {
        /// <summary>
        /// 0, default
        /// </summary>
        [Description("Normal")]
        [EnumMember]
        Normal = 0,

        /// <summary>
        /// +1
        /// </summary>
        [Description("+1")]
        [EnumMember]
        Plus1 = 1,

        /// <summary>
        /// +2
        /// </summary>
        [Description("+2")]
        [EnumMember]
        Plus2 = 2,

        /// <summary>
        /// +3
        /// </summary>
        [Description("+3")]
        [EnumMember]
        Plus3 = 3,

        /// <summary>
        /// +4
        /// </summary>
        [Description("+4")]
        [EnumMember]
        Plus4 = 4
    }
}
