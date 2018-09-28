//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
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


using System;

#if X86
using AndorSDK = ATMCD32CS.AndorSDK;
#endif
#if X64
using AndorSDK = ATMCD64CS.AndorSDK;
#endif

namespace ANDOR_CS.Classes
{
    /// <summary>
    /// Holds a singleton instance of AndorSDK class that is used across applications.
    /// Thread-safety is unknown.
    /// </summary>
    public static class AndorSdkInitialization
    {

        //private static ATMCD64CS.AndorSDK _SDKInstance

       // private static System.Threading.SemaphoreSlim semaphore = new System.Threading.SemaphoreSlim(1, 1);
        private static volatile System.Threading.SemaphoreSlim _locker = new System.Threading.SemaphoreSlim(1, 1);
       // private static volatile int LockDepth = 0;

        /// <summary>
        /// Gets an singleton instance of a basic AndorSDK class
        /// </summary>
        public static AndorSDK SDKInstance
        {
            get;
        } = new AndorSDK();


        public delegate uint AndorSdk<T1>(ref T1 p1);
        public delegate uint AndorSdk<in T1, T2>(T1 p1, ref T2 p2);
        public delegate uint AndorSdk<in T1, in T2, T3>(T1 p1, T2 p2, ref T3 p3);
        //public delegate uint AndorSDK<T1, T2, T3>(T1 p1, ref T2 p2, ref T3 p3);

        /// <summary>
        /// Task-safely invokes SDK method with one output ref parameter
        /// </summary>
        /// <typeparam name="T1">Type of first parameter</typeparam>
        /// <param name="method"><see cref="SDKInstance"/> method to invoke</param>
        /// <param name="p1">Stores result of the function call</param>
        /// <returns>Return code</returns>
        public static uint Call<T1>(SafeSdkCameraHandle handle, AndorSdk<T1> method, out T1 p1)
        {
            // Stores return code

            try
            {
                p1 = default(T1);
                // Waits until SDKInstance is available
                _locker.Wait();
                SetActiveCamera(handle);
                

                // Calls function
                var result = method(ref p1);
                return result;
            }
            finally
            {
                // Releases semaphore
                _locker.Release();
            }
            
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
        public static uint Call<T1, T2>(SafeSdkCameraHandle handle, AndorSdk<T1, T2> method, T1 p1, out T2 p2)
        {
            // Stores return code
            p2 = default(T2);

            try
            {
                // Waits until SDKInstance is available
                _locker.Wait();
                SetActiveCamera(handle);
                // Calls function
                var result = method(p1, ref p2);

                return result;
            }
            finally
            {
                // Releases semaphore
                _locker.Release();
            }
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
        public static uint Call<T1, T2, T3>(SafeSdkCameraHandle handle, AndorSdk<T1, T2, T3> method, T1 p1, T2 p2, out T3 p3)
        {
            // Stores return code
            p3 = default(T3);

            try
            {
                // Waits until SDKInstance is available
                _locker.Wait();
                SetActiveCamera(handle);
                // Calls function
                var result = method(p1, p2, ref p3);

                return result;
            }

            finally
            {
                // Releases semaphore
                _locker.Release();
            }
        }

        public static uint Call<T1>(SafeSdkCameraHandle handle, Func<T1, uint> method, T1 p1)
        {
            // Stores return code


            try
            {// Waits until SDKInstance is available
                _locker.Wait();

                // Calls function
                SetActiveCamera(handle);
                var result = method(p1);

                return result;
            }
            finally
            {// Releases semaphore
                _locker.Release();
            }

            
        }

        public static uint Call(SafeSdkCameraHandle handle, Func<uint> method)
        {

            // Stores return code

            try
            {
                // Waits until SDKInstance is available
                _locker.Wait();
                SetActiveCamera(handle);
                // Calls function
                var result = method();
                return result;
            }
            finally
            {
                // Releases semaphore
                _locker.Release();

            }            

        }

        public static uint CallWithoutHandle<T1, T2>(AndorSdk<T1, T2> method, T1 p1, out T2 p2)
        {
            // Stores return code
            p2 = default(T2);
            try
            {// Waits until SDKInstance is available
                _locker.Wait();

                // Calls function
                var result = method(p1, ref p2);

                return result;
            }
            finally
            {// Releases semaphore
                _locker.Release();
            }
        }
        public static uint CallWithoutHandle<T1>(AndorSdk<T1> method, out T1 p1)
        {
            // Stores return code
            p1 = default(T1);
            try
            {// Waits until SDKInstance is available
                _locker.Wait();

                // Calls function
                var result = method(ref p1);

                return result;
            }
            finally
            {// Releases semaphore
                _locker.Release();
            }
        }

        public static uint CallWithoutHandle(Func<uint> method)
        {
            // Stores return code
            try
            {// Waits until SDKInstance is available
                _locker.Wait();

                // Calls function
                var result = method();

                return result;
            }
            finally
            {// Releases semaphore
                _locker.Release();
            }
        }


        private static void SetActiveCamera(SafeSdkCameraHandle handle)
        {
            if (handle == null) return;

            var currHandle = 0;
            if (SDKInstance.GetCurrentCamera(ref currHandle) != AndorSDK.DRV_SUCCESS)
                throw new Exception();

            if (currHandle == handle.SdkPtr) return;

            if (SDKInstance.SetCurrentCamera(handle.SdkPtr) != AndorSDK.DRV_SUCCESS)
                throw new Exception();
        }
        

    }
}
