﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using TheAirline.Model.AirlineModel;
using TheAirline.Model.AirlinerModel;
using TheAirline.Model.AirportModel;

namespace TheAirline.Model.GeneralModel.ScenarioModel
{
    //the class for a scenario
    [Serializable]
    public class Scenario : ISerializable
    {
        [Versioning("name")]
        public string Name { get; set; }
        [Versioning("airline")]
        public Airline Airline { get; set; }
        [Versioning("homebase")]
        public Airport Homebase { get; set; }
        [Versioning("startyear")]
        public int StartYear { get; set; }
        [Versioning("startcash")]
        public long StartCash { get; set; }
        [Versioning("description")]
        public string Description { get; set; }
        [Versioning("opponents")]
        public List<ScenarioAirline> OpponentAirlines { get; set; }
        [Versioning("destinations")]
        public List<Airport> Destinations { get; set; }
        [Versioning("fleet")]
        public Dictionary<AirlinerType, int> Fleet { get; set; }
        [Versioning("difficulty")]
        public DifficultyLevel Difficulty { get; set; }
        [Versioning("routes")]
        public List<ScenarioAirlineRoute> Routes { get; set; }
        [Versioning("failures")]
        public List<ScenarioFailure> Failures { get; set; }
        [Versioning("demands")]
        public List<ScenarioPassengerDemand> PassengerDemands { get; set; }
        [Versioning("endyear")]
        public int EndYear { get; set; }
        public Scenario(string name,string description, Airline airline, Airport homebase, int startyear, int endyear, long startcash,DifficultyLevel difficulty)
        {
            this.Name = name;
            this.Airline = airline;
            this.Homebase = homebase;
            this.StartYear = startyear;
            this.StartCash = startcash;
            this.Description = description;
            this.Difficulty = difficulty;
            this.EndYear = endyear;
            this.OpponentAirlines = new List<ScenarioAirline>();
            this.Destinations = new List<Airport>();
            this.Fleet = new Dictionary<AirlinerType, int>();
            this.Routes = new List<ScenarioAirlineRoute>();
            this.Failures = new List<ScenarioFailure>();
            this.PassengerDemands = new List<ScenarioPassengerDemand>();
        }
        //adds a passenger demand to the scenario
        public void addPassengerDemand(ScenarioPassengerDemand demand)
        {
            this.PassengerDemands.Add(demand);
        }
        //adds a route to the scenario
        public void addRoute(ScenarioAirlineRoute route)
        {
            this.Routes.Add(route);
        }
        //adds an airliner type with a quantity to the scenario
        public void addFleet(AirlinerType type, int quantity)
        {
            this.Fleet.Add(type, quantity);
        }
        //adds a destinaton to the scenario
        public void addDestination(Airport destination)
        {
            this.Destinations.Add(destination);
        }
        //adds an opponent airline to the scenario
        public void addOpponentAirline(ScenarioAirline airline)
        {
            this.OpponentAirlines.Add(airline);
        }
        //adds a failure to the scenario
        public void addScenarioFailure(ScenarioFailure failure)
        {
            this.Failures.Add(failure);
        }
             private Scenario(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt16("version");

            var fields = this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p => p.GetCustomAttribute(typeof(Versioning)) != null);

            IList<PropertyInfo> props = new List<PropertyInfo>(this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p => p.GetCustomAttribute(typeof(Versioning)) != null));

            var propsAndFields = props.Cast<MemberInfo>().Union(fields.Cast<MemberInfo>());

            foreach (SerializationEntry entry in info)
            {
                MemberInfo prop = propsAndFields.FirstOrDefault(p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Name == entry.Name);


                if (prop != null)
                {
                    if (prop is FieldInfo)
                        ((FieldInfo)prop).SetValue(this, entry.Value);
                    else
                        ((PropertyInfo)prop).SetValue(this, entry.Value);
                }
            }

            var notSetProps = propsAndFields.Where(p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Version > version);

            foreach (MemberInfo notSet in notSetProps)
            {
                Versioning ver = (Versioning)notSet.GetCustomAttribute(typeof(Versioning));

                if (ver.AutoGenerated)
                {
                    if (notSet is FieldInfo)
                        ((FieldInfo)notSet).SetValue(this, ver.DefaultValue);
                    else
                        ((PropertyInfo)notSet).SetValue(this, ver.DefaultValue);

                }

            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", 1);

            Type myType = this.GetType();

            var fields = myType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p => p.GetCustomAttribute(typeof(Versioning)) != null);

            IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p => p.GetCustomAttribute(typeof(Versioning)) != null));

            var propsAndFields = props.Cast<MemberInfo>().Union(fields.Cast<MemberInfo>());

            foreach (MemberInfo member in propsAndFields)
            {
                object propValue;

                if (member is FieldInfo)
                    propValue = ((FieldInfo)member).GetValue(this);
                else
                    propValue = ((PropertyInfo)member).GetValue(this, null);

                Versioning att = (Versioning)member.GetCustomAttribute(typeof(Versioning));

                info.AddValue(att.Name, propValue);
            }
        }


    }
    //the list of scenario
    public class Scenarios
    {
        private static List<Scenario> scenarios = new List<Scenario>();
        //adds a scenario to the list
        public static void AddScenario(Scenario scenario)
        {
            scenarios.Add(scenario);
        }
        //returns all scenarios
        public static List<Scenario> GetScenarios()
        {
            return scenarios;
        }
        //returns a scenario with a name
        public static Scenario GetScenario(string name)
        {
            return scenarios.Find(s => s.Name == name);
        }
        //clears the list of scenarios
        public static void Clear()
        {
            scenarios.Clear();
        }
        //returns the number of scenarios
        public static int GetNumberOfScenarios()
        {
            return scenarios.Count;
        }
    }
}
