using System.Runtime.Serialization;

namespace ROTATOR_CS
{
    [DataContract]
    public enum AxisParameter : byte
    {
        [EnumMember]
        TargetPosition = 0,
        [EnumMember]
        ActualPosition = 1,
        [EnumMember]
        TargetSpeed = 2,
        [EnumMember]
        ActualSpeed = 3,
        [EnumMember]
        MaximumSpeed = 4,
        [EnumMember]
        MaximumAcceleration = 5,
        [EnumMember]
        AbsoluteMaximumCurrent = 6,
        [EnumMember]
        StandbyCurrent = 7,
        [EnumMember]
        TargetPoisitionReached = 8,
        [EnumMember]
        ReferenceSwitchStatus = 9,
        [EnumMember]
        RightLImitSwitchStatus = 10,
        [EnumMember]
        LeftLimitSwitchStatus = 11,
        [EnumMember]
        RightLimitSwitchDisable = 12,
        [EnumMember]
        LeftLimitSwitchDisable = 13,
        [EnumMember]
        StepratePrescaler = 14,


        [EnumMember]
        MinimumSpeed = 130,
        [EnumMember]
        ActualAcceleration = 135,
        [EnumMember]
        AccelerationThreshold = 136,
        [EnumMember]
        AccelerationDividor = 137,
        [EnumMember]
        RampMode = 138,
        [EnumMember]
        InterruptFlags = 139,
        [EnumMember]
        MicrostepResolution = 140,
        [EnumMember]
        ReferenceSwitchTolerance = 141,
        [EnumMember]
        SnapshotPosition = 142,
        [EnumMember]
        MaximumCurrentAtRest = 143,
        [EnumMember]
        MaximumCurrentAtLowAcceleration = 144,
        [EnumMember]
        MaximumCurrentAtHighAcceleration = 145,
        [EnumMember]
        AccelerationFactor = 146,
        [EnumMember]
        ReferenceSwitchDisableFlag = 147,
        [EnumMember]
        LimitSwitchDisableFlag = 148,
        [EnumMember]
        SoftStopFlag = 149,
        [EnumMember]
        PositionLatchFlag = 151,
        [EnumMember]
        InterruptMask = 152,
        [EnumMember]
        RampDivisor = 153,
        [EnumMember]
        PulseDivisor = 154,
        [EnumMember]
        ReferencingMode = 193,
        [EnumMember]
        ReferencingSearchSpeed = 194,
        [EnumMember]
        ReferencingSwitchSpeed = 195,
        [EnumMember]
        DriverOffTime = 198,
        [EnumMember]
        FastDecayTime = 200,
        [EnumMember]
        MixedDecayThreshold = 203,
        [EnumMember]
        FreeWheeling = 204,
        [EnumMember]
        StallDetectionThreshold = 205,
        [EnumMember]
        ActualLoadValue = 206,
        [EnumMember]
        DriverErrorFlags = 208,
        [EnumMember]
        EncoderPosition = 209,
        [EnumMember]
        EncoderPreScaler = 210,
        [EnumMember]
        FullstepThreshold = 211,
        [EnumMember]
        MaximumEncoderDeviation = 212,
        [EnumMember]
        GroupIndex = 213
    }
}
