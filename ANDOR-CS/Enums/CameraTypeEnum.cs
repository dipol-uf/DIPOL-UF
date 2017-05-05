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
