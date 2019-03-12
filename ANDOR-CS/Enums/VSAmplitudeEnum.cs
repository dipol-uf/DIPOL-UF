//    This file is part of Dipol-3 Camera Manager.

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
using System.ComponentModel;

namespace ANDOR_CS.Enums
{
    /// <summary>
    /// Available vertical clock voltage amplitudes to set. 
    /// Not all camera support this feature, not all amplitudes may be available.
    /// </summary>
    [DataContract]
    public enum VSAmplitude
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
