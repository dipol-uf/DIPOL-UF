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

namespace ANDOR_CS.Enums
{
    /// <summary>
    /// Represents function of an object that can be in either of two regimes:
    /// Enabled or Disabled
    /// </summary>
    [DataContract]
    public enum Switch : uint
    {
        /// <summary>
        /// Funcion is enabled/turned on
        /// </summary>
        [EnumMember]
        Enabled = 1,
        
        /// <summary>
        /// Function is disabled/turned of
        /// </summary>
        [EnumMember]
        Disabled = 0
    }
}
