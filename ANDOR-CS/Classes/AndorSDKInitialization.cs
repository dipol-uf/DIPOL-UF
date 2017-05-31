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

namespace ANDOR_CS.Classes
{
    /// <summary>
    /// Holds a singleton instance of AndorSDK class that is used across applications.
    /// Thread-safety is unknown.
    /// </summary>
    public static class AndorSDKInitialization
    {

        private static Random R = new Random();
        private static ATMCD64CS.AndorSDK _SDKInstance = new ATMCD64CS.AndorSDK();

        private static System.Threading.SemaphoreSlim semaphore = new System.Threading.SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets an singleton instance of a basic AndorSDK class
        /// </summary>
        public static ATMCD64CS.AndorSDK SDKInstance
        {
            get
            {
                byte[] arr = new byte[4];

                R.NextBytes(arr);

                int handle = BitConverter.ToInt32(arr, 0);

                bool entered = false;

                try
                {
                    semaphore.Wait();
                    entered = true;
                    //Console.ForegroundColor = ConsoleColor.DarkGreen;
                    //Console.WriteLine($"Semaphore entered. ({handle.ToString("X8")})");
                    //Console.ForegroundColor = ConsoleColor.White;

                    return _SDKInstance;
                }
                finally
                {
                    if (entered)
                    {
                        semaphore.Release();
                        entered = true;
                        //Console.ForegroundColor = ConsoleColor.DarkGreen;
                        //Console.WriteLine($"Semaphore left.    ({handle.ToString("X8")})");
                        //Console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        //Console.ForegroundColor = ConsoleColor.Red;
                        //Console.WriteLine($"Semaphore was not entered!. ({handle.ToString("X8")})");
                        //Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }
            
        } 
    }
}
