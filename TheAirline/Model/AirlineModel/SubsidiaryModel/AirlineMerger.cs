﻿namespace TheAirline.Model.AirlineModel.SubsidiaryModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    using TheAirline.Model.GeneralModel;

    [Serializable]
    //the class for a merger between two airlines either as a regular merger or where one of them gets subsidiary of the other
    public class AirlineMerger : ISerializable
    {
        #region Constructors and Destructors

        public AirlineMerger(string name, Airline airline1, Airline airline2, DateTime date, MergerType type)
        {
            this.Name = name;
            this.Airline1 = airline1;
            this.Airline2 = airline2;
            this.Date = date;
            this.Type = type;
        }

        private AirlineMerger(SerializationInfo info, StreamingContext ctxt)
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

        public enum MergerType
        {
            Merger,

            Subsidiary
        }

        #endregion

        #region Public Properties

        public Airline Airline1 { get; set; }

        public Airline Airline2 { get; set; }

        public DateTime Date { get; set; }

        public string Name { get; set; }

        public string NewName { get; set; }

        public MergerType Type { get; set; }

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

    //the list of airline mergers 
    public class AirlineMergers
    {
        #region Static Fields

        private static readonly List<AirlineMerger> mergers = new List<AirlineMerger>();

        #endregion

        //adds a merger to the list of mergers

        #region Public Methods and Operators

        public static void AddAirlineMerger(AirlineMerger merger)
        {
            mergers.Add(merger);
        }

        public static void Clear()
        {
            mergers.Clear();
        }

        //returns all mergers
        public static List<AirlineMerger> GetAirlineMergers()
        {
            return mergers;
        }

        //returns all mergers for a specific date
        public static List<AirlineMerger> GetAirlineMergers(DateTime date)
        {
            return mergers.FindAll(m => m.Date.ToShortDateString() == date.ToShortDateString());
        }

        //removes a merger from the list
        public static void RemoveAirlineMerger(AirlineMerger merger)
        {
            mergers.Remove(merger);
        }

        #endregion

        //clears the list of mergers
    }
}