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

#if X86
using SDK = ATMCD32CS.AndorSDK;
#endif
#if X64
using SDK = ATMCD64CS.AndorSDK;
#endif

namespace ANDOR_CS.Enums
{
    [DataContract]
    public enum CameraType : uint
    {

        [EnumMember]
        Pda = SDK.AC_CAMERATYPE_PDA,
        [EnumMember]
        IXon = SDK.AC_CAMERATYPE_IXON,
        [EnumMember]
        Iccd = SDK.AC_CAMERATYPE_ICCD,
        [EnumMember]
        Emccd = SDK.AC_CAMERATYPE_EMCCD,
        [EnumMember]
        Ccd = SDK.AC_CAMERATYPE_CCD,
        [EnumMember]
        IStar = SDK.AC_CAMERATYPE_ISTAR,
        [EnumMember]
        ThirdPartyVideo = SDK.AC_CAMERATYPE_VIDEO,
        [EnumMember]
        IDus = SDK.AC_CAMERATYPE_IDUS,
        [EnumMember]
        Newton = SDK.AC_CAMERATYPE_NEWTON,
        [EnumMember]
        Surcam = SDK.AC_CAMERATYPE_SURCAM,
        [EnumMember]
        Usbiccd = SDK.AC_CAMERATYPE_USBICCD,
        [EnumMember]
        Luca = SDK.AC_CAMERATYPE_LUCA,
        [EnumMember]
        Reserved = SDK.AC_CAMERATYPE_RESERVED,
        [EnumMember]
        IKon = SDK.AC_CAMERATYPE_IKON,
        [EnumMember]
        InGaAs = SDK.AC_CAMERATYPE_INGAAS,
        [EnumMember]
        IVac = SDK.AC_CAMERATYPE_IVAC,
        [EnumMember]
        Clara = SDK.AC_CAMERATYPE_CLARA,
        [EnumMember]
        UsBiStar = SDK.AC_CAMERATYPE_USBISTAR,
        [EnumMember]
        IXonUltra = SDK.AC_CAMERATYPE_IXONULTRA,
        [EnumMember]
        IVacCcd = SDK.AC_CAMERATYPE_IVAC_CCD,
        [EnumMember]
        IKonXl = SDK.AC_CAMERATYPE_IKONXL,
        [EnumMember]
        IStarScmos = SDK.AC_CAMERATYPE_ISTAR_SCMOS,
        [EnumMember]
        IKonLr = 31 //SDK.AC_CAMERATYPE_IKONLR
    }
}
