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
using ANDOR_CS.Classes;

namespace ANDOR_CS.Exceptions
{
    /// <inheritdoc />
    public class AcquisitionInProgressException : Exception
    {
        /// <inheritdoc />
        public AcquisitionInProgressException(string message) :
            base(message)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cam"></param>
        /// <exception cref="AcquisitionInProgressException"></exception>
        [Obsolete]
        public static void ThrowIfAcquiring(Camera cam) 
            
        {
            if (cam.IsAcquiring)
                throw new AcquisitionInProgressException("Camera is acquiring image(s) at the moment.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="except"></param>
        /// <returns></returns>
        public static bool FailIfAcquiring(Camera cam, out Exception except)
        {
            except = null;

            if (cam.IsAcquiring)
            {
                except = new AcquisitionInProgressException("Camera is acquiring image(s) at the moment.");
                return true;
            }

            return false;


        }

    }
}
