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

namespace StepMotor 
{
    /// <summary>
    /// A list of available commands
    /// </summary>
    [DataContract]
    public enum Command : byte
    {
        /// <summary>
        /// Unknown command
        /// </summary>
        [EnumMember]
        Unknown = 0,

        /// <summary>
        /// ROR: rotates right
        /// </summary>
        [EnumMember]
        RotateRight = 1,

        /// <summary>
        /// ROLL rotates left
        /// </summary>
        [EnumMember]
        RotateLeft = 2,

        /// <summary>
        /// MST: Motor stopps
        /// </summary>
        [EnumMember]
        MotorStop = 3,

        /// <summary>
        /// MVP: Moves to position
        /// </summary>
        [EnumMember]
        MoveToPosition = 4,

        /// <summary>
        /// SAP: Sets axis parameter
        /// </summary>
        [EnumMember]
        SetAxisParameter = 5,

        /// <summary>
        /// GAP: Gets axis paramter
        /// </summary>
        [EnumMember]
        GetAxisParameter = 6,

        /// <summary>
        /// STAP: Stores axis parameter
        /// </summary>
        [EnumMember]
        StoreAxisParameter = 7,

        /// <summary>
        /// RSAP: Restores axis parameter
        /// </summary>
        [EnumMember]
        RestoreAxisPArameter = 8,

        /// <summary>
        /// SGP: Sets global parameter
        /// </summary>
        [EnumMember]
        SetGlobalParameter = 9,

        /// <summary>
        /// GGP: Gets global parameter
        /// </summary>
        [EnumMember]
        GetGlobalParameter = 10,

        /// <summary>
        /// STGP: Stores global parameter
        /// </summary>
        [EnumMember]
        StoreGlobalParameter = 11,

        /// <summary>
        /// RSGP: Restores global parameter
        /// </summary>
        [EnumMember]
        RestoreGlobalParameter = 12,

        /// <summary>
        /// RFS: Rfreshes search
        /// </summary>
        [EnumMember]
        RefreshSearch = 13,

        /// <summary>
        /// SIO: Sets output
        /// </summary>
        [EnumMember]
        SetOutput = 14,

        /// <summary>
        /// GIO: Gets input/output
        /// </summary>
        [EnumMember]
        GetInputOutput = 15,

        /// <summary>
        /// CALC: Calculates
        /// </summary>
        [EnumMember]
        Calculate = 19,

        /// <summary>
        /// COMP: Compares
        /// </summary>
        [EnumMember]
        Compare = 20,

        /// <summary>
        /// SAC: Accesses SPI Bus
        /// </summary>
        [EnumMember]
        SPIBUsAccess = 29,

        /// <summary>
        /// SCO: Sets coordinate
        /// </summary>
        [EnumMember]
        SetCoordinate = 30,

        /// <summary>
        /// GCO: Gets coordinate
        /// </summary>
        [EnumMember]
        GetCoordinate = 31,

        /// <summary>
        /// CCO: Captures coordinate
        /// </summary>
        [EnumMember]
        CaptureCoordinate = 32,

        /// <summary>
        /// CALCX: Calculates using a register
        /// </summary>
        [EnumMember]
        CalculateWithRegister = 33,

        /// <summary>
        /// AAP: Accumulates to axis parameter
        /// </summary>
        [EnumMember]
        AccumulatorToAxisParameter = 34,

        /// <summary>
        /// AGP: Accumulates to global parameter
        /// </summary>
        [EnumMember]
        AccumulatorToGlovalParameter = 35,

        /// <summary>
        /// CLE: CLears error flags
        /// </summary>
        [EnumMember]
        ClearErrorFlags = 36
    }
}
