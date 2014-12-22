﻿namespace TheAirline.Model.PassengerModel
{
    using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using TheAirline.GUIModel.HelpersModel;
using TheAirline.Model.AirlineModel;
using TheAirline.Model.AirlinerModel;
using TheAirline.Model.AirportModel;
using TheAirline.Model.GeneralModel;
using TheAirline.Model.GeneralModel.CountryModel;

    /*
     * The class for flight restrictions with no flights between two countries or unions
     */

    [Serializable]
    public class FlightRestriction : ISerializable
    {
        #region Constructors and Destructors

        public FlightRestriction(RestrictionType type, DateTime startDate, DateTime endDate, BaseUnit from, BaseUnit to)
        {
            this.Type = type;
            this.StartDate = startDate;
            this.EndDate = endDate;
            this.From = from;
            this.To = to;
        }

        private FlightRestriction(SerializationInfo info, StreamingContext ctxt)
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

        public enum RestrictionType
        {
            Flights,

            Airlines,

            AllowAirline,

            Airline, 

            Aircrafts,

            AllowAirport,

            Maintenance
        }

        #endregion

        #region Public Properties

        [Versioning("enddate")]
        public DateTime EndDate { get; set; }

        [Versioning("from")]
        public BaseUnit From { get; set; }

        [Versioning("startdate")]
        public DateTime StartDate { get; set; }

        [Versioning("to")]
        public BaseUnit To { get; set; }

        [Versioning("type")]
        public RestrictionType Type { get; set; }

        [Versioning("level",Version=2)]
        public int MaintenanceLevel { get; set; }

        [Versioning("airline",Version=2)]
        public Airline Airline { get; set; }

        [Versioning("airport",Version=2)]
        public Airport Airport { get; set; }
        #endregion

        #region Public Methods and Operators

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
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
        }

        #endregion
    }

    //the list of flight restrictions
    public class FlightRestrictions
    {
        #region Static Fields

        private static readonly List<FlightRestriction> restrictions = new List<FlightRestriction>();

        #endregion

        //adds a flight restriction to the list of restrictions

        #region Public Methods and Operators

        public static void AddRestriction(FlightRestriction restriction)
        {
            restrictions.Add(restriction);
        }

        //returns all restrictions

        //celars the list
        public static void Clear()
        {
            restrictions.Clear();
        }

        public static List<FlightRestriction> GetRestrictions()
        {
            return restrictions;
        }
        private static List<FlightRestriction> GetRestrictions(Country to, DateTime date, Airline airline, FlightRestriction.RestrictionType type)
        {
            to = new CountryCurrentCountryConverter().Convert(to) as Country;

            return
                GetRestrictions()
                    .FindAll(
                        r =>
                            (r.To == to || (r.To is Union && ((Union)r.To).isMember(to, date)))
                            && (date >= r.StartDate && date <= r.EndDate) && r.Type == type && r.Airline != null && r.Airline == airline);
        }
        public static List<FlightRestriction> GetRestrictions(Country from,
        Country to,
        DateTime date,
        FlightRestriction.RestrictionType type)
        {
            from = new CountryCurrentCountryConverter().Convert(from) as Country;
            to = new CountryCurrentCountryConverter().Convert(to) as Country;

            //var aircraftRestrictions = restrictions.Where(r => r.Type == type);

           // var fromRestrictions = aircraftRestrictions.Where(r => (r.From == from || (r.From is Union && ((Union)r.From).isMember(from, date))));
           // var toRestrictions = aircraftRestrictions.Where(r => (r.To == to || (r.To is Union && ((Union)r.To).isMember(to, date))));

            return
                GetRestrictions()
                    .FindAll(
                        r =>
                            (r.From == from || (r.From is Union && ((Union)r.From).isMember(from, date)))
                            && (r.To == to || (r.To is Union && ((Union)r.To).isMember(to, date)))
                            && (date >= r.StartDate && date <= r.EndDate) && r.Type == type);
        }
        //returns if there is flight restrictions from one country to another
        public static Boolean HasRestriction(
            Country from,
            Country to,
            DateTime date,
            FlightRestriction.RestrictionType type)
        {
            var restrictions = GetRestrictions(from,to,date,type);

            return restrictions.Count > 0;
        }
       //returns if there is restrictions for buying an aircraft type for an airline
        public static Boolean HasRestriction(Airline airline, AirlinerType airliner, DateTime date)
        {
            return HasRestriction(airline.Profile.Country, airliner.Manufacturer.Country, date, FlightRestriction.RestrictionType.Aircrafts);
        }
        //returns if there is flight restrictions between two countries
        public static Boolean HasRestriction(Country country1, Country country2, DateTime date)
        {
            return HasRestriction(country1, country2, date, FlightRestriction.RestrictionType.Flights)
                   || HasRestriction(country2, country1, date, FlightRestriction.RestrictionType.Flights);
        }

       
        //returns if there is flight restrictions for airlines to one of the destinations
        public static Boolean HasRestriction(Airline airline, Country dest1, Country dest2, DateTime date)
        {
            var dest = airline.Profile.Country == dest1 ? dest2 : dest1;

            if (GetRestrictions(dest, date, airline, FlightRestriction.RestrictionType.AllowAirline).Count > 0)
                return false;

            if (GetRestrictions(dest, date, airline, FlightRestriction.RestrictionType.Airline).Count > 0)
                return true;

            var dest1Restriction = HasRestriction(airline.Profile.Country, dest2, date, FlightRestriction.RestrictionType.Airlines);
            var dest2Restriction = HasRestriction(airline.Profile.Country, dest1, date, FlightRestriction.RestrictionType.Airlines);

            return dest1Restriction
                   || dest2Restriction;
        }
        public static Boolean IsAllowed(Airport airport1, Airport airport2, DateTime date)
        {
            var restrictionsAirport1 = GetRestrictions().Where(r => r.Airport != null && r.Airport == airport1 && (date >= r.StartDate && date <= r.EndDate) && r.Type == FlightRestriction.RestrictionType.AllowAirport);
            var restrictionsAirport2 = GetRestrictions().Where(r => r.Airport != null && r.Airport == airport2 && (date >= r.StartDate && date <= r.EndDate) && r.Type == FlightRestriction.RestrictionType.AllowAirport);

            if (restrictionsAirport1.Count() == 0 && restrictionsAirport2.Count() == 0)
                return true;

            if (restrictionsAirport1.Count() > 0)
            {
                return restrictionsAirport1.Any(r => r.To == airport2.Profile.Country);
            }

            if (restrictionsAirport2.Count() > 0)
            {
                return restrictionsAirport2.Any(r => r.To == airport1.Profile.Country);
     
            }

            return true;
        }
        //returns if an airline is allowed
        public static Boolean IsAllowed(Airline airline, BaseUnit to, DateTime date)
        {
            Country from = new CountryCurrentCountryConverter().Convert(airline.Profile.Country) as Country;
            Country toCountry = new CountryCurrentCountryConverter().Convert(to) as Country;

            //var aircraftRestrictions = restrictions.Where(r => r.Type == type);

            // var fromRestrictions = aircraftRestrictions.Where(r => (r.From == from || (r.From is Union && ((Union)r.From).isMember(from, date))));
            // var toRestrictions = aircraftRestrictions.Where(r => (r.To == to || (r.To is Union && ((Union)r.To).isMember(to, date))));
           // fjern iairport AirlineLogo 041
            var rs = GetRestrictions().FindAll(r=>date >= r.StartDate && date <= r.EndDate && r.Type == FlightRestriction.RestrictionType.AllowAirline);
            
            var restrictions = 
                GetRestrictions()
                    .FindAll(
                        r =>
                            r.Airline != null && r.Airline == airline
                            && (r.To == to || (r.To is Union && ((Union)r.To).isMember(toCountry, date)))
                            && (date >= r.StartDate && date <= r.EndDate) && r.Type == FlightRestriction.RestrictionType.AllowAirline);

     
            return restrictions.Count > 0;
        }
        #endregion
    }
}