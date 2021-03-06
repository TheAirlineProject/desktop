﻿using System;
using System.Collections.Generic;
using System.Linq;
using TheAirline.GUIModel.HelpersModel;
using TheAirline.Models.Airliners;
using TheAirline.Models.Airlines;
using TheAirline.Models.Airports;
using TheAirline.Models.General;
using TheAirline.Models.General.Countries;
using TheAirline.Models.General.Environment;
using TheAirline.Models.Passengers;
using TheAirline.Models.Routes;

namespace TheAirline.Helpers
{
    //the class for some general airport helpers
    public class AirportHelpers
    {
        #region Static Fields

        private static readonly Random Rnd = new Random();

        #endregion

        #region Public Methods and Operators

        //returns the fuel price for an airport
        public static double GetFuelPrice(Airport airport)
        {
            return GetFuelPrice(airport.Profile.Country.Region);
        }

        public static double GetFuelPrice(Region region)
        {
            return region.FuelIndex*GameObject.GetInstance().FuelPrice;
        }

        //returns the standard landing fee for an airport
        public static double GetStandardLandingFee(Airport airport)
        {
            const double basefee = 0.27;

            return basefee*((int) (airport.Profile.Size) + 1);
        }

        //returns the landing fee for an airliner at an airport
        public static double GetLandingFee(Airport airport, Airliner airliner)
        {
            return (airliner.Type.Weight/1000)*GeneralHelpers.GetInflationPrice(airport.LandingFee);
        }

        public static void AddAirlineContract(AirportContract contract)
        {
            contract.Airport.AddAirlineContract(contract);

            AirportFacility checkinFacility =
                AirportFacilities.GetFacilities(AirportFacility.FacilityType.CheckIn).First(f => f.TypeLevel == 1);
            AirportFacility ticketFacility =
                AirportFacilities.GetFacilities(AirportFacility.FacilityType.TicketOffice).First(f => f.TypeLevel == 1);
            AirportFacility serviceFacility =
                AirportFacilities.GetFacilities(AirportFacility.FacilityType.Service).First(f => f.TypeLevel == 1);
            AirportFacility cargoTerminal =
                AirportFacilities.GetFacilities(AirportFacility.FacilityType.Cargo).Find(f => f.TypeLevel > 0);

            if (contract.Type == AirportContract.ContractType.FullService)
            {
                if (
                    contract.Airport.GetAirlineAirportFacility(null, AirportFacility.FacilityType.Cargo)
                            .Facility.TypeLevel < cargoTerminal.TypeLevel)
                {
                    contract.Airport.AddAirportFacility(null, cargoTerminal, GameObject.GetInstance().GameTime);
                }

                if (
                    contract.Airport.GetAirlineAirportFacility(null, AirportFacility.FacilityType.CheckIn)
                            .Facility.TypeLevel < checkinFacility.TypeLevel)
                {
                    contract.Airport.AddAirportFacility(null, checkinFacility, GameObject.GetInstance().GameTime);
                }

                if (
                    contract.Airport.GetAirlineAirportFacility(null, AirportFacility.FacilityType.TicketOffice)
                            .Facility.TypeLevel < ticketFacility.TypeLevel)
                {
                    contract.Airport.AddAirportFacility(null, ticketFacility, GameObject.GetInstance().GameTime);
                }

                if (
                    contract.Airport.GetAirlineAirportFacility(null, AirportFacility.FacilityType.Service)
                            .Facility.TypeLevel < serviceFacility.TypeLevel)
                {
                    contract.Airport.AddAirportFacility(null, serviceFacility, GameObject.GetInstance().GameTime);
                }
            }
            if (contract.Type == AirportContract.ContractType.MediumService)
            {
                if (
                    contract.Airport.GetAirlineAirportFacility(null, AirportFacility.FacilityType.CheckIn)
                            .Facility.TypeLevel < checkinFacility.TypeLevel)
                {
                    contract.Airport.AddAirportFacility(null, checkinFacility, GameObject.GetInstance().GameTime);
                }

                if (
                    contract.Airport.GetAirlineAirportFacility(null, AirportFacility.FacilityType.TicketOffice)
                            .Facility.TypeLevel < ticketFacility.TypeLevel)
                {
                    contract.Airport.AddAirportFacility(null, ticketFacility, GameObject.GetInstance().GameTime);
                }
            }
            if (contract.Type == AirportContract.ContractType.LowService)
            {
                if (
                    contract.Airport.GetAirlineAirportFacility(null, AirportFacility.FacilityType.CheckIn)
                            .Facility.TypeLevel < checkinFacility.TypeLevel)
                {
                    contract.Airport.AddAirportFacility(null, checkinFacility, GameObject.GetInstance().GameTime);
                }
            }
        }

        public static bool CanFillRoutesEntries(
            Airport airport,
            Airline airline,
            List<AirportContract> contracts,
            Weather.Season season)
        {
            List<Route> routes = GetAirportRoutes(airport, airline);

            int numberOfOccupiedSlots =
                GetOccupiedSlotTimes(airport, airline, contracts, season)
                    .GroupBy(s => s.Ticks).Count(x => x.Count() > 1);
            return numberOfOccupiedSlots == 0 && !(routes.Count > 0 && contracts.Count == 0);
        }

        public static void CheckForExtendGates(Airport airport)
        {
            const int minYearsBetweenExpansions = 5;

            if (airport.Terminals.GetOrdereredGates() == 0
                && GameObject.GetInstance().GameTime.AddYears(-minYearsBetweenExpansions) > airport.LastExpansionDate)
            {
                Terminal minTerminal = airport.Terminals.AirportTerminals.OrderBy(t => t.Gates.NumberOfGates).First();

                bool newTerminal = minTerminal.Gates.NumberOfGates > 50;
                //extend existing
                if (!newTerminal)
                {
                    int numberOfGates = Math.Max(5, minTerminal.Gates.NumberOfGates);
                    int daysToBuild = numberOfGates*10 + (newTerminal ? 60 : 0);

                    long price = numberOfGates*airport.GetTerminalGatePrice()
                                 + (newTerminal ? airport.GetTerminalPrice() : 0);
                    price = price/3*4;

                    if (airport.Income > price)
                    {
                        for (int i = 0; i < numberOfGates; i++)
                        {
                            var gate = new Gate(GameObject.GetInstance().GameTime.AddDays(daysToBuild)) {Airline = minTerminal.Airline};

                            minTerminal.Gates.AddGate(gate);
                        }

                        airport.Income -= price;
                        airport.LastExpansionDate = GameObject.GetInstance().GameTime;
                    }
                }
                    //build new terminal
                else
                {
                    int numberOfGates = airport.Terminals.GetTerminals()[0].Gates.NumberOfDeliveredGates;

                    int daysToBuild = numberOfGates*10 + (newTerminal ? 60 : 0);

                    long price = numberOfGates*airport.GetTerminalGatePrice()
                                 + (newTerminal ? airport.GetTerminalPrice() : 0);
                    price = price/3*4;

                    if (airport.Income > price)
                    {
                        var terminal = new Terminal(
                            airport,
                            null,
                            "Terminal",
                            numberOfGates,
                            GameObject.GetInstance().GameTime.AddDays(daysToBuild),
                            Terminal.TerminalType.Passenger);

                        airport.AddTerminal(terminal);
                        airport.Income -= price;
                        airport.LastExpansionDate = GameObject.GetInstance().GameTime;
                    }
                }
            }
        }

        public static void CheckForExtendRunway(Airport airport)
        {
            bool isOnlyHeliport = !airport.Runways.Exists(r => r.Type == Runway.RunwayType.Regular);

            if (!isOnlyHeliport)
            {
                const int minYearsBetweenExpansions = 5;

                long maxRunwayLenght = (from r in airport.Runways select r.Length).Max();
                long longestRequiredRunwayLenght =
                    AirlinerTypes.GetTypes(
                        a =>
                        a.Produced.From <= GameObject.GetInstance().GameTime
                        && a.Produced.To >= GameObject.GetInstance().GameTime).Max(a => a.MinRunwaylength);

                List<Route> airportRoutes = GetAirportRoutes(airport);
                IEnumerable<FleetAirliner> routeAirliners = airportRoutes.SelectMany(r => r.GetAirliners());

                var fleetAirliners = routeAirliners as FleetAirliner[] ?? routeAirliners.ToArray();
                long longestRunwayInUse = fleetAirliners.Any()
                                              ? fleetAirliners.Max(a => a.Airliner.MinRunwaylength)
                                              : 0;

                if (maxRunwayLenght < longestRequiredRunwayLenght/2 && maxRunwayLenght < longestRunwayInUse*3/4
                    && GameObject.GetInstance().GameTime.AddYears(-minYearsBetweenExpansions) > airport.LastExpansionDate)
                {
                    List<string> runwayNames =
                        (from r in Airports.GetAllAirports().SelectMany(a => a.Runways) select r.Name).Distinct().ToList();

                    foreach (Runway r in airport.Runways)
                    {
                        runwayNames.Remove(r.Name);
                    }

                    Runway.SurfaceType surface = airport.Runways[0].Surface;
                    long length = Math.Min(longestRequiredRunwayLenght*3/4, longestRunwayInUse*2);

                    var runway = new Runway(
                        runwayNames[Rnd.Next(runwayNames.Count)],
                        length,
                        Runway.RunwayType.Regular,
                        surface,
                        GameObject.GetInstance().GameTime.AddDays(90),
                        false);
                    airport.Runways.Add(runway);

                    airport.LastExpansionDate = GameObject.GetInstance().GameTime;
                }
            }
        }

        //returns the longest distance between airports in a match

        public static void ClearAirportStatistics()
        {
            foreach (Airport airport in Airports.GetAllAirports())
            {
                airport.Statistics.Stats.Clear();
            }
        }

        public static GeneralHelpers.Size ConvertAirportPaxToSize(double size)
        {
            var yearCoeffs = new Dictionary<int, double> {{1960, 1.3}, {1970, 1.2}, {1980, 1.15}, {1990, 1.10}, {2000, 1.0658}, {2010, 1}};

            int decade = (GameObject.GetInstance().GameTime.Year - 1960)/10*10 + 1960;

            double coeff = 1;

            if (yearCoeffs.ContainsKey(decade))
            {
                coeff = yearCoeffs[decade];
            }

            double coeffPax = coeff*size;

            if (coeffPax > 32000)
            {
                return GeneralHelpers.Size.Largest;
            }
            if (coeffPax > 16000)
            {
                return GeneralHelpers.Size.VeryLarge;
            }
            if (coeffPax > 9000)
            {
                return GeneralHelpers.Size.Large;
            }
            if (coeffPax > 3000)
            {
                return GeneralHelpers.Size.Medium;
            }
            if (coeffPax > 535)
            {
                return GeneralHelpers.Size.Small;
            }
            if (coeffPax > 160)
            {
                return GeneralHelpers.Size.VerySmall;
            }

            return GeneralHelpers.Size.Smallest;
        }

        //returns all entries for a specific airport with take off in a time span for a day

        //creates the weather for an airport
        public static void CreateAirportWeather(Airport airport)
        {
            airport.Weather[0] = null;

            WeatherAverage average =
                (WeatherAverages.GetWeatherAverages(
                    w => w.Airport != null && w.Airport == airport && w.Month == GameObject.GetInstance().GameTime.Month)
                                .FirstOrDefault() ?? WeatherAverages.GetWeatherAverages(
                                    w =>
                                    w.Town != null && w.Town == airport.Profile.Town
                                    && w.Month == GameObject.GetInstance().GameTime.Month).FirstOrDefault()) ?? WeatherAverages.GetWeatherAverages(
                                        w =>
                                        w.Country != null && w.Country == airport.Profile.Town.Country
                                        && w.Month == GameObject.GetInstance().GameTime.Month).FirstOrDefault();

            if (average == null)
            {
                CreateFiveDaysAirportWeather(airport);
            }
            else
            {
                var lAirport = new List<Airport> {airport};

                CreateAirportsWeather(lAirport, average);
            }
        }

        //creates the weather (5 days) for a number of airport with an average
        public static void CreateAirportsWeather(List<Airport> airports, WeatherAverage average)
        {
            if (airports.Count > 0)
            {
                const int maxDays = 5;
                var weathers = new Weather[maxDays];

                if (airports[0].Weather[0] == null)
                {
                    for (int i = 0; i < maxDays; i++)
                    {
                        weathers[i] = CreateDayWeather(
                            GameObject.GetInstance().GameTime.AddDays(i),
                            i > 0 ? weathers[i - 1] : null,
                            average);
                    }
                }
                else
                {
                    for (int i = 1; i < maxDays; i++)
                    {
                        weathers[i - 1] = airports[0].Weather[i];
                    }

                    weathers[maxDays - 1] = CreateDayWeather(
                        GameObject.GetInstance().GameTime.AddDays(maxDays - 1),
                        weathers[maxDays - 2],
                        average);
                }

                foreach (Airport airport in airports)
                {
                    airport.Weather = weathers;
                }
            }
        }

        //creates the weather (5 days) for an airport
        public static void CreateFiveDaysAirportWeather(Airport airport)
        {
            const int maxDays = 5;
            if (airport.Weather[0] == null)
            {
                for (int i = 0; i < maxDays; i++)
                {
                    airport.Weather[i] = CreateDayWeather(
                        airport,
                        GameObject.GetInstance().GameTime.AddDays(i),
                        i > 0 ? airport.Weather[i - 1] : null);
                }
            }
            else
            {
                for (int i = 1; i < maxDays; i++)
                {
                    airport.Weather[i - 1] = airport.Weather[i];
                }

                airport.Weather[maxDays - 1] = CreateDayWeather(
                    airport,
                    GameObject.GetInstance().GameTime.AddDays(maxDays - 1),
                    airport.Weather[maxDays - 2]);
            }
        }

        //creates a new weather object for a specific date based on the weather for another day

        //returns the price for a contract at an airport
        public static double GetAirportContractPrice(Airport airport)
        {
            double paxDemand = airport.Profile.MajorDestionations.Sum(d => d.Value) + airport.Profile.Pax;

            const double basePrice = 10000;

            return GeneralHelpers.GetInflationPrice(paxDemand*basePrice);

            //(initial amount, in the millions; and a montly amount probably $50,000 or so
        }

        public static List<RouteTimeTableEntry> GetAirportLandings(
            Airport airport,
            DayOfWeek day,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            return
                GetAirportRoutes(airport)
                    .SelectMany(
                        r =>
                        r.TimeTable.Entries.FindAll(
                            e =>
                            e.Airliner != null && e.Destination.Airport == airport
                            && e.Time.Add(
                                MathHelpers.GetFlightTime(
                                    e.Destination.Airport.Profile.Coordinates.ConvertToGeoCoordinate(),
                                    e.DepartureAirport.Profile.Coordinates.ConvertToGeoCoordinate(),
                                    e.Airliner.Airliner.Type)) >= startTime
                            && e.Time.Add(
                                MathHelpers.GetFlightTime(
                                    e.Destination.Airport.Profile.Coordinates.ConvertToGeoCoordinate(),
                                    e.DepartureAirport.Profile.Coordinates.ConvertToGeoCoordinate(),
                                    e.Airliner.Airliner.Type)) < endTime && e.Day == day))
                    .ToList();
        }

        public static List<Route> GetAirportRoutes(Airport airport, Airline airline)
        {
            return airline.Routes.FindAll(r => r.Destination2 == airport || r.Destination1 == airport);
        }

        public static List<Route> GetAirportRoutes(Airport airport)
        {
            IEnumerable<Route> routes =
                Airlines.GetAllAirlines()
                        .SelectMany(a => a.Routes)
                        .Where(r => r.Destination1 == airport || r.Destination2 == airport);

            return routes.ToList();
        }

        public static List<Route> GetAirportRoutes(Airport airport1, Airport airport2)
        {
            var routes = new List<Route>(Airlines.GetAllAirlines().SelectMany(a => a.Routes));

            return
                routes.Where(
                    r =>
                    (r.Destination1 == airport1 && r.Destination2 == airport2)
                    || (r.Destination1 == airport2 && r.Destination2 == airport1)).ToList();
        }

        public static double GetAirportRunwayPrice(Airport airport, long lenght)
        {
            double pricePerMeter = 0;
            if (airport.Profile.Size == GeneralHelpers.Size.VeryLarge
                || airport.Profile.Size == GeneralHelpers.Size.Largest)
            {
                pricePerMeter = 30000;
            }
            if (airport.Profile.Size == GeneralHelpers.Size.Large || airport.Profile.Size == GeneralHelpers.Size.Medium)
            {
                pricePerMeter = 24000;
            }
            if (airport.Profile.Size == GeneralHelpers.Size.Small)
            {
                pricePerMeter = 18000;
            }
            if (airport.Profile.Size == GeneralHelpers.Size.Smallest
                || airport.Profile.Size == GeneralHelpers.Size.VerySmall)
            {
                pricePerMeter = 12000;
            }

            return pricePerMeter*lenght;
        }

        public static List<RouteTimeTableEntry> GetAirportTakeoffs(
            Airport airport,
            DayOfWeek day,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            return
                GetAirportRoutes(airport)
                    .SelectMany(
                        r =>
                        r.TimeTable.Entries.FindAll(
                            e =>
                            e.Airliner != null && e.DepartureAirport == airport && e.Time >= startTime
                            && e.Time < endTime && e.Day == day))
                    .ToList();
        }

        public static List<Airport> GetAirportsNearAirport(Airport airport, double distance)
        {
            return airport.Statics.GetAirportsWithin(distance);
        }

        public static double GetHubPrice(Airport airport, HubType type)
        {
            double price = type.Price;

            price = price + 25000*((int) airport.Profile.Size);
            return Convert.ToInt64(GeneralHelpers.GetInflationPrice(price));
        }

        public static double GetLongestDistance(Predicate<Airport> match)
        {
            List<Airport> airports1 = Airports.GetAllAirports(match);
            List<Airport> airports2 = Airports.GetAllAirports(match);

            double maxDistance = double.MinValue;

            foreach (Airport airport1 in airports1)
            {
                double distance = airports2.Where(a => a != airport1).Max(a => MathHelpers.GetDistance(a, airport1));

                if (distance >= maxDistance)
                {
                    maxDistance = distance;
                }
            }

            return maxDistance;
        }

        public static int GetNumberOfAirportsRoutes(Airport airport1, Airport airport2)
        {
            var routes = new List<Route>(Airlines.GetAllAirlines().SelectMany(a => a.Routes));

            return
                routes.Count(
                    r =>
                    (r.Destination1 == airport1 && r.Destination2 == airport2)
                    || (r.Destination1 == airport2 && r.Destination2 == airport1));
        }

        //checks an airport for extending of runway

        //returns all occupied slot times for an airline at an airport (15 minutes slots)
        public static List<TimeSpan> GetOccupiedSlotTimes(
            Airport airport,
            Airline airline,
            List<AirportContract> contracts,
            Weather.Season season)
        {
            var occupiedSlots = new List<KeyValuePair<Route, TimeSpan>>();

            var gateTimeBefore = new TimeSpan(0, 15, 0);
            var gateTimeAfter = new TimeSpan(0, 15, 0);

            int gates = contracts.Sum(c => c.NumberOfGates);

            var routes = new List<Route>(GetAirportRoutes(airport, airline));

            var entries =
                new List<RouteTimeTableEntry>(
                    routes.Where(r => season == Weather.Season.AllYear || season == r.Season)
                          .SelectMany(r => r.TimeTable.Entries));

            foreach (RouteTimeTableEntry entry in entries)
            {
                var entryTakeoffTime = new TimeSpan(
                    (int) entry.Day,
                    entry.Time.Hours,
                    entry.Time.Minutes,
                    entry.Time.Seconds);
                TimeSpan entryLandingTime =
                    entryTakeoffTime.Add(entry.TimeTable.Route.GetFlightTime(entry.Airliner.Airliner.Type));

                if (entryLandingTime.Days > 6)
                {
                    entryLandingTime = new TimeSpan(
                        0,
                        entryLandingTime.Hours,
                        entryLandingTime.Minutes,
                        entryLandingTime.Seconds);
                }

                if (entry.DepartureAirport == airport)
                {
                    TimeSpan entryStartTakeoffTime = entryTakeoffTime.Subtract(gateTimeBefore);
                    TimeSpan entryEndTakeoffTime = entryTakeoffTime.Add(gateTimeAfter);

                    var tTakeoffTime = new TimeSpan(
                        entryStartTakeoffTime.Days,
                        entryStartTakeoffTime.Hours,
                        (entryStartTakeoffTime.Minutes/15)*15,
                        0);

                    while (tTakeoffTime < entryEndTakeoffTime)
                    {
                        if (!occupiedSlots.Exists(s => s.Key == entry.TimeTable.Route && s.Value == tTakeoffTime))
                        {
                            occupiedSlots.Add(new KeyValuePair<Route, TimeSpan>(entry.TimeTable.Route, tTakeoffTime));
                        }
                        tTakeoffTime = tTakeoffTime.Add(new TimeSpan(0, 15, 0));
                    }
                }

                if (entry.DepartureAirport != airport)
                {
                    TimeSpan entryStartLandingTime = entryLandingTime.Subtract(gateTimeBefore);
                    TimeSpan entryEndLandingTime = entryLandingTime.Add(gateTimeAfter);

                    var tLandingTime = new TimeSpan(
                        entryStartLandingTime.Days,
                        entryStartLandingTime.Hours,
                        (entryStartLandingTime.Minutes/15)*15,
                        0);

                    while (tLandingTime < entryEndLandingTime)
                    {
                        if (!occupiedSlots.Exists(s => s.Key == entry.TimeTable.Route && s.Value == tLandingTime))
                        {
                            occupiedSlots.Add(new KeyValuePair<Route, TimeSpan>(entry.TimeTable.Route, tLandingTime));
                        }
                        tLandingTime = tLandingTime.Add(new TimeSpan(0, 15, 0));
                    }
                }
            }

            var slots = (from s in occupiedSlots group s.Value by s.Value into g select new {Time = g.Key, Slots = g});

            return slots.Where(s => s.Slots.Count() >= gates).SelectMany(s => s.Slots).ToList();
        }

        public static List<TimeSpan> GetOccupiedSlotTimes(Airport airport, Airline airline, Weather.Season season, Terminal.TerminalType type)
        {
            return GetOccupiedSlotTimes(
                airport,
                airline,
                airport.AirlineContracts.Where(c => c.Airline == airline && c.TerminalType == type).ToList(),
                season);
        }

        public static double GetShortestDistance(Predicate<Airport> match)
        {
            List<Airport> airports1 = Airports.GetAllAirports(match);
            List<Airport> airports2 = Airports.GetAllAirports(match);

            double minDistance = double.MaxValue;

            foreach (Airport airport1 in airports1)
            {
                double distance = airports2.Where(a => a != airport1).Max(a => MathHelpers.GetDistance(a, airport1));

                if (distance <= minDistance)
                {
                    minDistance = distance;
                }
            }

            return minDistance;
        }

        //sets the airport expansion to an airport
        public static void SetAirportExpansion(Airport airport, AirportExpansion expansion, bool onStartUp = false)
        {
            if (expansion.Type == AirportExpansion.ExpansionType.Name)
            {
                if (expansion.NotifyOnChange && !onStartUp)
                {
                    GameObject.GetInstance()
                              .NewsBox.AddNews(
                                  new News(
                                      News.NewsType.AirportNews,
                                      GameObject.GetInstance().GameTime,
                                      "Airport Name Changed",
                                      $"[LI airport={airport.Profile.IATACode}]({new AirportCodeConverter().Convert(airport)}) has changed its name to {expansion.Name}"));
                }

                airport.Profile.Name = expansion.Name;
            }
            if (expansion.Type == AirportExpansion.ExpansionType.NewRunway)
            {
                var runway = new Runway(expansion.Name, expansion.Length, Runway.RunwayType.Regular, expansion.Surface, expansion.Date, true);
                airport.Runways.Add(runway);

                if (expansion.NotifyOnChange && !onStartUp)
                {
                    GameObject.GetInstance()
                              .NewsBox.AddNews(
                                  new News(
                                      News.NewsType.AirportNews,
                                      GameObject.GetInstance().GameTime,
                                      "New Runway",
                                      $"[LI airport={airport.Profile.IATACode}]({new AirportCodeConverter().Convert(airport)}) has created a new runway"));
                }
            }
            if (expansion.Type == AirportExpansion.ExpansionType.RunwayLength)
            {
                Runway runway = airport.Runways.FirstOrDefault(r => r.Name == expansion.Name);

                if (runway != null)
                {
                    if (expansion.NotifyOnChange && !onStartUp)
                    {
                        GameObject.GetInstance()
                                  .NewsBox.AddNews(
                                      new News(
                                          News.NewsType.AirportNews,
                                          GameObject.GetInstance().GameTime,
                                          "New Terminal",
                                          $"[LI airport={airport.Profile.IATACode}]({new AirportCodeConverter().Convert(airport)}) has changed the length of the runway {expansion.Name} to {new SmallDistanceToUnitConverter().Convert(expansion.Length, null, null, null)}"));
                    }
                }
            }
            if (expansion.Type == AirportExpansion.ExpansionType.NewTerminal)
            {
                var terminal = new Terminal(airport, expansion.Name, expansion.Gates, expansion.Date, expansion.TerminalType);
                airport.AddTerminal(terminal);

                if (expansion.NotifyOnChange && !onStartUp)
                {
                    GameObject.GetInstance()
                              .NewsBox.AddNews(
                                  new News(
                                      News.NewsType.AirportNews,
                                      GameObject.GetInstance().GameTime,
                                      "New Terminal",
                                      $"[LI airport={airport.Profile.IATACode}]({new AirportCodeConverter().Convert(airport)}) has created a new terminal with {expansion.Gates} gates"));
                }
            }
            if (expansion.Type == AirportExpansion.ExpansionType.ExtraGates)
            {
                Terminal terminal = airport.Terminals.AirportTerminals.FirstOrDefault(t => t.Name == expansion.Name);

                if (terminal != null)
                {
                    for (int i = 0; i < expansion.Gates; i++)
                        terminal.Gates.AddGate(new Gate(expansion.Date));

                    if (expansion.NotifyOnChange && !onStartUp)
                    {
                        GameObject.GetInstance()
                                  .NewsBox.AddNews(
                                      new News(
                                          News.NewsType.AirportNews,
                                          GameObject.GetInstance().GameTime,
                                          "New Gates at Airport",
                                          $"[LI airport={airport.Profile.IATACode}]({new AirportCodeConverter().Convert(airport)}) has created {expansion.Gates} gates in {expansion.Name}"));
                    }
                }
            }
            if (expansion.Type == AirportExpansion.ExpansionType.CloseTerminal)
            {
                Terminal terminal = airport.Terminals.AirportTerminals.FirstOrDefault(t => t.Name == expansion.Name);

                if (terminal != null)
                {
                    airport.RemoveTerminal(terminal);

                    if (expansion.NotifyOnChange && !onStartUp)
                    {
                        GameObject.GetInstance()
                                  .NewsBox.AddNews(
                                      new News(
                                          News.NewsType.AirportNews,
                                          GameObject.GetInstance().GameTime,
                                          "Closed Terminal",
                                          $"[LI airport={airport.Profile.IATACode}]({new AirportCodeConverter().Convert(airport)}) has closed its terminal {expansion.Name}"));
                    }
                }
            }
            //close terminal
        }

        //returns if an airline has enough free slots at an airport

        //returns the yearly payment for a number of gates
        public static double GetYearlyContractPayment(
            Airport airport,
            AirportContract.ContractType type,
            int gates,
            int length)
        {
            double basePrice = 0;

            if (type == AirportContract.ContractType.Full)
            {
                basePrice = airport.GetGatePrice()*12;
            }

            if (type == AirportContract.ContractType.LowService)
            {
                basePrice = airport.GetGatePrice()*13;
            }

            if (type == AirportContract.ContractType.MediumService)
            {
                basePrice = airport.GetGatePrice()*17;
            }

            if (type == AirportContract.ContractType.FullService)
            {
                basePrice = airport.GetGatePrice()*20;
            }

            double lengthFactor = 100 - length;

            return gates*(basePrice*(lengthFactor/100));
        }

        public static bool HasBadWeather(Airport airport)
        {
            return false;
            //airport.Weather[0].WindSpeed == Weather.eWindSpeed.Hurricane || airport.Weather[0].WindSpeed == Weather.eWindSpeed.Violent_Storm;
        }

        public static bool HasFreeGates(Airport airport, Airline airline)
        {
            Terminal.TerminalType type = airline.AirlineRouteFocus == Route.RouteType.Cargo ? Terminal.TerminalType.Cargo : Terminal.TerminalType.Passenger;

            return HasFreeGates(airport, airline, type);
        }

        public static bool HasFreeGates(Airport airport, Airline airline, Terminal.TerminalType type)
        {
            List<AirportContract> contracts = airport.GetAirlineContracts(airline).Where(c => c.TerminalType == type).ToList();

            if (contracts.Count == 0)
            {
                return false;
            }

            return airport.Terminals.GetFreeSlotsPercent(airline, type) > 90;
        }

        public static bool HasRoute(Airport airport1, Airport airport2)
        {
            var airlines = new List<Airline>(Airlines.GetAllAirlines());

            var routes = new List<Route>();

            foreach (Airline airline in airlines)
            {
                routes.AddRange(airline.Routes);
            }

            return
                routes.Any(r => (r.Destination1 == airport1 && r.Destination2 == airport2)
                                || (r.Destination1 == airport2 && r.Destination2 == airport1));
        }

        public static void ReallocateAirport(Airport airportOld, Airport airportNew)
        {
            if (airportNew.GetMajorDestinations().Count == 0)
            {
                foreach (DestinationDemand paxDemand in airportOld.GetDestinationsPassengers())
                {
                    airportNew.AddDestinationPassengersRate(paxDemand);
                }
            }
        }

        public static bool RentGates(Airport airport, Airline airline, AirportContract.ContractType type)
        {
            Terminal.TerminalType terminaltype = airline.AirlineRouteFocus == Route.RouteType.Cargo ? Terminal.TerminalType.Cargo : Terminal.TerminalType.Passenger;

            return RentGates(airport, airline, type, terminaltype);
        }

        public static bool RentGates(Airport airport, Airline airline, AirportContract.ContractType type, Terminal.TerminalType terminaltype)
        {
            int maxGates = airport.Terminals.GetFreeGates(terminaltype);

            int gatesToRent = Math.Min(maxGates, (int) (airline.Mentality) + 2);

            if (gatesToRent == 0)
            {
                return false;
            }

            RentGates(airport, airline, type, terminaltype, gatesToRent);

            return true;
        }

        public static void RentGates(
            Airport airport,
            Airline airline,
            AirportContract.ContractType type,
            Terminal.TerminalType terminaltype,
            int gates,
            int length = 20)
        {
            int currentgates = airport.AirlineContracts.Where(a => a.Airline == airline && a.TerminalType == terminaltype).Sum(c => c.NumberOfGates);
            var contract = new AirportContract(
                airline,
                airport,
                type,
                terminaltype,
                GameObject.GetInstance().GameTime,
                gates,
                length,
                GetYearlyContractPayment(airport, type, gates, length),
                true);

            if (currentgates == 0)
            {
                AddAirlineContract(contract);
            }
            else
            {
                foreach (AirportContract c in airport.AirlineContracts.Where(a => a.Airline == airline))
                {
                    c.NumberOfGates += gates;
                }
            }

            for (int i = 0; i < gates; i++)
            {
                Gate gate = airport.Terminals.GetGates().FirstOrDefault(g => g.Airline == null);

                if (gate != null)
                    gate.Airline = airline;
            }
        }

        #endregion

        #region Methods

        private static Weather CreateDayWeather(Airport airport, DateTime date, Weather previousWeather)
        {
            WeatherAverage average = WeatherAverages.GetWeatherAverage(date.Month, airport);

            if (average != null)
            {
                return CreateDayWeather(date, previousWeather, average);
            }

            var precipitationValues = (Weather.Precipitation[]) Enum.GetValues(typeof (Weather.Precipitation));
            var coverValues = (Weather.CloudCover[]) Enum.GetValues(typeof (Weather.CloudCover));
            var windDirectionValues = (Weather.WindDirection[]) Enum.GetValues(typeof (Weather.WindDirection));
            var windSpeedValues = (Weather.eWindSpeed[]) Enum.GetValues(typeof (Weather.eWindSpeed));
            Weather.eWindSpeed windSpeed;
            double temperature;

            Weather.WindDirection windDirection = windDirectionValues[Rnd.Next(windDirectionValues.Length)];

            if (previousWeather == null)
            {
                windSpeed = windSpeedValues[Rnd.Next(windSpeedValues.Length)];

                const double maxTemp = 40;
                const double minTemp = -20;

                temperature = MathHelpers.GetRandomDoubleNumber(minTemp, maxTemp);
            }
            else
            {
                int windIndex = windSpeedValues.ToList().IndexOf(previousWeather.WindSpeed);
                windSpeed =
                    windSpeedValues[
                        Rnd.Next(Math.Max(0, windIndex - 2), Math.Min(windIndex + 2, windSpeedValues.Length))];

                double previousTemperature = (previousWeather.TemperatureHigh + previousWeather.TemperatureLow)/2;

                double maxTemp = Math.Min(40, previousTemperature + 5);
                double minTemp = Math.Max(-20, previousTemperature - 5);

                temperature = MathHelpers.GetRandomDoubleNumber(minTemp, maxTemp);
            }

            double temperatureLow = temperature - Rnd.Next(1, 10);
            double temperatureHigh = temperature + Rnd.Next(1, 10);

            double tempDiff = temperatureHigh - temperatureLow;
            double temperatureSunrise = temperatureLow + MathHelpers.GetRandomDoubleNumber(-2, Math.Min(tempDiff, 2));
            double temperatureSunset = temperatureHigh - MathHelpers.GetRandomDoubleNumber(-2, Math.Min(tempDiff, 2));
            double temperatureDayend = temperatureLow + Rnd.Next(-2, 2);

            Weather.CloudCover cover = coverValues[Rnd.Next(coverValues.Length)];
            Weather.Precipitation precip;
            if (cover == Weather.CloudCover.Overcast)
            {
                precip = precipitationValues[Rnd.Next(precipitationValues.Length)];
            }

            var hourlyTemperature = new HourlyWeather[24];

            if (previousWeather == null)
            {
                hourlyTemperature[0] = new HourlyWeather(
                    temperatureLow,
                    cover,
                    cover == Weather.CloudCover.Overcast ? GetPrecipitation(temperatureLow) : Weather.Precipitation.None,
                    windSpeed,
                    windDirection);
            }
            else
            {
                hourlyTemperature[0] = previousWeather.Temperatures[previousWeather.Temperatures.Length - 1];
            }

            double morningSteps = (temperatureSunrise - hourlyTemperature[0].Temperature)/(Weather.Sunrise - 1);

            for (int i = 1; i <= Weather.Sunrise; i++)
            {
                double temp = hourlyTemperature[i - 1].Temperature + morningSteps;
                Weather.CloudCover hourlyCover = Rnd.Next(3) == 0 ? coverValues[Rnd.Next(coverValues.Length)] : cover;

                int windspeedIndex = windSpeedValues.ToList().IndexOf(windSpeed);
                Weather.eWindSpeed[] hourlyWindspeedValues =
                    {
                        windSpeed, windSpeed, windSpeed,
                        hourlyTemperature[i - 1].WindSpeed,
                        windspeedIndex > 0
                            ? (Weather.eWindSpeed) windspeedIndex - 1
                            : (Weather.eWindSpeed) windspeedIndex + 1,
                        windspeedIndex < windSpeedValues.Length - 1
                            ? (Weather.eWindSpeed) windspeedIndex + 1
                            : (Weather.eWindSpeed) windspeedIndex - 1
                    };
                Weather.eWindSpeed hourlyWindspeed = hourlyWindspeedValues[Rnd.Next(hourlyWindspeedValues.Length)];

                hourlyTemperature[i] = new HourlyWeather(
                    temp,
                    hourlyCover,
                    hourlyCover == Weather.CloudCover.Overcast ? GetPrecipitation(temp) : Weather.Precipitation.None,
                    hourlyWindspeed,
                    windDirection);
            }

            double daySteps = (temperatureSunset - temperatureSunrise)/(Weather.Sunset - Weather.Sunrise - 1);

            for (int i = Weather.Sunrise + 1; i < Weather.Sunset; i++)
            {
                Weather.CloudCover hourlyCover = Rnd.Next(3) == 0 ? coverValues[Rnd.Next(coverValues.Length)] : cover;

                double temp = hourlyTemperature[i - 1].Temperature + daySteps;
                if (hourlyCover != hourlyTemperature[i - 1].Cover && hourlyCover == Weather.CloudCover.Overcast)
                {
                    temp -= MathHelpers.GetRandomDoubleNumber(1, 4);
                }
                if (hourlyCover != hourlyTemperature[i - 1].Cover
                    && hourlyTemperature[i - 1].Cover == Weather.CloudCover.Overcast)
                {
                    temp += MathHelpers.GetRandomDoubleNumber(1, 4);
                }

                int windspeedIndex = windSpeedValues.ToList().IndexOf(windSpeed);
                Weather.eWindSpeed[] hourlyWindspeedValues =
                    {
                        windSpeed, windSpeed, windSpeed,
                        hourlyTemperature[i - 1].WindSpeed,
                        windspeedIndex > 0
                            ? (Weather.eWindSpeed) windspeedIndex - 1
                            : (Weather.eWindSpeed) windspeedIndex + 1,
                        windspeedIndex < windSpeedValues.Length - 1
                            ? (Weather.eWindSpeed) windspeedIndex + 1
                            : (Weather.eWindSpeed) windspeedIndex - 1
                    };
                Weather.eWindSpeed hourlyWindspeed = hourlyWindspeedValues[Rnd.Next(hourlyWindspeedValues.Length)];

                hourlyTemperature[i] = new HourlyWeather(
                    temp,
                    hourlyCover,
                    hourlyCover == Weather.CloudCover.Overcast ? GetPrecipitation(temp) : Weather.Precipitation.None,
                    hourlyWindspeed,
                    windDirection);
            }

            double eveningSteps = (temperatureDayend - temperatureSunset)/(hourlyTemperature.Length - Weather.Sunset);

            for (int i = Weather.Sunset; i < hourlyTemperature.Length; i++)
            {
                double temp = hourlyTemperature[i - 1].Temperature + eveningSteps;
                Weather.CloudCover hourlyCover = Rnd.Next(3) == 0 ? coverValues[Rnd.Next(coverValues.Length)] : cover;

                int windspeedIndex = windSpeedValues.ToList().IndexOf(windSpeed);
                Weather.eWindSpeed[] hourlyWindspeedValues =
                    {
                        windSpeed, windSpeed, windSpeed,
                        hourlyTemperature[i - 1].WindSpeed,
                        windspeedIndex > 0
                            ? (Weather.eWindSpeed) windspeedIndex - 1
                            : (Weather.eWindSpeed) windspeedIndex + 1,
                        windspeedIndex < windSpeedValues.Length - 1
                            ? (Weather.eWindSpeed) windspeedIndex + 1
                            : (Weather.eWindSpeed) windspeedIndex - 1
                    };
                Weather.eWindSpeed hourlyWindspeed = hourlyWindspeedValues[Rnd.Next(hourlyWindspeedValues.Length)];

                hourlyTemperature[i] = new HourlyWeather(
                    temp,
                    hourlyCover,
                    hourlyCover == Weather.CloudCover.Overcast ? GetPrecipitation(temp) : Weather.Precipitation.None,
                    hourlyWindspeed,
                    windDirection);
            }

            temperatureLow = hourlyTemperature.Min(t => t.Temperature);
            temperatureHigh = hourlyTemperature.Max(t => t.Temperature);
            cover =
                (from c in hourlyTemperature group c by c.Cover into g select new {Cover = g.Key, Qty = g.Count()})
                    .OrderByDescending(g => g.Qty).First().Cover;
            precip =
                (from c in hourlyTemperature group c by c.Precip into g select new {Precip = g.Key, Qty = g.Count()})
                    .OrderByDescending(g => g.Qty).First().Precip;

            var weather = new Weather(
                date,
                windSpeed,
                windDirection,
                cover,
                precip,
                hourlyTemperature,
                temperatureLow,
                temperatureHigh);

            return weather;
        }

        //creates the weather from an average
        private static Weather CreateDayWeather(DateTime date, Weather previousWeather, WeatherAverage average)
        {
            var windDirectionValues = (Weather.WindDirection[]) Enum.GetValues(typeof (Weather.WindDirection));
            var windSpeedValues = (Weather.eWindSpeed[]) Enum.GetValues(typeof (Weather.eWindSpeed));
            var coverValues = (Weather.CloudCover[]) Enum.GetValues(typeof (Weather.CloudCover));

            Weather.WindDirection windDirection = windDirectionValues[Rnd.Next(windDirectionValues.Length)];
            Weather.CloudCover cover;
            Weather.Precipitation precip;
            Weather.eWindSpeed windSpeed;
            double temperatureLow,
                   temperatureHigh;

            int windIndexMin = windSpeedValues.ToList().IndexOf(average.WindSpeedMin);
            int windIndexMax = windSpeedValues.ToList().IndexOf(average.WindSpeedMax);

            if (previousWeather == null)
            {
                windSpeed = windSpeedValues[Rnd.Next(windIndexMin, windIndexMax)];

                temperatureLow = MathHelpers.GetRandomDoubleNumber(
                    average.TemperatureMin - 5,
                    average.TemperatureMin + 5);
                temperatureHigh =
                    MathHelpers.GetRandomDoubleNumber(
                        Math.Max(average.TemperatureMax - 5, temperatureLow + 1),
                        average.TemperatureMax + 5);
            }
            else
            {
                double previousTemperature = (previousWeather.TemperatureHigh + previousWeather.TemperatureLow)/2;
                int windIndex = windSpeedValues.ToList().IndexOf(previousWeather.WindSpeed);
                windSpeed =
                    windSpeedValues[
                        Rnd.Next(Math.Max(windIndexMin, windIndex - 2), Math.Min(windIndex + 2, windIndexMax))];

                double minTemp = Math.Max(average.TemperatureMin, previousTemperature - 5);
                temperatureLow = MathHelpers.GetRandomDoubleNumber(minTemp - 5, minTemp + 5);

                double maxTemp = Math.Min(average.TemperatureMax, previousTemperature + 5);

                temperatureHigh = MathHelpers.GetRandomDoubleNumber(Math.Max(maxTemp - 5, temperatureLow), maxTemp + 5);
                //rnd.NextDouble() * ((maxTemp + 5) - Math.Max(maxTemp - 5, temperatureLow + 2)) + Math.Max(maxTemp - 5, temperatureLow + 2);
            }

            double tempDiff = temperatureHigh - temperatureLow;
            double temperatureSunrise = temperatureLow + MathHelpers.GetRandomDoubleNumber(-2, Math.Min(tempDiff, 2));
            double temperatureSunset = temperatureHigh - MathHelpers.GetRandomDoubleNumber(-2, Math.Min(tempDiff, 2));
            double temperatureDayend = temperatureLow + Rnd.Next(-2, 2);

            double temperature = (temperatureLow + temperatureHigh)/2;

            bool isOvercast = Rnd.Next(100) < average.Precipitation;
            if (isOvercast)
            {
                cover = Weather.CloudCover.Overcast;
                precip = GetPrecipitation(temperature);
            }
            else
            {
                Weather.CloudCover[] notOvercastCovers =
                    {
                        Weather.CloudCover.Clear, Weather.CloudCover.MostlyCloudy,
                        Weather.CloudCover.PartlyCloudy
                    };
                cover = notOvercastCovers[Rnd.Next(notOvercastCovers.Length)];
            }

            var hourlyTemperature = new HourlyWeather[24];

            if (previousWeather == null)
            {
                hourlyTemperature[0] = new HourlyWeather(
                    temperatureLow,
                    cover,
                    cover == Weather.CloudCover.Overcast ? GetPrecipitation(temperatureLow) : Weather.Precipitation.None,
                    windSpeed,
                    windDirection);
            }
            else
            {
                hourlyTemperature[0] = previousWeather.Temperatures[previousWeather.Temperatures.Length - 1];
            }

            double morningSteps = (temperatureSunrise - hourlyTemperature[0].Temperature)/(Weather.Sunrise - 1);

            for (int i = 1; i <= Weather.Sunrise; i++)
            {
                double temp = hourlyTemperature[i - 1].Temperature + morningSteps;
                Weather.CloudCover hourlyCover = Rnd.Next(3) == 0 ? coverValues[Rnd.Next(coverValues.Length)] : cover;

                int windspeedIndex = windSpeedValues.ToList().IndexOf(windSpeed);
                Weather.eWindSpeed[] hourlyWindspeedValues =
                    {
                        windSpeed, windSpeed, windSpeed,
                        hourlyTemperature[i - 1].WindSpeed,
                        windspeedIndex > 0
                            ? (Weather.eWindSpeed) windspeedIndex - 1
                            : (Weather.eWindSpeed) windspeedIndex + 1,
                        windspeedIndex < windSpeedValues.Length - 1
                            ? (Weather.eWindSpeed) windspeedIndex + 1
                            : (Weather.eWindSpeed) windspeedIndex - 1
                    };
                Weather.eWindSpeed hourlyWindspeed = hourlyWindspeedValues[Rnd.Next(hourlyWindspeedValues.Length)];

                hourlyTemperature[i] = new HourlyWeather(
                    temp,
                    hourlyCover,
                    hourlyCover == Weather.CloudCover.Overcast ? GetPrecipitation(temp) : Weather.Precipitation.None,
                    hourlyWindspeed,
                    windDirection);
            }

            double daySteps = (temperatureSunset - temperatureSunrise)/(Weather.Sunset - Weather.Sunrise - 1);

            for (int i = Weather.Sunrise + 1; i < Weather.Sunset; i++)
            {
                Weather.CloudCover hourlyCover = Rnd.Next(3) == 0 ? coverValues[Rnd.Next(coverValues.Length)] : cover;

                double temp = hourlyTemperature[i - 1].Temperature + daySteps;
                if (hourlyCover != hourlyTemperature[i - 1].Cover && hourlyCover == Weather.CloudCover.Overcast)
                {
                    temp -= MathHelpers.GetRandomDoubleNumber(1, 4);
                }
                if (hourlyCover != hourlyTemperature[i - 1].Cover
                    && hourlyTemperature[i - 1].Cover == Weather.CloudCover.Overcast)
                {
                    temp += MathHelpers.GetRandomDoubleNumber(1, 4);
                }

                int windspeedIndex = windSpeedValues.ToList().IndexOf(windSpeed);
                Weather.eWindSpeed[] hourlyWindspeedValues =
                    {
                        windSpeed, windSpeed, windSpeed,
                        hourlyTemperature[i - 1].WindSpeed,
                        windspeedIndex > 0
                            ? (Weather.eWindSpeed) windspeedIndex - 1
                            : (Weather.eWindSpeed) windspeedIndex + 1,
                        windspeedIndex < windSpeedValues.Length - 1
                            ? (Weather.eWindSpeed) windspeedIndex + 1
                            : (Weather.eWindSpeed) windspeedIndex - 1
                    };
                Weather.eWindSpeed hourlyWindspeed = hourlyWindspeedValues[Rnd.Next(hourlyWindspeedValues.Length)];

                hourlyTemperature[i] = new HourlyWeather(
                    temp,
                    hourlyCover,
                    hourlyCover == Weather.CloudCover.Overcast ? GetPrecipitation(temp) : Weather.Precipitation.None,
                    hourlyWindspeed,
                    windDirection);
            }

            double eveningSteps = (temperatureDayend - temperatureSunset)/(hourlyTemperature.Length - Weather.Sunset);

            for (int i = Weather.Sunset; i < hourlyTemperature.Length; i++)
            {
                double temp = hourlyTemperature[i - 1].Temperature + eveningSteps;
                Weather.CloudCover hourlyCover = Rnd.Next(3) == 0 ? coverValues[Rnd.Next(coverValues.Length)] : cover;

                int windspeedIndex = windSpeedValues.ToList().IndexOf(windSpeed);
                Weather.eWindSpeed[] hourlyWindspeedValues =
                    {
                        windSpeed, windSpeed, windSpeed,
                        hourlyTemperature[i - 1].WindSpeed,
                        windspeedIndex > 0
                            ? (Weather.eWindSpeed) windspeedIndex - 1
                            : (Weather.eWindSpeed) windspeedIndex + 1,
                        windspeedIndex < windSpeedValues.Length - 1
                            ? (Weather.eWindSpeed) windspeedIndex + 1
                            : (Weather.eWindSpeed) windspeedIndex - 1
                    };
                Weather.eWindSpeed hourlyWindspeed = hourlyWindspeedValues[Rnd.Next(hourlyWindspeedValues.Length)];

                hourlyTemperature[i] = new HourlyWeather(
                    temp,
                    hourlyCover,
                    hourlyCover == Weather.CloudCover.Overcast ? GetPrecipitation(temp) : Weather.Precipitation.None,
                    hourlyWindspeed,
                    windDirection);
            }
            temperatureLow = hourlyTemperature.Min(t => t.Temperature);
            temperatureHigh = hourlyTemperature.Max(t => t.Temperature);

            cover =
                (from c in hourlyTemperature group c by c.Cover into g select new {Cover = g.Key, Qty = g.Count()})
                    .OrderByDescending(g => g.Qty).First().Cover;
            precip =
                (from c in hourlyTemperature group c by c.Precip into g select new {Precip = g.Key, Qty = g.Count()})
                    .OrderByDescending(g => g.Qty).First().Precip;

            var weather = new Weather(
                date,
                windSpeed,
                windDirection,
                cover,
                precip,
                hourlyTemperature,
                temperatureLow,
                temperatureHigh);

            return weather;
        }

        //returns the precipitation for a temperature
        private static Weather.Precipitation GetPrecipitation(double temperature)
        {
            if (temperature > 10)
            {
                Weather.Precipitation[] values =
                    {
                        Weather.Precipitation.Thunderstorms, Weather.Precipitation.HeavyRain,
                        Weather.Precipitation.LightRain,
                        Weather.Precipitation.IsolatedThunderstorms
                    };
                return values[Rnd.Next(values.Length)];
            }
            if (temperature <= 10 && temperature >= 5)
            {
                Weather.Precipitation[] values =
                    {
                        Weather.Precipitation.HeavyRain, Weather.Precipitation.LightRain,
                        Weather.Precipitation.IsolatedRain,
                        Weather.Precipitation.IsolatedThunderstorms
                    };
                return values[Rnd.Next(values.Length)];
            }
            if (temperature < 5 && temperature >= -3)
            {
                Weather.Precipitation[] values =
                    {
                        Weather.Precipitation.FreezingRain,
                        Weather.Precipitation.MixedRainAndSnow,
                        Weather.Precipitation.Sleet, Weather.Precipitation.LightSnow,
                        Weather.Precipitation.IsolatedSnow
                    };
                return values[Rnd.Next(values.Length)];
            }
            if (temperature < -3)
            {
                Weather.Precipitation[] values =
                    {
                        Weather.Precipitation.HeavySnow, Weather.Precipitation.LightSnow,
                        Weather.Precipitation.IsolatedSnow
                    };
                return values[Rnd.Next(values.Length)];
            }
            return Weather.Precipitation.LightRain;
        }

        #endregion

        //returns the price for a hub at an airport
    }
}