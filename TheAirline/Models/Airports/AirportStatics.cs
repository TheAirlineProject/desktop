﻿using System;
using System.Collections.Generic;
using System.Linq;
using TheAirline.Helpers;
using TheAirline.Models.Airliners;
using TheAirline.Models.Passengers;

namespace TheAirline.Models.Airports
{
    //some static values for an airport
    public class AirportStatics
    {
        #region Fields

        private readonly Dictionary<Airport, double> _airportDistances;

        private readonly List<DestinationDemand> _cargoDemand;

        private readonly List<DestinationDemand> _passengerDemand;

        #endregion

        #region Constructors and Destructors

        public AirportStatics(Models.Airports.Airport airport)
        {
            _airportDistances = new Dictionary<Airport, double>();
            _passengerDemand = new List<DestinationDemand>();
            _cargoDemand = new List<DestinationDemand>();
            Airport = airport;
        }

        #endregion

        #region Public Properties

        public Models.Airports.Airport Airport { get; set; }

        #endregion

        #region Public Methods and Operators

        public void AddCargoDemand(DestinationDemand demand)
        {
            lock (_cargoDemand) _cargoDemand.Add(demand);
        }

        //adds a distance to the airport
        public void AddDistance(Models.Airports.Airport airport, double distance)
        {
            lock (_airportDistances)
            {
                if (!_airportDistances.ContainsKey(airport))
                {
                    _airportDistances.Add(airport, distance);
                }
            }
        }

        public void AddPassengerDemand(DestinationDemand demand)
        {
            lock (_passengerDemand) _passengerDemand.Add(demand);
        }

        public List<Airport> GetAirportsWithin(double range)
        {
            List<Airport> airports;
            lock (_airportDistances)
            {
                if (_airportDistances.Count == 0)
                {
                    foreach (Models.Airports.Airport airport in Models.Airports.Airports.GetAllAirports())
                    {
                        AddDistance(airport, MathHelpers.GetDistance(Airport, airport));
                    }
                }
                airports = new List<Airport>(from a in _airportDistances where a.Value <= range select a.Key);
            }

            return airports;
        }

        public List<DestinationDemand> GetDemands()
        {
            var demands = new List<DestinationDemand>();

            demands.AddRange(_cargoDemand);
            demands.AddRange(_passengerDemand);

            return demands;
        }

        //returns if the airport have passenger demand to another airport

        //returns the destination cargo for a specific destination
        public ushort GetDestinationCargoRate(Models.Airports.Airport destination)
        {
            DestinationDemand cargo = _cargoDemand.Find(a => a.Destination == destination.Profile.IATACode);

            if (cargo == null)
            {
                return 0;
            }
            return cargo.Rate;
        }

        //returns the destination passengers for a specific destination for a class
        public ushort GetDestinationPassengersRate(Models.Airports.Airport destination, AirlinerClass.ClassType type)
        {
            DestinationDemand pax = _passengerDemand.Find(a => a.Destination == destination.Profile.IATACode);

            Array values = Enum.GetValues(typeof (AirlinerClass.ClassType));

            int classFactor = 0;

            int i = 1;

            foreach (AirlinerClass.ClassType value in values)
            {
                if (value == type)
                {
                    classFactor = i;
                }
                i++;
            }

            if (pax == null)
            {
                return 0;
            }
            return (ushort) (pax.Rate/classFactor);
        }

        public int GetDestinationPassengersSum()
        {
            int sum;
            lock (_passengerDemand)
            {
                sum = _passengerDemand.Sum(p => p.Rate);
            }
            return sum;
        }

        //returns all demands

        //returns the distance for an airport
        public double GetDistance(Models.Airports.Airport airport)
        {
            double distance;
            lock (_airportDistances)
            {
                distance = _airportDistances.ContainsKey(airport) ? _airportDistances[airport] : 0;
            }

            return distance;
        }

        public bool HasDestinationCargoRate(Models.Airports.Airport destination)
        {
            return _cargoDemand.Exists(a => a.Destination == destination.Profile.IATACode);
        }

        public bool HasDestinationPassengersRate(Models.Airports.Airport destination)
        {
            return _passengerDemand.Exists(a => a.Destination == destination.Profile.IATACode);
        }

        #endregion

        //adds passenger demand to the airport

        //adds cargo demand to the airport

        //returns all airports within a range
    }
}