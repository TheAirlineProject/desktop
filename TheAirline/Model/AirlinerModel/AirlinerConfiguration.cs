﻿namespace TheAirline.Model.AirlinerModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    using TheAirline.Model.GeneralModel;

    //the configuration of an airliner 
    [Serializable]
    public class AirlinerConfiguration : Configuration
    {
        #region Constructors and Destructors

        public AirlinerConfiguration(string name, int minimumSeats, Boolean standard)
            : base(ConfigurationType.Airliner, name, standard)
        {
            this.MinimumSeats = minimumSeats;
            this.Classes = new List<AirlinerClassConfiguration>();
        }

        //returns the number of classes

        private AirlinerConfiguration(SerializationInfo info, StreamingContext ctxt)
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
            }
        }

        #endregion

        #region Public Properties

        [Versioning("classes")]
        public List<AirlinerClassConfiguration> Classes { get; set; }

        [Versioning("minseats")]
        public int MinimumSeats { get; set; }

        #endregion

        #region Public Methods and Operators

        public new void GetObjectData(SerializationInfo info, StreamingContext context)
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

            base.GetObjectData(info, context);
        }

        public void addClassConfiguration(AirlinerClassConfiguration conf)
        {
            this.Classes.Add(conf);
        }

        public int getNumberOfClasses()
        {
            return this.Classes.Count;
        }

        #endregion
    }

    //the configuration of an airliner class
    [Serializable]
    public class AirlinerClassConfiguration : ISerializable
    {
        #region Constructors and Destructors

        public AirlinerClassConfiguration(AirlinerClass.ClassType type, int seating, int regularseating)
        {
            this.SeatingCapacity = seating;
            this.RegularSeatingCapacity = regularseating;
            this.Type = type;
            this.Facilities = new List<AirlinerFacility>();
        }

        private AirlinerClassConfiguration(SerializationInfo info, StreamingContext ctxt)
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

        [Versioning("facilities")]
        public List<AirlinerFacility> Facilities { get; set; }

        [Versioning("regularseating")]
        public int RegularSeatingCapacity { get; set; }

        [Versioning("seatingcapacity")]
        public int SeatingCapacity { get; set; }

        [Versioning("type")]
        public AirlinerClass.ClassType Type { get; set; }

        #endregion

        #region Public Methods and Operators

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
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
        }

        //adds a facility to the configuration
        public void addFacility(AirlinerFacility facility)
        {
            this.Facilities.Add(facility);
        }

        //returns all facilities
        public List<AirlinerFacility> getFacilities()
        {
            return this.Facilities;
        }

        //returns the facility of a specific type
        public AirlinerFacility getFacility(AirlinerFacility.FacilityType type)
        {
            return this.Facilities.Find(f => f.Type == type);
        }

        #endregion
    }
}