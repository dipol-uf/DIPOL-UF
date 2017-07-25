using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.ServiceModel;

namespace DIPOL_Remote.Interfaces
{
    public interface IRemoteCallback
    {
        [OperationContract(IsOneWay = true)]
        void SendToClient(string m);

        [OperationContract(IsOneWay = true)]
        void NotifyRemotePropertyChanged(int camIndex, string session, string proeprty);
    }
}
