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

        //private static ATMCD64CS.AndorSDK _SDKInstance

       // private static System.Threading.SemaphoreSlim semaphore = new System.Threading.SemaphoreSlim(1, 1);
        private static volatile System.Threading.SemaphoreSlim locker = new System.Threading.SemaphoreSlim(1, 1);
        private static volatile int LockDepth = 0;

        /// <summary>
        /// Gets an singleton instance of a basic AndorSDK class
        /// </summary>
        public static ATMCD64CS.AndorSDK SDKInstance
        {
            get;
            private set;

        } = new ATMCD64CS.AndorSDK();

        //public static void Lock() => locker.Wait();

        //public static void Release() => locker.Release();

        public delegate uint AndorSDK<T1>(ref T1 p1);
        public delegate uint AndorSDK<in T1, T2>(T1 p1, ref T2 p2);
        public delegate uint AndorSDK<in T1, in T2, T3>(T1 p1, T2 p2, ref T3 p3);
        //public delegate uint AndorSDK<T1, T2, T3>(T1 p1, ref T2 p2, ref T3 p3);

        /// <summary>
        /// Task-safely invokes SDK method with one output ref parameter
        /// </summary>
        /// <typeparam name="T1">Type of first parameter</typeparam>
        /// <param name="method"><see cref="SDKInstance"/> method to invoke</param>
        /// <param name="p1">Stores result of the function call</param>
        /// <returns>Return code</returns>
        public static uint Call<T1>(AndorSDK<T1> method, out T1 p1)
        {
            // Stores return code
            uint result = 0;

            try
            {
                p1 = default(T1);

                // Waits until SDKInstance is available
                locker.Wait();

                // Calls function
                result = method(ref p1);
            }
            finally
            {
                // Releases semaphore
                locker.Release();
            }
            return result;
        }

        /// <summary>
        /// Task-safely invokes SDK method with oneinput and one output ref parameter
        /// </summary>
        /// <typeparam name="T1">Type of first parameter</typeparam>
        /// <typeparam name="T2">Type of second parameter</typeparam>
        /// <param name="method"><see cref="SDKInstance"/> method to invoke</param>
        /// <param name="p1">Inpput argument of the method</param>
        /// <param name="p2">Stores result of the function call</param>
        /// <returns>Return code</returns>
        public static uint Call<T1, T2>(AndorSDK<T1, T2> method, T1 p1, out T2 p2)
        {
            // Stores return code
            uint result = 0;
            p2 = default(T2);
                
            // Waits until SDKInstance is available
            locker.Wait();

            // Calls function
            result = method(p1, ref p2);

            // Releases semaphore
            locker.Release();

            return result;
        }

        /// <summary>
        /// Task-safely invokes SDK method with oneinput and one output ref parameter
        /// </summary>
        /// <typeparam name="T1">Type of first parameter</typeparam>
        /// <typeparam name="T2">Type of second parameter</typeparam>
        /// <typeparam name="T3">Type of third parameter</typeparam>
        /// <param name="method"><see cref="SDKInstance"/> method to invoke</param>
        /// <param name="p1">First inpput argument of the method</param>
        /// <param name="p2">Second inpput argument of the method</param>
        /// <param name="p3">Stores result of the function call</param>
        /// <returns>Return code</returns>
        public static uint Call<T1, T2, T3>(AndorSDK<T1, T2, T3> method, T1 p1, T2 p2, out T3 p3)
        {
            // Stores return code
            uint result = 0;
            p3 = default(T3);
           
            // Waits until SDKInstance is available
            locker.Wait();

            // Calls function
            result = method(p1, p2, ref p3);

            // Releases semaphore
            locker.Release();

            return result;
        }

        public static uint Call<T1>(Func<T1, uint> method, T1 p1)
        {
            // Stores return code
            uint result = 0;


            // Waits until SDKInstance is available
            locker.Wait();

            // Calls function
            result = method(p1);

            // Releases semaphore
            locker.Release();

            return result;
        }

        public static uint Call(Func<uint> method)
        {

            // Stores return code
            uint result = 0;

            try
            {           
                // Waits until SDKInstance is available
                locker.Wait();

                // Calls function
                result = method();
                
            }
            finally
            {
                // Releases semaphore
                locker.Release();
                
            }
            return result;

        }


        ///// <summary>
        ///// Manually waits while other tasks access SDK instance
        ///// </summary>
        //internal static void LockManually()
        //{
        //    if (LockDepth++ == 0)
        //        locker.Wait();

        //}

        ///// <summary>
        ///// Manually releases semaphore and allows other tasks to call SDK functions
        ///// </summary>
        //internal static void ReleaseManually()
        //{
        //    if (--LockDepth == 0)
        //        locker.Release();
        //}

    }
}
