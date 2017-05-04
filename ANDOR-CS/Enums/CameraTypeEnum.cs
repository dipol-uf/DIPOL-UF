using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    public enum CameraType : int
    {
        
        PDA = (int)SDK.AC_CAMERATYPE_PDA,
        iXon = (int)SDK.AC_CAMERATYPE_IXON,
        ICCD = (int)SDK.AC_CAMERATYPE_ICCD,
        EMCCD = (int)SDK.AC_CAMERATYPE_EMCCD,
        CCD = (int)SDK.AC_CAMERATYPE_CCD,
        iStar = (int)SDK.AC_CAMERATYPE_ISTAR,
        ThirdPartyVideo = (int)SDK.AC_CAMERATYPE_VIDEO,
        iDus = (int)SDK.AC_CAMERATYPE_IDUS,
        Newton = (int)SDK.AC_CAMERATYPE_NEWTON,
        Surcam = (int)SDK.AC_CAMERATYPE_SURCAM,
        USBICCD = (int)SDK.AC_CAMERATYPE_USBICCD,
        Luca = (int)SDK.AC_CAMERATYPE_LUCA,
        Reserved = (int)SDK.AC_CAMERATYPE_RESERVED,
        iKon = (int)SDK.AC_CAMERATYPE_IKON,
        InGaAs = (int)SDK.AC_CAMERATYPE_INGAAS,
        iVac = (int)SDK.AC_CAMERATYPE_IVAC,
        Clara = (int)SDK.AC_CAMERATYPE_CLARA,
        USBiStar = (int)SDK.AC_CAMERATYPE_USBISTAR,
        iXonUltra = (int)SDK.AC_CAMERATYPE_IXONULTRA
    }
}
