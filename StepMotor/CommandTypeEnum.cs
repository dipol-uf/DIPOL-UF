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

namespace StepMotor
{
    [DataContract]
    public enum CommandType : byte
    {
        [EnumMember]
        Unused = 0,

        [EnumMember]
        Absolute = 0,
        [EnumMember]
        Relative = 1,
        [EnumMember]
        Coordinates = 2,

        [EnumMember]
        Start = 0,
        [EnumMember]
        Stop = 1,
        [EnumMember]
        Status = 2,

        [EnumMember]
        Add = 0,
        [EnumMember]
        Subtract = 1,
        [EnumMember]
        Multiply = 2,
        [EnumMember]
        Divide = 3,
        [EnumMember]
        Modulo = 4,
        [EnumMember]
        And = 5,
        [EnumMember]
        Or = 6,
        [EnumMember]
        Xor = 7,
        [EnumMember]
        Not = 8,
        [EnumMember]
        Load = 9,
        [EnumMember]
        Swap = 10,

        [EnumMember]
        AllFlags = 0,
        [EnumMember]
        TimeoutFlag = 1,
        [EnumMember]
        AlarmFlag = 2,
        [EnumMember]
        DeviationFlag = 3,
        [EnumMember]
        PositionFlag = 4,
        [EnumMember]
        ShutdownFlag = 5
    }
}
