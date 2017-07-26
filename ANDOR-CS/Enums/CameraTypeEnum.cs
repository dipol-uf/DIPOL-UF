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

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [DataContract]
    public enum CameraType : uint
    {

        [EnumMember]
        PDA = SDK.AC_CAMERATYPE_PDA,
        [EnumMember]
        iXon = SDK.AC_CAMERATYPE_IXON,
        [EnumMember]
        ICCD = SDK.AC_CAMERATYPE_ICCD,
        [EnumMember]
        EMCCD = SDK.AC_CAMERATYPE_EMCCD,
        [EnumMember]
        CCD = SDK.AC_CAMERATYPE_CCD,
        [EnumMember]
        iStar = SDK.AC_CAMERATYPE_ISTAR,
        [EnumMember]
        ThirdPartyVideo = SDK.AC_CAMERATYPE_VIDEO,
        [EnumMember]
        iDus = SDK.AC_CAMERATYPE_IDUS,
        [EnumMember]
        Newton = SDK.AC_CAMERATYPE_NEWTON,
        [EnumMember]
        Surcam = SDK.AC_CAMERATYPE_SURCAM,
        [EnumMember]
        USBICCD = SDK.AC_CAMERATYPE_USBICCD,
        [EnumMember]
        Luca = SDK.AC_CAMERATYPE_LUCA,
        [EnumMember]
        Reserved = SDK.AC_CAMERATYPE_RESERVED,
        [EnumMember]
        iKon = SDK.AC_CAMERATYPE_IKON,
        [EnumMember]
        InGaAs = SDK.AC_CAMERATYPE_INGAAS,
        [EnumMember]
        iVac = SDK.AC_CAMERATYPE_IVAC,
        [EnumMember]
        Clara = SDK.AC_CAMERATYPE_CLARA,
        [EnumMember]
        USBiStar = SDK.AC_CAMERATYPE_USBISTAR,
        [EnumMember]
        iXonUltra = SDK.AC_CAMERATYPE_IXONULTRA,
        [EnumMember]
        iVacCCD = SDK.AC_CAMERATYPE_IVAC_CCD,
        [EnumMember]
        iKonXL = SDK.AC_CAMERATYPE_IKONXL,
        [EnumMember]
        iStarSCMOS = SDK.AC_CAMERATYPE_ISTAR_SCMOS,
        [EnumMember]
        iKonLR = 31 //SDK.AC_CAMERATYPE_IKONLR
    }
}
