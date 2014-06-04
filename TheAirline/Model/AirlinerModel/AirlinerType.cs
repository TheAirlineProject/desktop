﻿namespace TheAirline.Model.AirlinerModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    using TheAirline.Model.GeneralModel;

    //the class for a type of airliner
    [Serializable]
    public abstract class AirlinerType : ISerializable
    {
        #region Constructors and Destructors

        public AirlinerType(
            Manufacturer manufacturer,
            TypeOfAirliner typeOfAirliner,
            string name,
            string family,
            int cockpitCrew,
            double speed,
            long range,
            double wingspan,
            double length,
            double consumption,
            long price,
            long minRunwaylength,
            long fuelcapacity,
            BodyType body,
            TypeRange rangeType,
            EngineType engine,
            Period<DateTime> produced,
            int prodRate,
            Boolean isConvertable,
            Boolean standardType)
        {
            this.TypeAirliner = typeOfAirliner;
            this.AirlinerFamily = family;
            this.Manufacturer = manufacturer;
            this.Name = name;
            this.CruisingSpeed = speed;
            this.Range = range;
            this.Wingspan = wingspan;
            this.Length = length;
            this.CockpitCrew = cockpitCrew;
            this.Price = price;
            this.FuelConsumption = consumption;
            this.Produced = produced;
            this.Engine = engine;
            this.Body = body;
            this.RangeType = rangeType;
            this.MinRunwaylength = minRunwaylength;
            this.FuelCapacity = fuelcapacity;
            this.IsStandardType = standardType;
            this.ProductionRate = prodRate;
            this.IsConvertable = isConvertable;
        }

        protected AirlinerType(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt16("version");

            IList<PropertyInfo> props =
                new List<PropertyInfo>(
                    this.GetType()
                        .GetProperties()
                        .Where(
                            p =>
                                p.GetCustomAttribute(typeof(Versioning)) != null
                                && ((Versioning)p.GetCustomAttribute(typeof(Versioning))).AutoGenerated));

            foreach (SerializationEntry entry in info)
            {
                PropertyInfo prop =
                    props.FirstOrDefault(p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Name == entry.Name);

                if (prop != null)
                {
                    prop.SetValue(this, entry.Value);
                }
            }

            IEnumerable<PropertyInfo> notSetProps =
                props.Where(p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Version > version);

            foreach (PropertyInfo prop in notSetProps)
            {
                var ver = (Versioning)prop.GetCustomAttribute(typeof(Versioning));

                if (ver.AutoGenerated)
                {
                    prop.SetValue(this, ver.DefaultValue);
                }
            }

            if (version == 1)
            {
                if (this.TypeAirliner == TypeOfAirliner.Cargo)
                {
                    this.IsConvertable = false;
                }
                else
                {
                    this.IsConvertable = true;
                }
            }
        }

        #endregion

        #region Enums

        public enum BodyType
        {
            Narrow_Body,

            Single_Aisle,

            Wide_Body,

            Helicopter

        }

        public enum EngineType
        {
            Jet,

            Turboprop
        }

        public enum TypeOfAirliner
        {
            Passenger,

            Cargo,

            Mixed
        }

        public enum TypeRange
        {
            Regional,

            Short_Range,

            Medium_Range,

            Long_Range
        }

        #endregion

        #region Public Properties

        [Versioning("family")]
        public string AirlinerFamily { get; set; }

        [Versioning("basetype")]
        public AirlinerType BaseType { get; set; }

        [Versioning("body")]
        public BodyType Body { get; set; }

        public int Capacity
        {
            get
            {
                return this.getCapacity();
            }
            private set
            {
                ;
            }
        }

        [Versioning("cockpit")]
        public int CockpitCrew { get; set; }

        [Versioning("speed")]
        public double CruisingSpeed { get; set; }

        [Versioning("engine")]
        public EngineType Engine { get; set; }

        [Versioning("fuelcapacity")]
        public long FuelCapacity { get; set; }

        [Versioning("fuel")]
        public double FuelConsumption { get; set; }

        [Versioning("image")]
        public string Image { get; set; }

        [Versioning("isconvertable", Version = 2)]
        public Boolean IsConvertable { get; set; }

        [Versioning("isstandard")]
        public Boolean IsStandardType { get; set; }

        [Versioning("length")]
        public double Length { get; set; }

        public double Maintenance
        {
            get
            {
                return this.getMaintenance();
            }
            private set
            {
                ;
            }
        }

        [Versioning("manufacturer")]
        public Manufacturer Manufacturer { get; set; }

        [Versioning("runway")]
        public long MinRunwaylength { get; set; }

        [Versioning("name")]
        public string Name { get; set; }

        public long Price
        {
            get
            {
                return Convert.ToInt64(GeneralHelpers.GetInflationPrice(this._APrice));
            }
            set
            {
                this._APrice = value;
            }
        }

        [Versioning("produced")]
        public Period<DateTime> Produced { get; set; }

        [Versioning("production")]
        public int ProductionRate { get; set; }

        [Versioning("range")]
        public long Range { get; set; }

        [Versioning("rangetype")]
        public TypeRange RangeType { get; set; }

        [Versioning("typeairliner")]
        public TypeOfAirliner TypeAirliner { get; set; }

        [Versioning("wingspan")]
        public double Wingspan { get; set; }

        [Versioning("price")]
        public long _APrice { get; set; }

        #endregion

        #region Public Methods and Operators

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", 2);

            Type myType = this.GetType();
            IList<PropertyInfo> props =
                new List<PropertyInfo>(
                    myType.GetProperties().Where(p => p.GetCustomAttribute(typeof(Versioning)) != null));

            foreach (PropertyInfo prop in props)
            {
                object propValue = prop.GetValue(this, null);

                var att = (Versioning)prop.GetCustomAttribute(typeof(Versioning));

                info.AddValue(att.Name, propValue);
            }
        }

        public abstract int getCapacity();

        public long getLeasingPrice()
        {
            double months = 5 * 12;
            double rate = 1.06;
            double leasingPrice = this.Price / months * rate;
            return Convert.ToInt64(leasingPrice);
        }

        public long getMaintenance()
        {
            double maintenance = 0.0013 * this.Price * 12;
            return Convert.ToInt64(maintenance);
        }

        #endregion
    }

    //the class for a passenger airliner type
    [Serializable]
    public class AirlinerPassengerType : AirlinerType
    {
        #region Constructors and Destructors

        public AirlinerPassengerType(
            Manufacturer manufacturer,
            string name,
            string family,
            int seating,
            int cockpitcrew,
            int cabincrew,
            double speed,
            long range,
            double wingspan,
            double length,
            double consumption,
            long price,
            int maxAirlinerClasses,
            long minRunwaylength,
            long fuelcapacity,
            BodyType body,
            TypeRange rangeType,
            EngineType engine,
            Period<DateTime> produced,
            int prodRate,
            Boolean isConvertable,
            Boolean standardType = true)
            : base(
                manufacturer,
                TypeOfAirliner.Passenger,
                name,
                family,
                cockpitcrew,
                speed,
                range,
                wingspan,
                length,
                consumption,
                price,
                minRunwaylength,
                fuelcapacity,
                body,
                rangeType,
                engine,
                produced,
                prodRate,
                isConvertable,
                standardType)
        {
            this.MaxSeatingCapacity = seating;
            this.CabinCrew = cabincrew;
            this.MaxAirlinerClasses = maxAirlinerClasses;
        }

        protected AirlinerPassengerType(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
            int version = info.GetInt16("version");

            IList<PropertyInfo> props =
                new List<PropertyInfo>(
                    this.GetType()
                        .GetProperties()
                        .Where(
                            p =>
                                p.GetCustomAttribute(typeof(Versioning)) != null
                                && ((Versioning)p.GetCustomAttribute(typeof(Versioning))).AutoGenerated));

            foreach (SerializationEntry entry in info)
            {
                PropertyInfo prop =
                    props.FirstOrDefault(p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Name == entry.Name);

                if (prop != null)
                {
                    prop.SetValue(this, entry.Value);
                }
            }

            IEnumerable<PropertyInfo> notSetProps =
                props.Where(p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Version > version);

            foreach (PropertyInfo prop in notSetProps)
            {
                var ver = (Versioning)prop.GetCustomAttribute(typeof(Versioning));

                if (ver.AutoGenerated)
                {
                    prop.SetValue(this, ver.DefaultValue);
                }
            }
        }

        #endregion

        #region Public Properties

        [Versioning("cabincrew")]
        public int CabinCrew { get; set; }

        [Versioning("maxclasses")]
        public int MaxAirlinerClasses { get; set; }

        [Versioning("maxcapacity")]
        public int MaxSeatingCapacity { get; set; }

        #endregion

        #region Public Methods and Operators

        protected virtual new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", 1);

            Type myType = this.GetType();
            IList<PropertyInfo> props =
                new List<PropertyInfo>(
                    myType.GetProperties().Where(p => p.GetCustomAttribute(typeof(Versioning)) != null));

            foreach (PropertyInfo prop in props)
            {
                object propValue = prop.GetValue(this, null);

                var att = (Versioning)prop.GetCustomAttribute(typeof(Versioning));

                info.AddValue(att.Name, propValue);
            }

            base.GetObjectData(info, context);
        }

        public override int getCapacity()
        {
            return this.MaxSeatingCapacity;
        }

        #endregion
    }

    //the class for a combi airliner type
    [Serializable]
    public class AirlinerCombiType : AirlinerPassengerType
    {
        #region Constructors and Destructors

        public AirlinerCombiType(
            Manufacturer manufacturer,
            string name,
            string family,
            int seating,
            int cockpitcrew,
            int cabincrew,
            double speed,
            long range,
            double wingspan,
            double length,
            double consumption,
            long price,
            int maxAirlinerClasses,
            long minRunwaylength,
            long fuelcapacity,
            BodyType body,
            TypeRange rangeType,
            EngineType engine,
            Period<DateTime> produced,
            int prodRate,
            double cargo,
            Boolean isConvertable,
            Boolean standardType = true)
            : base(
                manufacturer,
                name,
                family,
                seating,
                cockpitcrew,
                cabincrew,
                speed,
                range,
                wingspan,
                length,
                consumption,
                price,
                maxAirlinerClasses,
                minRunwaylength,
                fuelcapacity,
                body,
                rangeType,
                engine,
                produced,
                prodRate,
                isConvertable,
                standardType)
        {
            this.CargoSize = cargo;
            this.TypeAirliner = TypeOfAirliner.Mixed;
        }

        private AirlinerCombiType(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
            int version = info.GetInt16("version");

            IList<PropertyInfo> props =
                new List<PropertyInfo>(
                    this.GetType()
                        .GetProperties()
                        .Where(
                            p =>
                                p.GetCustomAttribute(typeof(Versioning)) != null
                                && ((Versioning)p.GetCustomAttribute(typeof(Versioning))).AutoGenerated));

            foreach (SerializationEntry entry in info)
            {
                PropertyInfo prop =
                    props.FirstOrDefault(p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Name == entry.Name);

                if (prop != null)
                {
                    prop.SetValue(this, entry.Value);
                }
            }

            IEnumerable<PropertyInfo> notSetProps =
                props.Where(p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Version > version);

            foreach (PropertyInfo prop in notSetProps)
            {
                var ver = (Versioning)prop.GetCustomAttribute(typeof(Versioning));

                if (ver.AutoGenerated)
                {
                    prop.SetValue(this, ver.DefaultValue);
                }
            }
        }

        #endregion

        #region Public Properties

        [Versioning("cargo")]
        public double CargoSize { get; set; }

        #endregion

        #region Public Methods and Operators

        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", 1);

            Type myType = this.GetType();
            IList<PropertyInfo> props =
                new List<PropertyInfo>(
                    myType.GetProperties().Where(p => p.GetCustomAttribute(typeof(Versioning)) != null));

            foreach (PropertyInfo prop in props)
            {
                object propValue = prop.GetValue(this, null);

                var att = (Versioning)prop.GetCustomAttribute(typeof(Versioning));

                info.AddValue(att.Name, propValue);
            }

            base.GetObjectData(info, context);
        }

        #endregion
    }

    //the class for a cargo airliner type
    [Serializable]
    public class AirlinerCargoType : AirlinerType
    {
        #region Constructors and Destructors

        public AirlinerCargoType(
            Manufacturer manufacturer,
            string name,
            string family,
            int cockpitcrew,
            double cargoSize,
            double speed,
            long range,
            double wingspan,
            double length,
            double consumption,
            long price,
            long minRunwaylength,
            long fuelcapacity,
            BodyType body,
            TypeRange rangeType,
            EngineType engine,
            Period<DateTime> produced,
            int prodRate,
            Boolean isConvertable,
            Boolean standardType = true)
            : base(
                manufacturer,
                TypeOfAirliner.Cargo,
                name,
                family,
                cockpitcrew,
                speed,
                range,
                wingspan,
                length,
                consumption,
                price,
                minRunwaylength,
                fuelcapacity,
                body,
                rangeType,
                engine,
                produced,
                prodRate,
                isConvertable,
                standardType)
        {
            this.CargoSize = cargoSize;
        }

        private AirlinerCargoType(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
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

                if (version == 1)
                {
                    this.IsConvertable = false;
                }
            }
        }

        #endregion

        #region Public Properties

        [Versioning("cargo")]
        public double CargoSize { get; set; }

        #endregion

        #region Public Methods and Operators

        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", 2);

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

            base.GetObjectData(info, context);
        }

        public override int getCapacity()
        {
            return (int)this.CargoSize;
        }

        #endregion
    }

    //the collection of airliner types
    public class AirlinerTypes
    {
        #region Static Fields

        private static List<AirlinerType> types = new List<AirlinerType>();

        #endregion

        //clears the list of airliner types

        //adds an airliner type to the list

        #region Public Methods and Operators

        public static void AddType(AirlinerType type)
        {
            types.Add(type);
        }

        public static void Clear()
        {
            types = new List<AirlinerType>();
        }

        //returns an airliner with a name

        //returns all airliner types
        public static List<AirlinerType> GetAllTypes()
        {
            return types.FindAll(t => t.IsStandardType);
        }

        //returns a list of airliner types

        //returns all non standard airliner types
        public static List<AirlinerType> GetNonStandardTypes()
        {
            return types.FindAll(t => !t.IsStandardType);
        }

        public static AirlinerType GetType(string name)
        {
            return types.Find(t => t.Name == name || t.Name.ToUpper() == name);
        }

        public static List<AirlinerType> GetTypes(Predicate<AirlinerType> match)
        {
            return types.FindAll(t => t.IsStandardType).FindAll(match);
        }

        //removes an airliner type
        public static void RemoveType(AirlinerType type)
        {
            types.Remove(type);
        }

        #endregion
    }
}