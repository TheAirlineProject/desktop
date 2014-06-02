﻿namespace TheAirline.Model.AirlinerModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    using TheAirline.Model.GeneralModel;

    //the class for a facility in an airliner
    [Serializable]
    public class AirlinerFacility : ISerializable
    {
        #region Fields

        [Versioning("price")]
        private double APricePerSeat;

        #endregion

        #region Constructors and Destructors

        public AirlinerFacility(
            string section,
            string uid,
            FacilityType type,
            int fromYear,
            int serviceLevel,
            double percentOfSeats,
            double pricePerSeat,
            double seatUses)
        {
            Section = section;
            this.Uid = uid;
            this.FromYear = fromYear;
            this.PricePerSeat = pricePerSeat;
            this.PercentOfSeats = percentOfSeats;
            this.Type = type;
            this.ServiceLevel = serviceLevel;
            this.SeatUses = seatUses;
        }

        private AirlinerFacility(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt16("version");

            IEnumerable<FieldInfo> fields =
                this.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(p => p.GetCustomAttribute(typeof(Versioning)) != null);

            IList<PropertyInfo> props =
                new List<PropertyInfo>(
                    this.GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Where(p => p.GetCustomAttribute(typeof(Versioning)) != null));

            IEnumerable<MemberInfo> propsAndFields = props.Cast<MemberInfo>().Union(fields.Cast<MemberInfo>());

            foreach (SerializationEntry entry in info)
            {
                MemberInfo prop =
                    propsAndFields.FirstOrDefault(
                        p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Name == entry.Name);

                if (prop != null)
                {
                    if (prop is FieldInfo)
                    {
                        ((FieldInfo)prop).SetValue(this, entry.Value);
                    }
                    else
                    {
                        ((PropertyInfo)prop).SetValue(this, entry.Value);
                    }
                }
            }

            IEnumerable<MemberInfo> notSetProps =
                propsAndFields.Where(p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Version > version);

            foreach (MemberInfo notSet in notSetProps)
            {
                var ver = (Versioning)notSet.GetCustomAttribute(typeof(Versioning));

                if (ver.AutoGenerated)
                {
                    if (notSet is FieldInfo)
                    {
                        ((FieldInfo)notSet).SetValue(this, ver.DefaultValue);
                    }
                    else
                    {
                        ((PropertyInfo)notSet).SetValue(this, ver.DefaultValue);
                    }
                }
            }
        }

        #endregion

        #region Enums

        public enum FacilityType
        {
            Audio,

            Video,

            Seat
        }

        #endregion

        #region Public Properties

        public static string Section { get; set; }

        [Versioning("fromyear")]
        public int FromYear { get; set; }

        public string Name
        {
            get
            {
                return Translator.GetInstance().GetString(Section, this.Uid);
            }
        }

        [Versioning("percent")]
        public double PercentOfSeats { get; set; }

        public double PricePerSeat
        {
            get
            {
                return GeneralHelpers.GetInflationPrice(this.APricePerSeat);
            }
            set
            {
                this.APricePerSeat = value;
            }
        }

        [Versioning("seatuses")]
        public double SeatUses { get; set; }

        [Versioning("servicelevel")]
        public int ServiceLevel { get; set; }

        [Versioning("type")]
        public FacilityType Type { get; set; }

        [Versioning("uid")]
        public string Uid { get; set; }

        #endregion

        #region Public Methods and Operators

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", 1);

            Type myType = this.GetType();

            IEnumerable<FieldInfo> fields =
                myType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(p => p.GetCustomAttribute(typeof(Versioning)) != null);

            IList<PropertyInfo> props =
                new List<PropertyInfo>(
                    myType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Where(p => p.GetCustomAttribute(typeof(Versioning)) != null));

            IEnumerable<MemberInfo> propsAndFields = props.Cast<MemberInfo>().Union(fields.Cast<MemberInfo>());

            foreach (MemberInfo member in propsAndFields)
            {
                object propValue;

                if (member is FieldInfo)
                {
                    propValue = ((FieldInfo)member).GetValue(this);
                }
                else
                {
                    propValue = ((PropertyInfo)member).GetValue(this, null);
                }

                var att = (Versioning)member.GetCustomAttribute(typeof(Versioning));

                info.AddValue(att.Name, propValue);
            }
        }

        #endregion
    }

    //lists of airliner facilities
    public class AirlinerFacilities
    {
        #region Static Fields

        private static Dictionary<AirlinerFacility.FacilityType, List<AirlinerFacility>> facilities =
            new Dictionary<AirlinerFacility.FacilityType, List<AirlinerFacility>>();

        #endregion

        //clears the list

        //adds a facility to the list

        #region Public Methods and Operators

        public static void AddFacility(AirlinerFacility facility)
        {
            if (!facilities.ContainsKey(facility.Type))
            {
                facilities.Add(facility.Type, new List<AirlinerFacility>());
            }

            facilities[facility.Type].Add(facility);
        }

        public static void Clear()
        {
            facilities = new Dictionary<AirlinerFacility.FacilityType, List<AirlinerFacility>>();
        }

        //returns the list of all facilities
        public static List<AirlinerFacility> GetAllFacilities()
        {
            return facilities.Values.SelectMany(v => v).ToList();
        }

        public static AirlinerFacility GetBasicFacility(AirlinerFacility.FacilityType type)
        {
            return facilities[type][0];
        }

        //returns the list of facilities for a specific type after a specific year
        public static List<AirlinerFacility> GetFacilities(AirlinerFacility.FacilityType type, int year)
        {
            if (facilities.ContainsKey(type))
            {
                return facilities[type].FindAll((delegate(AirlinerFacility f) { return f.FromYear <= year; }));
            }

            return new List<AirlinerFacility>();
        }

        //returns the list of facilities for a specific type
        public static List<AirlinerFacility> GetFacilities(AirlinerFacility.FacilityType type)
        {
            if (facilities.ContainsKey(type))
            {
                return facilities[type];
            }
            return new List<AirlinerFacility>();
        }

        // chs, 2011-13-10 added function to return a specific airliner facility
        //returns a facility based on name and type
        public static AirlinerFacility GetFacility(AirlinerFacility.FacilityType type, string uid)
        {
            if (GetFacilities(type).Count > 0)
            {
                return GetFacilities(type).Find((delegate(AirlinerFacility f) { return f.Uid == uid; }));
            }
            return null;
        }

        #endregion

        //returns the basic facility for a specific type
    }
}