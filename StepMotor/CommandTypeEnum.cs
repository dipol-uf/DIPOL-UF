﻿//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

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
