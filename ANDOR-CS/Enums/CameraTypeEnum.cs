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

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    public enum CameraType : uint
    {
        
        PDA = SDK.AC_CAMERATYPE_PDA,
        iXon = SDK.AC_CAMERATYPE_IXON,
        ICCD = SDK.AC_CAMERATYPE_ICCD,
        EMCCD = SDK.AC_CAMERATYPE_EMCCD,
        CCD = SDK.AC_CAMERATYPE_CCD,
        iStar = SDK.AC_CAMERATYPE_ISTAR,
        ThirdPartyVideo = SDK.AC_CAMERATYPE_VIDEO,
        iDus = SDK.AC_CAMERATYPE_IDUS,
        Newton = SDK.AC_CAMERATYPE_NEWTON,
        Surcam = SDK.AC_CAMERATYPE_SURCAM,
        USBICCD = SDK.AC_CAMERATYPE_USBICCD,
        Luca = SDK.AC_CAMERATYPE_LUCA,
        Reserved = SDK.AC_CAMERATYPE_RESERVED,
        iKon = SDK.AC_CAMERATYPE_IKON,
        InGaAs = SDK.AC_CAMERATYPE_INGAAS,
        iVac = SDK.AC_CAMERATYPE_IVAC,
        Clara = SDK.AC_CAMERATYPE_CLARA,
        USBiStar = SDK.AC_CAMERATYPE_USBISTAR,
        iXonUltra = SDK.AC_CAMERATYPE_IXONULTRA,
        iVacCCD = SDK.AC_CAMERATYPE_IVAC_CCD,
        iKonXL = SDK.AC_CAMERATYPE_IKONXL,
        iStarSCMOS = SDK.AC_CAMERATYPE_ISTAR_SCMOS,
        iKonLR = 31 //SDK.AC_CAMERATYPE_IKONLR
    }
}
