using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    /// <summary>
    /// Holds a singleton instance of AndorSDK class that is used across applications.
    /// Thread-safety is unknown.
    /// </summary>
    public static class AndorSDKInitialization
    {
        /// <summary>
        /// Gets an singleton instance of a basic AndorSDK class
        /// </summary>
        public static ATMCD64CS.AndorSDK SDKInstance
        {
            get;
            private set;
        } = new ATMCD64CS.AndorSDK();
    }
}
