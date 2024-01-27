using Newtonsoft.Json;
using System;
using System.Xml;

namespace RageCoop.Resources.HandlingEnforcer.Client
{
    internal struct Vector3
    {
        public float X, Y, Z;
        public static implicit operator Vector3(GTA.Math.Vector3 v) => new Vector3() { X = v.X, Y = v.Y, Z = v.Z };
        public static implicit operator GTA.Math.Vector3(Vector3 v) => new GTA.Math.Vector3() { X = v.X, Y = v.Y, Z = v.Z };

    }
    internal class HandlingData
    {
        public HandlingData()
        {

        }
        private HandlingData(XmlNode node)
        {
            foreach (XmlNode n in node.ChildNodes)
            {
                switch (n.Name)
                {
                    case "handlingName":
                        Name = n.InnerText;
                        break;
                    case "fMass":
                        Mass = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fPercentSubmerged":
                        PercentSubmerged = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fInitialDriveForce":
                        InitialDriveForce = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fDriveInertia":
                        DriveInertia = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fClutchChangeRateScaleUpShift":
                        ClutchChangeRateScaleUpShift = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fClutchChangeRateScaleDownShift":
                        ClutchChangeRateScaleDownShift = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fBrakeForce":
                        BrakeForce = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fHandBrakeForce":
                        HandBrakeForce = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSteeringLock":
                        SteeringLock = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fTractionCurveMax":
                        TractionCurveMax = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fTractionCurveMin":
                        TractionCurveMin = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fTractionSpringDeltaMax":
                        TractionSpringDeltaMax = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fCamberStiffnesss":
                        CamberStiffness = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fTractionBiasFront":
                        TractionBiasFront = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fTractionLossMult":
                        TractionLossMultiplier = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionForce":
                        SuspensionForce = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionCompDamp":
                        SuspensionCompressionDamping = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionReboundDamp":
                        SuspensionReboundDamping = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionUpperLimit":
                        SuspensionUpperLimit = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionLowerLimit":
                        SuspensionLowerLimit = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionRaise":
                        SuspensionRaise = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionBiasFront":
                        SuspensionBiasFront = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fAntiRollBarForce":
                        AntiRollBarForce = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fAntiRollBarBiasFront":
                        AntiRollBarBiasFront = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fRollCentreHeightFront":
                        RollCenterHeightFront = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fRollCentreHeightRear":
                        RollCenterHeightRear = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fCollisionDamageMult":
                        CollisionDamageMultiplier = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fWeaponDamageMult":
                        WeaponDamageMultiplier = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fDeformationDamageMult":
                        DeformationDamageMultiplier = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fEngineDamageMult":
                        EngineDamageMultiplier = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fPetrolTankVolume":
                        PetrolTankVolume = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fOilVolume":
                        OilVolume = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSeatOffsetDistX":
                        SeatOffsetDistanceX = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSeatOffsetDistY":
                        SeatOffsetDistanceY = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSeatOffsetDistZ":
                        SeatOffsetDistanceZ = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "nMonetaryValue":
                        MonetaryValue = int.Parse(n.Attributes["value"].Value);
                        break;
                    case "nInitialDriveGears":
                        InitialDriveGears = int.Parse(n.Attributes["value"].Value);
                        break;
                    case "vecCentreOfMassOffset":
                        CenterOfMassOffset = ToVec(n);
                        break;
                    case "vecInertiaMultiplier":
                        InertiaMultiplier = ToVec(n);
                        break;
                }
            }

            // Due to mismatched values between SHVDN and handling.meta
            AntiRollBarBiasFront *= 2;
            SuspensionBiasFront *= 2;
            TractionBiasFront *= 2;
            SuspensionCompressionDamping /= 10;
            SuspensionReboundDamping /= 10;
            SteeringLock /= 57.29577f;
            TractionLossMultiplier /= 100;
        }
        public HandlingData(GTA.HandlingData h, int hash)
        {
            Hash = hash;
            AntiRollBarBiasFront = h.AntiRollBarBiasFront;
            AntiRollBarForce = h.AntiRollBarForce;
            BrakeForce = h.BrakeForce;
            CamberStiffness = h.CamberStiffness;
            CenterOfMassOffset = h.CenterOfMassOffset;
            ClutchChangeRateScaleDownShift = h.ClutchChangeRateScaleDownShift;
            ClutchChangeRateScaleUpShift = h.ClutchChangeRateScaleUpShift;
            CollisionDamageMultiplier = h.CollisionDamageMultiplier;
            DeformationDamageMultiplier = h.DeformationDamageMultiplier;
            DriveInertia = h.DriveInertia;
            EngineDamageMultiplier = h.EngineDamageMultiplier;
            HandBrakeForce = h.HandBrakeForce;
            InertiaMultiplier = h.InertiaMultiplier;
            InitialDriveForce = h.InitialDriveForce;
            InitialDriveGears = h.InitialDriveGears;
            Mass = h.Mass;
            MonetaryValue = h.MonetaryValue;
            OilVolume = h.OilVolume;
            PercentSubmerged = h.PercentSubmerged;
            PetrolTankVolume = h.PetrolTankVolume;
            RollCenterHeightFront = h.RollCenterHeightFront;
            RollCenterHeightRear = h.RollCenterHeightRear;
            SeatOffsetDistanceX = h.SeatOffsetDistanceX;
            SeatOffsetDistanceY = h.SeatOffsetDistanceY;
            SeatOffsetDistanceZ = h.SeatOffsetDistanceZ;
            SteeringLock = h.SteeringLock;
            SuspensionBiasFront = h.SuspensionBiasFront;
            SuspensionCompressionDamping = h.SuspensionCompressionDamping;
            SuspensionForce = h.SuspensionForce;
            SuspensionLowerLimit = h.SuspensionLowerLimit;
            SuspensionRaise = h.SuspensionRaise;
            SuspensionReboundDamping = h.SuspensionReboundDamping;
            SuspensionUpperLimit = h.SuspensionUpperLimit;
            TractionBiasFront = h.TractionBiasFront;
            TractionCurveMax = h.TractionCurveMax;
            TractionCurveMin = h.TractionCurveMin;
            TractionLossMultiplier = h.TractionLossMultiplier;
            TractionSpringDeltaMax = h.TractionSpringDeltaMax;
            WeaponDamageMultiplier = h.WeaponDamageMultiplier;
            BoostMaxSpeed = h.BoostMaxSpeed;
            BrakeBiasFront = h.BrakeBiasFront;
            DownForceModifier = h.DownForceModifier;
            InitialDragCoefficient = h.InitialDragCoefficient;
            InitialDriveMaxFlatVelocity = h.InitialDriveMaxFlatVelocity;
            LowSpeedTractionLossMultiplier = h.LowSpeedTractionLossMultiplier;
            PopUpLightRotation = h.PopUpLightRotation;
            RocketBoostCapacity = h.RocketBoostCapacity;
            TractionCurveLateral = h.TractionCurveLateral;
        }

        private Vector3 ToVec(XmlNode n)
        {
            return new Vector3()
            {
                X = float.Parse(n.Attributes["x"].Value),
                Y = float.Parse(n.Attributes["x"].Value),
                Z = float.Parse(n.Attributes["x"].Value),
            };
        }
        public void ApplyTo(GTA.HandlingData h)
        {
            h.AntiRollBarBiasFront = AntiRollBarBiasFront;
            h.AntiRollBarForce = AntiRollBarForce;
            h.BrakeForce = BrakeForce;
            h.CamberStiffness = CamberStiffness;
            h.CenterOfMassOffset = CenterOfMassOffset;
            h.ClutchChangeRateScaleDownShift = ClutchChangeRateScaleDownShift;
            h.ClutchChangeRateScaleUpShift = ClutchChangeRateScaleUpShift;
            h.CollisionDamageMultiplier = CollisionDamageMultiplier;
            h.DeformationDamageMultiplier = DeformationDamageMultiplier;
            h.DriveInertia = DriveInertia;
            h.EngineDamageMultiplier = EngineDamageMultiplier;
            h.HandBrakeForce = HandBrakeForce;
            h.InertiaMultiplier = InertiaMultiplier;
            h.InitialDriveForce = InitialDriveForce;
            h.InitialDriveGears = InitialDriveGears;
            h.Mass = Mass;
            h.MonetaryValue = MonetaryValue;
            h.OilVolume = OilVolume;
            h.PercentSubmerged = PercentSubmerged;
            h.PetrolTankVolume = PetrolTankVolume;
            h.RollCenterHeightFront = RollCenterHeightFront;
            h.RollCenterHeightRear = RollCenterHeightRear;
            h.SeatOffsetDistanceX = SeatOffsetDistanceX;
            h.SeatOffsetDistanceY = SeatOffsetDistanceY;
            h.SeatOffsetDistanceZ = SeatOffsetDistanceZ;
            h.SteeringLock = SteeringLock;
            h.SuspensionBiasFront = SuspensionBiasFront;
            h.SuspensionCompressionDamping = SuspensionCompressionDamping;
            h.SuspensionForce = SuspensionForce;
            h.SuspensionLowerLimit = SuspensionLowerLimit;
            h.SuspensionRaise = SuspensionRaise;
            h.SuspensionReboundDamping = SuspensionReboundDamping;
            h.SuspensionUpperLimit = SuspensionUpperLimit;
            h.TractionBiasFront = TractionBiasFront;
            h.TractionCurveMax = TractionCurveMax;
            h.TractionCurveMin = TractionCurveMin;
            h.TractionLossMultiplier = TractionLossMultiplier;
            h.TractionSpringDeltaMax = TractionSpringDeltaMax;
            h.WeaponDamageMultiplier = WeaponDamageMultiplier;
            h.BoostMaxSpeed = BoostMaxSpeed;
            h.BrakeBiasFront = BrakeBiasFront;
            h.DownForceModifier = DownForceModifier;
            h.InitialDragCoefficient = InitialDragCoefficient;
            h.InitialDriveMaxFlatVelocity = InitialDriveMaxFlatVelocity;
            h.LowSpeedTractionLossMultiplier = LowSpeedTractionLossMultiplier;
            h.PopUpLightRotation = PopUpLightRotation;
            h.RocketBoostCapacity = RocketBoostCapacity;
            h.TractionCurveLateral = TractionCurveLateral;
        }
        public float BoostMaxSpeed;
        public float BrakeBiasFront;
        public float DownForceModifier;
        public float InitialDragCoefficient;
        public float InitialDriveMaxFlatVelocity;
        public float LowSpeedTractionLossMultiplier;
        public float PopUpLightRotation;
        public float RocketBoostCapacity;
        public float TractionCurveLateral;

        public string Name;
        public int Hash;
        public float AntiRollBarBiasFront;
        public float AntiRollBarForce;
        public float BrakeForce;
        public float CamberStiffness;
        public Vector3 CenterOfMassOffset;
        public float ClutchChangeRateScaleDownShift;
        public float ClutchChangeRateScaleUpShift;
        public float CollisionDamageMultiplier;
        public float DeformationDamageMultiplier;
        public float DriveInertia;
        public float EngineDamageMultiplier;
        public float HandBrakeForce;
        public Vector3 InertiaMultiplier;
        public float InitialDriveForce;
        public int InitialDriveGears;
        public float Mass;
        public int MonetaryValue;
        public float OilVolume;
        public float PercentSubmerged;
        public float PetrolTankVolume;
        public float RollCenterHeightFront;
        public float RollCenterHeightRear;
        public float SeatOffsetDistanceX;
        public float SeatOffsetDistanceY;
        public float SeatOffsetDistanceZ;
        public float SteeringLock;
        public float SuspensionBiasFront;
        public float SuspensionCompressionDamping;
        public float SuspensionForce;
        public float SuspensionLowerLimit;
        public float SuspensionRaise;
        public float SuspensionReboundDamping;
        public float SuspensionUpperLimit;
        public float TractionBiasFront;
        public float TractionCurveMax;
        public float TractionCurveMin;
        public float TractionLossMultiplier;
        public float TractionSpringDeltaMax;
        public float WeaponDamageMultiplier;
    }
    public class VersionConverter : JsonConverter<GTA.Math.Vector3>
    {
        public override void WriteJson(JsonWriter writer, GTA.Math.Vector3 value, JsonSerializer serializer)
        {
            writer.WriteValue(value.X);
            writer.WriteValue(value.Y);
            writer.WriteValue(value.Z);
        }

        public override GTA.Math.Vector3 ReadJson(JsonReader reader, Type objectType, GTA.Math.Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {

            return new GTA.Math.Vector3()
            {
                X = (float)reader.ReadAsDouble(),
                Y = (float)reader.ReadAsDouble(),
                Z = (float)reader.ReadAsDouble(),
            };
        }
    }
}
