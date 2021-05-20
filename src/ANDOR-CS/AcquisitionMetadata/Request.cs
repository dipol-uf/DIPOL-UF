//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2020 Ilia Kosenkov
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
#nullable enable
#pragma warning disable 1591

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ANDOR_CS.Enums;
using FITS_CS;

namespace ANDOR_CS.AcquisitionMetadata
{
    [DataContract]
    public class Request
    {
        [DataMember]
        public List<FitsKey> FitsKeys { get; private set; }

        [DataMember]
        public ImageFormat ImageFormat { get; private set; }
        
        [DataMember]
        public FrameType FrameType { get; private set; }

        [DataMember]
        public bool IsSkyflat { get; private set; }
        public Request(
            ImageFormat imageFormat = ImageFormat.UnsignedInt16,
            FrameType frameType = FrameType.Light,
            bool isSkyflat = false,
            IEnumerable<FitsKey>? keys = default)
        {
            FitsKeys = keys?.ToList() ?? new List<FitsKey>();
            FrameType = frameType;
            ImageFormat = imageFormat;
            IsSkyflat = isSkyflat;
        }

        public Request WithNewKeywords(IEnumerable<FitsKey> newKeys)
        {
            if(FitsKeys?.Count == 0 && newKeys is null)
                return new Request(ImageFormat, FrameType);

            var keys = new List<FitsKey>();

            if(FitsKeys?.Any() == true)
                keys.AddRange(FitsKeys);
            if(newKeys is {})
                keys.AddRange(newKeys);
              
            return new Request(ImageFormat, FrameType, IsSkyflat, keys);
        }

    }
}
