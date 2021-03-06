﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheAirline.GUIModel.HelpersModel;
using TheAirline.Infrastructure;
using TheAirline.Infrastructure.Enums;
using TheAirline.Models.Airliners;
using TheAirline.Models.Airlines;
using TheAirline.Models.Airlines.Subsidiary;
using TheAirline.Models.Airports;
using TheAirline.Models.General;
using TheAirline.Models.General.Countries;
using TheAirline.Models.General.Finances;
using TheAirline.Models.Passengers;
using TheAirline.Models.Routes;

namespace TheAirline.Helpers
{
    //the helpers class for the AI
    public class AIHelpers
    {
        #region Static Fields

        private static readonly Random Rnd = new Random();

        #endregion

        #region Public Methods and Operators

        public static bool WillTakeSpecialContract(Airline airline, SpecialContractType contract)
        {
            return false;
        }

        public static bool CanJoinAlliance(Airline airline, Alliance alliance)
        {
            IEnumerable<Country> sameCountries =
                alliance.Members.SelectMany(m => m.Airline.Airports)
                        .Select(a => a.Profile.Country)
                        .Distinct()
                        .Intersect(airline.Airports.Select(a => a.Profile.Country).Distinct());
            IEnumerable<Airport> sameDestinations =
                alliance.Members.SelectMany(m => m.Airline.Airports).Distinct().Intersect(airline.Airports);

            double airlineDestinations = airline.Airports.Count;
            double airlineRoutes = airline.Routes.Count;
            double airlineCountries = airline.Airports.Select(a => a.Profile.Country).Distinct().Count();
            double airlineAlliances = airline.Alliances.Count;

            double allianceRoutes = alliance.Members.SelectMany(m => m.Airline.Routes).Count();

            //declines if airline is much smaller than alliance
            if (airlineRoutes*5 < allianceRoutes)
            {
                return false;
            }

            //declines if there is a match for 75% of the airline and alliance destinations
            if (sameDestinations.Count() >= airlineDestinations*0.75)
            {
                return false;
            }

            //declines if there is a match for 75% of the airline and alliance countries
            if (sameCountries.Count() >= airlineCountries*0.75)
            {
                return false;
            }

            return true;
        }

        public static bool CanRemoveFromAlliance(Airline remover, Airline toremove, Alliance alliance)
        {
            return remover.GetValue() > toremove.GetValue()*0.9;
        }

        public static RouteTimeTable CreateAirlinerRouteTimeTable(
            Route route,
            FleetAirliner airliner,
            int flightsPerDay,
            string flightCode1,
            string flightCode2)
        {
            const int startHour = 6;
            const int endHour = 22;

            TimeSpan routeFlightTime = route.GetFlightTime(airliner.Airliner.Type);

            TimeSpan minFlightTime = routeFlightTime.Add(FleetAirlinerHelpers.GetMinTimeBetweenFlights(airliner));

            var minDelayMinutes = (int) FleetAirlinerHelpers.GetMinTimeBetweenFlights(airliner).TotalMinutes;

            int startMinutes =
                Convert.ToInt16(((endHour - startHour)*60) - (minFlightTime.TotalMinutes*flightsPerDay*2));

            if (startMinutes < 0)
            {
                startMinutes = 0;
            }

            TimeSpan flightTime = new TimeSpan(startHour, 0, 0).Add(new TimeSpan(0, startMinutes/2, 0));

            return CreateAirlinerRouteTimeTable(
                route,
                airliner,
                flightsPerDay,
                true,
                minDelayMinutes,
                flightTime,
                flightCode1,
                flightCode2);
        }

        public static RouteTimeTable CreateAirlinerRouteTimeTable(
            Route route,
            FleetAirliner airliner,
            int numberOfFlights,
            Boolean isDaily,
            int delayMinutes,
            TimeSpan startTime,
            string flightCode1,
            string flightCode2)
        {
            var delayTime = new TimeSpan(0, delayMinutes, 0);
            var timeTable = new RouteTimeTable(route);

            TimeSpan routeFlightTime = route.GetFlightTime(airliner.Airliner.Type);

            TimeSpan minFlightTime = routeFlightTime.Add(delayTime);

            if (minFlightTime.Hours < 12 && minFlightTime.Days < 1 && isDaily)
            {
                var flightTime = new TimeSpan(startTime.Hours, startTime.Minutes, startTime.Seconds);
                //new TimeSpan(startHour, 0, 0).Add(new TimeSpan(0, startMinutes / 2, 0));

                for (int i = 0; i < numberOfFlights; i++)
                {
                    timeTable.AddDailyEntries(new RouteEntryDestination(route.Destination2, flightCode1), flightTime);

                    flightTime = flightTime.Add(minFlightTime);

                    timeTable.AddDailyEntries(new RouteEntryDestination(route.Destination1, flightCode2), flightTime);

                    flightTime = flightTime.Add(minFlightTime);
                }
            }
            else
            {
                if (isDaily || minFlightTime.Hours >= 12 || minFlightTime.Days >= 1)
                {
                    DayOfWeek day = 0;

                    int outTime = 15*Rnd.Next(-12, 12);
                    int homeTime = 15*Rnd.Next(-12, 12);

                    for (int i = 0; i < 3; i++)
                    {
                        Gate outboundGate = GetFreeAirlineGate(
                            airliner.Airliner.Airline,
                            route.Destination1,
                            day,
                            new TimeSpan(12, 0, 0).Add(new TimeSpan(0, outTime, 0)));
                        timeTable.AddEntry(
                            new RouteTimeTableEntry(
                                timeTable,
                                day,
                                new TimeSpan(12, 0, 0).Add(new TimeSpan(0, outTime, 0)),
                                new RouteEntryDestination(route.Destination2, flightCode1),
                                outboundGate));

                        day += 2;
                    }

                    day = (DayOfWeek) 1;

                    for (int i = 0; i < 3; i++)
                    {
                        Gate outboundGate = GetFreeAirlineGate(
                            airliner.Airliner.Airline,
                            route.Destination2,
                            day,
                            new TimeSpan(12, 0, 0).Add(new TimeSpan(0, homeTime, 0)));

                        timeTable.AddEntry(
                            new RouteTimeTableEntry(
                                timeTable,
                                day,
                                new TimeSpan(12, 0, 0).Add(new TimeSpan(0, homeTime, 0)),
                                new RouteEntryDestination(route.Destination1, flightCode2),
                                outboundGate));

                        day += 2;
                    }
                }
                else
                {
                    var day = (DayOfWeek) (7 - numberOfFlights/2);

                    for (int i = 0; i < numberOfFlights; i++)
                    {
                        var flightTime = new TimeSpan(startTime.Hours, startTime.Minutes, startTime.Seconds);

                        timeTable.AddEntry(
                            new RouteTimeTableEntry(
                                timeTable,
                                day,
                                flightTime,
                                new RouteEntryDestination(route.Destination2, flightCode1)));

                        flightTime = flightTime.Add(minFlightTime);

                        timeTable.AddEntry(
                            new RouteTimeTableEntry(
                                timeTable,
                                day,
                                flightTime,
                                new RouteEntryDestination(route.Destination1, flightCode2)));

                        day++;

                        if (((int) day) > 6)
                        {
                            day = 0;
                        }
                    }
                }
            }

            foreach (RouteTimeTableEntry e in timeTable.Entries)
            {
                e.Airliner = airliner;
            }

            return timeTable;
        }

        public static RouteTimeTable CreateBusinessRouteTimeTable(
            Route route,
            FleetAirliner airliner,
            int flightsPerDay,
            string flightCode1,
            string flightCode2)
        {
            var timeTable = new RouteTimeTable(route);

            TimeSpan minFlightTime =
                MathHelpers.GetFlightTime(
                    route.Destination1.Profile.Coordinates.ConvertToGeoCoordinate(),
                    route.Destination2.Profile.Coordinates.ConvertToGeoCoordinate(),
                    airliner.Airliner.Type)
                           .Add(new TimeSpan(FleetAirlinerHelpers.GetMinTimeBetweenFlights(airliner).Ticks));

            int startHour = 6;
            const int endHour = 10;

            int maxHours = endHour - startHour; //entries.Airliners == null

            int startMinutes = Convert.ToInt16((maxHours*60) - (minFlightTime.TotalMinutes*flightsPerDay*2));

            if (startMinutes < 0)
            {
                startMinutes = 0;
            }

            //morning
            TimeSpan flightTime = new TimeSpan(startHour, 0, 0).Add(new TimeSpan(0, startMinutes/2, 0));

            for (int i = 0; i < flightsPerDay; i++)
            {
                timeTable.AddWeekDailyEntries(new RouteEntryDestination(route.Destination2, flightCode1), flightTime);

                flightTime = flightTime.Add(minFlightTime);

                timeTable.AddWeekDailyEntries(new RouteEntryDestination(route.Destination1, flightCode2), flightTime);

                flightTime = flightTime.Add(minFlightTime);
            }
            //evening
            startHour = 18;
            flightTime = new TimeSpan(startHour, 0, 0).Add(new TimeSpan(0, startMinutes/2, 0));
            for (int i = 0; i < flightsPerDay; i++)
            {
                timeTable.AddWeekDailyEntries(new RouteEntryDestination(route.Destination2, flightCode1), flightTime);

                flightTime = flightTime.Add(minFlightTime);

                timeTable.AddWeekDailyEntries(new RouteEntryDestination(route.Destination1, flightCode2), flightTime);

                flightTime = flightTime.Add(minFlightTime);
            }

            if (timeTable.Entries.Count == 0)
            {
                flightCode1 = "TT";
            }

            foreach (RouteTimeTableEntry e in timeTable.Entries)
            {
                e.Airliner = airliner;
            }

            return timeTable;
        }

        public static void CreateCargoRouteTimeTable(Route route, FleetAirliner airliner)
        {
            TimeSpan routeFlightTime =
                MathHelpers.GetFlightTime(
                    route.Destination1.Profile.Coordinates.ConvertToGeoCoordinate(),
                    route.Destination2.Profile.Coordinates.ConvertToGeoCoordinate(),
                    airliner.Airliner.Type);
            TimeSpan minFlightTime =
                routeFlightTime.Add(new TimeSpan(FleetAirlinerHelpers.GetMinTimeBetweenFlights(airliner).Ticks));

            const int maxHours = 20 - 8; //from 08.00 to 20.00

            int flightsPerDay = Convert.ToInt16(maxHours*60/(2*minFlightTime.TotalMinutes));

            string flightCode1 = airliner.Airliner.Airline.GetNextFlightCode(0);
            string flightCode2 = airliner.Airliner.Airline.GetNextFlightCode(1);

            route.TimeTable = CreateAirlinerRouteTimeTable(route, airliner, flightsPerDay, flightCode1, flightCode2);
        }

        public static bool CreateRouteTimeTable(Route route, List<FleetAirliner> airliners)
        {
            var totalFlightTime = new TimeSpan(airliners.Sum(a => route.GetFlightTime(a.Airliner.Type).Ticks));
            var maxFlightTime = new TimeSpan(airliners.Max(a => route.GetFlightTime(a.Airliner.Type)).Ticks);

            int maxHours = 22 - 6 - (int) Math.Ceiling(maxFlightTime.TotalMinutes); //from 06.00 to 22.00

            if (totalFlightTime.TotalMinutes > maxHours)
            {
                return false;
            }

            var startTime = new TimeSpan(6, 0, 0);

            foreach (FleetAirliner airliner in airliners)
            {
                string flightCode1 = airliner.Airliner.Airline.GetNextFlightCode(0);
                string flightCode2 = airliner.Airliner.Airline.GetNextFlightCode(1);

                CreateAirlinerRouteTimeTable(
                    route,
                    airliner,
                    1,
                    true,
                    (int) FleetAirlinerHelpers.GetMinTimeBetweenFlights(airliner).TotalMinutes,
                    startTime,
                    flightCode1,
                    flightCode2);

                startTime = startTime.Add(route.GetFlightTime(airliner.Airliner.Type));
            }

            return true;
        }

        public static void CreateRouteTimeTable(Route route, FleetAirliner airliner)
        {
            TimeSpan routeFlightTime =
                MathHelpers.GetFlightTime(
                    route.Destination1.Profile.Coordinates.ConvertToGeoCoordinate(),
                    route.Destination2.Profile.Coordinates.ConvertToGeoCoordinate(),
                    airliner.Airliner.Type);
            TimeSpan minFlightTime =
                routeFlightTime.Add(new TimeSpan(FleetAirlinerHelpers.GetMinTimeBetweenFlights(airliner).Ticks));

            const int maxHours = 22 - 6; //from 06.00 to 22.00

            int flightsPerDay = Convert.ToInt16(maxHours*60/(2*minFlightTime.TotalMinutes));

            string flightCode1 = airliner.Airliner.Airline.GetNextFlightCode(0);
            string flightCode2 = airliner.Airliner.Airline.GetNextFlightCode(1);

            route.TimeTable = CreateAirlinerRouteTimeTable(route, airliner, flightsPerDay, flightCode1, flightCode2);
        }

        public static bool DoAcceptAllianceInvitation(Airline airline, Alliance alliance)
        {
            //a subsidiary of a human airline will always accept an invitation to an alliance where the parent is a member
            if (alliance.IsHumanAlliance && airline.IsHuman)
            {
                return true;
            }

            IEnumerable<Country> sameCountries =
                alliance.Members.SelectMany(m => m.Airline.Airports)
                        .Select(a => a.Profile.Country)
                        .Distinct()
                        .Intersect(airline.Airports.Select(a => a.Profile.Country).Distinct());
            IEnumerable<Airport> sameDestinations =
                alliance.Members.SelectMany(m => m.Airline.Airports).Distinct().Intersect(airline.Airports);

            double airlineDestinations = airline.Airports.Count;
            double airlineRoutes = airline.Routes.Count;
            double airlineCountries = airline.Airports.Select(a => a.Profile.Country).Distinct().Count();
            double airlineAlliances = airline.Alliances.Count;

            double allianceRoutes = alliance.Members.SelectMany(m => m.Airline.Routes).Count();

            //declines if invited airline is much larger than alliance
            if (airlineRoutes > 2*allianceRoutes)
            {
                return false;
            }

            //declines if there is a match for 50% of the airline and alliance destinations
            if (sameDestinations.Count() >= airlineDestinations*0.50)
            {
                return false;
            }

            //declines if there is a match for 75% of the airline and alliance countries
            if (sameCountries.Count() >= airlineCountries*0.75)
            {
                return false;
            }

            //declines if the airline already are in "many" alliances - many == 2
            if (airlineAlliances > 2)
            {
                return false;
            }

            return true;
        }

        public static KeyValuePair<Airliner, bool>? GetAirlinerForRoute(
            Airline airline,
            Airport destination1,
            Airport destination2,
            bool doLeasing,
            Route.RouteType focus,
            bool forStartdata = false)
        {
            List<AirlinerType> airlineAircrafts = airline.Profile.PreferedAircrafts;

            const double maxLoanTotal = 100000000;
            double distance = MathHelpers.GetDistance(
                destination1.Profile.Coordinates.ConvertToGeoCoordinate(),
                destination2.Profile.Coordinates.ConvertToGeoCoordinate());

            AirlinerType.TypeRange rangeType = GeneralHelpers.ConvertDistanceToRangeType(distance);

            List<Airliner> airliners;

            if (airlineAircrafts.Count > 0)
            {
                if (focus == Route.RouteType.Cargo)
                {
                    if (doLeasing)
                    {
                        airliners = Airliners.GetAirlinersForLeasing().FindAll(a => a.Type is AirlinerCargoType
                                                                                    && a.LeasingPrice*2 < airline.Money && a.GetAge() < 10 && distance < a.Range
                                                                                    && airlineAircrafts.Contains(a.Type));

                        if (airliners.Count == 0)
                        {
                            airliners =
                                Airliners.GetAirlinersForSale(a => a.Type is AirlinerCargoType)
                                         .FindAll(
                                             a =>
                                             a.LeasingPrice*2 < airline.Money && a.GetAge() < 10 && distance < a.Range
                                             && airlineAircrafts.Contains(a.Type));
                        }
                    }
                    else
                    {
                        airliners =
                            Airliners.GetAirlinersForSale(a => a.Type is AirlinerCargoType)
                                     .FindAll(
                                         a =>
                                         a.GetPrice() < airline.Money - 1000000 && a.GetAge() < 10
                                         && distance < a.Range && airlineAircrafts.Contains(a.Type));
                    }
                }
                else if (focus == Route.RouteType.Helicopter)
                {
                    if (doLeasing)
                    {
                        airliners =
                            Airliners.GetAirlinersForSale(a => a.Type is AirlinerPassengerType && a.Type.TypeAirliner == AirlinerType.TypeOfAirliner.Helicopter)
                                     .FindAll(
                                         a =>
                                         a.LeasingPrice*2 < airline.Money && a.GetAge() < 10 && distance < a.Range
                                         && airlineAircrafts.Contains(a.Type));
                    }
                    else
                    {
                        airliners =
                            Airliners.GetAirlinersForSale(a => a.Type is AirlinerPassengerType && a.Type.TypeAirliner == AirlinerType.TypeOfAirliner.Helicopter)
                                     .FindAll(
                                         a =>
                                         a.GetPrice() < airline.Money - 1000000 && a.GetAge() < 10
                                         && distance < a.Range && airlineAircrafts.Contains(a.Type));
                    }
                }
                else
                {
                    if (doLeasing)
                    {
                        airliners =
                            Airliners.GetAirlinersForLeasing()
                                     .FindAll(
                                         a => a.Type is AirlinerPassengerType
                                              && a.LeasingPrice*2 < airline.Money && a.GetAge() < 10 && distance < a.Range
                                              && airlineAircrafts.Contains(a.Type));

                        if (airliners.Count == 0)
                        {
                            airliners =
                                Airliners.GetAirlinersForSale(a => a.Type is AirlinerPassengerType)
                                         .FindAll(
                                             a =>
                                             a.LeasingPrice*2 < airline.Money && a.GetAge() < 10 && distance < a.Range
                                             && airlineAircrafts.Contains(a.Type));
                        }
                    }
                    else
                    {
                        airliners =
                            Airliners.GetAirlinersForSale(a => a.Type is AirlinerPassengerType)
                                     .FindAll(
                                         a =>
                                         a.GetPrice() < airline.Money - 1000000 && a.GetAge() < 10
                                         && distance < a.Range && airlineAircrafts.Contains(a.Type));
                    }
                }
            }
            else
            {
                if (focus == Route.RouteType.Cargo)
                {
                    if (doLeasing)
                    {
                        airliners = Airliners.GetAirlinersForLeasing()
                                             .FindAll(
                                                 a => a.Type is AirlinerCargoType
                                                      && a.LeasingPrice*2 < airline.Money && a.GetAge() < 10 && distance < a.Range);

                        if (airliners.Count == 0)
                        {
                            airliners =
                                Airliners.GetAirlinersForSale(a => a.Type is AirlinerCargoType)
                                         .FindAll(
                                             a =>
                                             a.LeasingPrice*2 < airline.Money && a.GetAge() < 10 && distance < a.Range);
                        }
                    }
                    else
                    {
                        airliners =
                            Airliners.GetAirlinersForSale(a => a.Type is AirlinerCargoType)
                                     .FindAll(
                                         a =>
                                         a.GetPrice() < airline.Money - 1000000 && a.GetAge() < 10
                                         && distance < a.Range);
                    }
                }
                else if (focus == Route.RouteType.Helicopter)
                {
                    if (doLeasing)
                    {
                        airliners =
                            Airliners.GetAirlinersForSale(a => a.Type is AirlinerPassengerType && a.Type.TypeAirliner == AirlinerType.TypeOfAirliner.Helicopter)
                                     .FindAll(
                                         a =>
                                         a.LeasingPrice*2 < airline.Money && a.GetAge() < 10 && distance < a.Range);
                    }
                    else
                    {
                        airliners =
                            Airliners.GetAirlinersForSale(a => a.Type is AirlinerPassengerType && a.Type.TypeAirliner == AirlinerType.TypeOfAirliner.Helicopter)
                                     .FindAll(
                                         a =>
                                         a.GetPrice() < airline.Money - 1000000 && a.GetAge() < 10
                                         && distance < a.Range);
                    }
                }
                else
                {
                    if (doLeasing)
                    {
                        airliners =
                            Airliners.GetAirlinersForSale()
                                     .FindAll(
                                         a => a.Type is AirlinerPassengerType
                                              && a.LeasingPrice*2 < airline.Money && a.GetAge() < 10 && distance < a.Range);

                        if (airliners.Count == 0)
                        {
                            airliners =
                                Airliners.GetAirlinersForSale(a => a.Type is AirlinerPassengerType)
                                         .FindAll(
                                             a =>
                                             a.LeasingPrice*2 < airline.Money && a.GetAge() < 10 && distance < a.Range);
                        }
                    }
                    else
                    {
                        airliners =
                            Airliners.GetAirlinersForSale(a => a.Type is AirlinerPassengerType)
                                     .FindAll(
                                         a =>
                                         a.GetPrice() < airline.Money - 1000000 && a.GetAge() < 10
                                         && distance < a.Range);
                    }
                }
            }
            if (airliners.Count > 0)
            {
                return new KeyValuePair<Airliner, bool>(airliners.OrderBy(a => a.Price).First(), false);
            }
            if (airline.Mentality == Airline.AirlineMentality.Aggressive || airline.Fleet.Count == 0 || forStartdata)
            {
                double airlineLoanTotal = airline.Loans.Sum(l => l.PaymentLeft);

                if (airlineLoanTotal < maxLoanTotal)
                {
                    List<Airliner> loanAirliners;

                    if (airlineAircrafts.Count > 0)
                    {
                        if (focus == Route.RouteType.Cargo)
                        {
                            loanAirliners =
                                Airliners.GetAirlinersForSale(a => a.Type is AirlinerCargoType)
                                         .FindAll(
                                             a =>
                                             a.GetPrice() < airline.Money + maxLoanTotal - airlineLoanTotal
                                             && distance < a.Range && airlineAircrafts.Contains(a.Type));
                        }
                        else if (focus == Route.RouteType.Helicopter)
                        {
                            loanAirliners =
                                Airliners.GetAirlinersForSale(a => a.Type is AirlinerPassengerType && a.Type.TypeAirliner == AirlinerType.TypeOfAirliner.Helicopter)
                                         .FindAll(
                                             a =>
                                             a.GetPrice() < airline.Money + maxLoanTotal - airlineLoanTotal
                                             && distance < a.Range && airlineAircrafts.Contains(a.Type));
                        }
                        else
                        {
                            loanAirliners =
                                Airliners.GetAirlinersForSale(a => a.Type is AirlinerPassengerType)
                                         .FindAll(
                                             a =>
                                             a.GetPrice() < airline.Money + maxLoanTotal - airlineLoanTotal
                                             && distance < a.Range && airlineAircrafts.Contains(a.Type));
                        }
                    }
                    else
                    {
                        if (focus == Route.RouteType.Cargo)
                        {
                            loanAirliners =
                                Airliners.GetAirlinersForSale(a => a.Type is AirlinerCargoType)
                                         .FindAll(
                                             a =>
                                             a.GetPrice() < airline.Money + maxLoanTotal - airlineLoanTotal
                                             && distance < a.Range);
                        }
                        else if (focus == Route.RouteType.Helicopter)
                        {
                            loanAirliners =
                                Airliners.GetAirlinersForSale(a => a.Type is AirlinerPassengerType && a.Type.TypeAirliner == AirlinerType.TypeOfAirliner.Helicopter)
                                         .FindAll(
                                             a =>
                                             a.GetPrice() < airline.Money + maxLoanTotal - airlineLoanTotal
                                             && distance < a.Range);
                        }
                        else
                        {
                            loanAirliners =
                                Airliners.GetAirlinersForSale(a => a.Type is AirlinerPassengerType)
                                         .FindAll(
                                             a =>
                                             a.GetPrice() < airline.Money + maxLoanTotal - airlineLoanTotal
                                             && distance < a.Range);
                        }
                    }
                    if (loanAirliners.Count > 0)
                    {
                        Airliner airliner = loanAirliners.OrderBy(a => a.Price).First();

                        if (airliner == null)
                        {
                            return null;
                        }

                        return new KeyValuePair<Airliner, bool>(airliner, true);
                    }
                    return null;
                }
                return null;
            }
            return null;
        }

        public static Airport GetDestinationAirport(Airline airline, Airport airport)
        {
            List<Airport> airports = GetDestinationAirports(airline, airport);
            if (airports.Count == 0)
            {
                return null;
            }
            return airports[0];
        }

        public static List<Airport> GetDestinationAirports(Airline airline, Airport airport)
        {
            IEnumerable<long> airliners = from a in Airliners.GetAirlinersForSale() select a.Range;
            var enumerable = airliners as long[] ?? airliners.ToArray();
            double maxDistance = !enumerable.Any() ? 5000 : enumerable.Max();

            double minDistance = (from a in Airports.GetAirports(a => a != airport)
                                  select
                                      MathHelpers.GetDistance(
                                          a.Profile.Coordinates.ConvertToGeoCoordinate(),
                                          airport.Profile.Coordinates.ConvertToGeoCoordinate())).Min();

            List<Airport> airports =
                Airports.GetAirports(
                    a =>
                    airline.Airports.Find(ar => ar.Profile.Town == a.Profile.Town) == null
                    && AirlineHelpers.HasAirlineLicens(airline, airport, a)
                    && !FlightRestrictions.HasRestriction(
                        a.Profile.Country,
                        airport.Profile.Country,
                        GameObject.GetInstance().GameTime,
                        FlightRestriction.RestrictionType.Flights)
                    && !FlightRestrictions.HasRestriction(
                        airport.Profile.Country,
                        a.Profile.Country,
                        GameObject.GetInstance().GameTime,
                        FlightRestriction.RestrictionType.Flights)
                    && !FlightRestrictions.HasRestriction(
                        airline,
                        a.Profile.Country,
                        airport.Profile.Country,
                        GameObject.GetInstance().GameTime));
            List<Route> routes = airline.Routes.FindAll(r => r.Destination1 == airport || r.Destination2 == airport);

            AirlineFocus marketFocus = airline.MarketFocus;

            Terminal.TerminalType terminaltype = airline.AirlineRouteFocus == Route.RouteType.Cargo ? Terminal.TerminalType.Cargo : Terminal.TerminalType.Passenger;

            if (airline.Airports.Count < 4)
            {
                var focuses = new List<AirlineFocus> {AirlineFocus.Local, AirlineFocus.Local, AirlineFocus.Local, marketFocus};

                marketFocus = focuses[Rnd.Next(focuses.Count)];
            }

            switch (marketFocus)
            {
                case AirlineFocus.Domestic:
                    airports = airports.FindAll(a => a.Profile.Country == airport.Profile.Country);
                    break;
                case AirlineFocus.Global:
                    airports =
                        airports.FindAll(
                            a =>
                            IsRouteInCorrectArea(airport, a)
                            && MathHelpers.GetDistance(
                                a.Profile.Coordinates.ConvertToGeoCoordinate(),
                                airport.Profile.Coordinates.ConvertToGeoCoordinate()) > 100
                            && airport.Profile.Town != a.Profile.Town
                            && MathHelpers.GetDistance(
                                a.Profile.Coordinates.ConvertToGeoCoordinate(),
                                airport.Profile.Coordinates.ConvertToGeoCoordinate()) < maxDistance
                            && MathHelpers.GetDistance(
                                a.Profile.Coordinates.ConvertToGeoCoordinate(),
                                airport.Profile.Coordinates.ConvertToGeoCoordinate()) > 100);
                    break;
                case AirlineFocus.Local:
                    airports =
                        airports.FindAll(
                            a =>
                            IsRouteInCorrectArea(airport, a)
                            && MathHelpers.GetDistance(
                                a.Profile.Coordinates.ConvertToGeoCoordinate(),
                                airport.Profile.Coordinates.ConvertToGeoCoordinate()) < Math.Max(minDistance, 1000)
                            && airport.Profile.Town != a.Profile.Town
                            && MathHelpers.GetDistance(
                                a.Profile.Coordinates.ConvertToGeoCoordinate(),
                                airport.Profile.Coordinates.ConvertToGeoCoordinate()) >= Route.MinRouteDistance);
                    break;
                case AirlineFocus.Regional:
                    airports =
                        airports.FindAll(
                            a =>
                            a.Profile.Country.Region == airport.Profile.Country.Region
                            && IsRouteInCorrectArea(airport, a)
                            && MathHelpers.GetDistance(
                                a.Profile.Coordinates.ConvertToGeoCoordinate(),
                                airport.Profile.Coordinates.ConvertToGeoCoordinate()) < maxDistance
                            && airport.Profile.Town != a.Profile.Town
                            && MathHelpers.GetDistance(
                                a.Profile.Coordinates.ConvertToGeoCoordinate(),
                                airport.Profile.Coordinates.ConvertToGeoCoordinate()) > 100);
                    break;
            }

            if (airports.Count == 0)
            {
                airports =
                    (from a in
                         Airports.GetAirports(
                             a =>
                             IsRouteInCorrectArea(airport, a)
                             && MathHelpers.GetDistance(
                                 a.Profile.Coordinates.ConvertToGeoCoordinate(),
                                 airport.Profile.Coordinates.ConvertToGeoCoordinate()) < 5000
                             && MathHelpers.GetDistance(
                                 a.Profile.Coordinates.ConvertToGeoCoordinate(),
                                 airport.Profile.Coordinates.ConvertToGeoCoordinate()) >= Route.MinRouteDistance)
                     orderby a.Profile.Size descending
                     select a).ToList();
            }

            return (from a in airports
                    where
                        routes.Find(r => r.Destination1 == a || r.Destination2 == a) == null
                        && (a.Terminals.GetFreeGates(terminaltype) > 0 || AirportHelpers.HasFreeGates(a, airline, terminaltype))
                    orderby
                         airport.GetDestinationPassengersRate(a, AirlinerClass.ClassType.EconomyClass)
                        + a.GetDestinationPassengersRate(airport, AirlinerClass.ClassType.EconomyClass) descending
                    select a).ToList();
        }

        public static T GetRandomItem<T>(Dictionary<T, int> list)
        {
            var tList = new List<T>();

            foreach (T item in list.Keys)
            {
                for (int i = 0; i < list[item]; i++)
                {
                    tList.Add(item);
                }
            }

            return tList[Rnd.Next(tList.Count)];
        }

        public static RouteClassesConfiguration GetRouteConfiguration(PassengerRoute route)
        {
            double distance = MathHelpers.GetDistance(route.Destination1, route.Destination2);

            if (distance < 500)
            {
                return (RouteClassesConfiguration) Configurations.GetStandardConfiguration("100");
            }
            if (distance < 2000)
            {
                return (RouteClassesConfiguration) Configurations.GetStandardConfiguration("101");
            }
            if (route.Destination1.Profile.Country == route.Destination2.Profile.Country)
            {
                return (RouteClassesConfiguration) Configurations.GetStandardConfiguration("102");
            }
            if (route.Destination1.Profile.Country != route.Destination2.Profile.Country)
            {
                return (RouteClassesConfiguration) Configurations.GetStandardConfiguration("103");
            }

            return null;
        }

        public static bool IsCargoRouteDestinationsCorrect(Airport dest1, Airport dest2, Airline airline)
        {
            return dest1.GetAirportFacility(airline, AirportFacility.FacilityType.Cargo, true).TypeLevel > 0
                   && dest2.GetAirportFacility(airline, AirportFacility.FacilityType.Cargo, true).TypeLevel > 0;
        }

        public static bool IsRouteInCorrectArea(Airport dest1, Airport dest2)
        {
            double distance = MathHelpers.GetDistance(
                dest1.Profile.Coordinates.ConvertToGeoCoordinate(),
                dest2.Profile.Coordinates.ConvertToGeoCoordinate());

            bool isOk = (dest1.Profile.Country == dest2.Profile.Country || distance < 1000
                            || (dest1.Profile.Country.Region == dest2.Profile.Country.Region
                                && (dest1.Profile.Type == AirportProfile.AirportType.ShortHaulInternational
                                    || dest1.Profile.Type == AirportProfile.AirportType.LongHaulInternational)
                                && (dest2.Profile.Type == AirportProfile.AirportType.ShortHaulInternational
                                    || dest2.Profile.Type == AirportProfile.AirportType.LongHaulInternational))
                            || (dest1.Profile.Type == AirportProfile.AirportType.LongHaulInternational
                                && dest2.Profile.Type == AirportProfile.AirportType.LongHaulInternational));

            return isOk;
        }

        public static void SetAirlinerHomebase(FleetAirliner airliner)
        {
            Airport homebase = GetServiceAirport(airliner.Airliner.Airline) ?? GetDestinationAirport(airliner.Airliner.Airline, airliner.Homebase);

            if (homebase.Terminals.GetNumberOfGates(airliner.Airliner.Airline) == 0)
            {
                AirportHelpers.RentGates(homebase, airliner.Airliner.Airline, AirportContract.ContractType.Full,
                                         airliner.Airliner.Airline.AirlineRouteFocus == Route.RouteType.Cargo ? Terminal.TerminalType.Cargo : Terminal.TerminalType.Passenger);
                AirportFacility checkinFacility =
                    AirportFacilities.GetFacilities(AirportFacility.FacilityType.CheckIn).Find(f => f.TypeLevel == 1);

                if (
                    homebase.GetAirportFacility(airliner.Airliner.Airline, AirportFacility.FacilityType.CheckIn)
                            .TypeLevel == 0)
                {
                    homebase.AddAirportFacility(
                        airliner.Airliner.Airline,
                        checkinFacility,
                        GameObject.GetInstance().GameTime);
                    AirlineHelpers.AddAirlineInvoice(
                        airliner.Airliner.Airline,
                        GameObject.GetInstance().GameTime,
                        Invoice.InvoiceType.Purchases,
                        -checkinFacility.Price);
                }
            }

            airliner.Homebase = homebase;
        }

        //updates a cpu airline
        public static void UpdateCPUAirline(Airline airline)
        {
            Parallel.Invoke(
                () => CheckForNewRoute(airline),
                () => CheckForNewHub(airline),
                () => CheckForUpdateRoute(airline),
                () => CheckForAirlinersWithoutRoutes(airline),
                () => CheckForOrderOfAirliners(airline),
                () => CheckForAirlineAlliance(airline),
                () => CheckForAirlineCodesharing(airline),
                () => CheckForSubsidiaryAirline(airline),
                () => CheckForAirlineAirportFacilities(airline),
                () => CheckForOrderOfAirliners(airline)); //close parallel.invoke
        }

        #endregion

        #region Methods

        private static void ChangeRouteServiceLevel(PassengerRoute route)
        {
            var oRoutes = new List<Route>(Airlines.GetAirlines(a => a != route.Airline).SelectMany(a => a.Routes));

            IEnumerable<Route> sameRoutes =
                oRoutes.Where(
                    r =>
                    (r.Type == Route.RouteType.Mixed || r.Type == Route.RouteType.Passenger || r.Type == Route.RouteType.Helicopter)
                    && ((r.Destination1 == route.Destination1 && r.Destination2 == route.Destination2)
                        || (r.Destination2 == route.Destination1 && r.Destination1 == route.Destination2)));

            var enumerable = sameRoutes as Route[] ?? sameRoutes.ToArray();
            if (enumerable.Any())
            {
                double avgServiceLevel =
                    enumerable.Where(r => r is PassengerRoute)
                              .Average(r => ((PassengerRoute) r).GetServiceLevel(AirlinerClass.ClassType.EconomyClass));

                RouteClassesConfiguration configuration = GetRouteConfiguration(route);

                Array types = Enum.GetValues(typeof (RouteFacility.FacilityType));

                int ct = 0;
                while (avgServiceLevel > route.GetServiceLevel(AirlinerClass.ClassType.EconomyClass)
                       && ct < types.Length)
                {
                    var type = (RouteFacility.FacilityType) types.GetValue(ct);

                    RouteFacility currentFacility =
                        route.GetRouteAirlinerClass(AirlinerClass.ClassType.EconomyClass).GetFacility(type);

                    List<RouteFacility> facilities =
                        RouteFacilities.GetFacilities(type).OrderBy(f => f.ServiceLevel).ToList();

                    int index = facilities.IndexOf(currentFacility);

                    if (index + 1 < facilities.Count)
                    {
                        route.GetRouteAirlinerClass(AirlinerClass.ClassType.EconomyClass)
                             .AddFacility(facilities[index + 1]);
                    }

                    ct++;
                }
            }
            else
            {
                Array types = Enum.GetValues(typeof (RouteFacility.FacilityType));
                double currentServiceLevel = route.GetServiceLevel(AirlinerClass.ClassType.EconomyClass);

                int ct = 0;
                while (currentServiceLevel + 50 > route.GetServiceLevel(AirlinerClass.ClassType.EconomyClass)
                       && ct < types.Length)
                {
                    var type = (RouteFacility.FacilityType) types.GetValue(ct);

                    RouteFacility currentFacility =
                        route.GetRouteAirlinerClass(AirlinerClass.ClassType.EconomyClass).GetFacility(type);

                    List<RouteFacility> facilities =
                        RouteFacilities.GetFacilities(type).OrderBy(f => f.ServiceLevel).ToList();

                    int index = facilities.IndexOf(currentFacility);

                    if (index + 1 < facilities.Count)
                    {
                        route.GetRouteAirlinerClass(AirlinerClass.ClassType.EconomyClass)
                             .AddFacility(facilities[index + 1]);
                    }

                    ct++;
                }
            }
        }

        //checks for building airport facilities for the airline
        private static void CheckForAirlineAirportFacilities(Airline airline)
        {
            int minRoutesForTicketOffice = 3 + (int) airline.Mentality;
            List<Airport> airports =
                airline.Airports.FindAll(
                    a => AirlineHelpers.GetAirportOutboundRoutes(airline, a) >= minRoutesForTicketOffice);

            foreach (Airport airport in airports)
            {
                Boolean allianceHasTicketOffice = airline.Alliances != null && airline.Alliances.SelectMany(a => a.Members)
                                                                                      .Any(
                                                                                          m =>
                                                                                          airport.GetAirlineAirportFacility(m.Airline, AirportFacility.FacilityType.TicketOffice)
                                                                                                 .Facility.TypeLevel > 0);

                if (
                    airport.GetAirlineAirportFacility(airline, AirportFacility.FacilityType.TicketOffice)
                           .Facility.TypeLevel == 0 && !allianceHasTicketOffice
                    && !airport.HasContractType(airline, AirportContract.ContractType.FullService)
                    && !airport.HasContractType(airline, AirportContract.ContractType.MediumService))
                {
                    AirportFacility facility =
                        AirportFacilities.GetFacilities(AirportFacility.FacilityType.TicketOffice)
                                         .Find(f => f.TypeLevel == 1);
                    double price = facility.Price;

                    if (!airport.IsBuildingFacility(airline, AirportFacility.FacilityType.TicketOffice))
                    {
                        if (airport.Profile.Country != airline.Profile.Country)
                        {
                            price = price*1.25;
                        }

                        AirlineHelpers.AddAirlineInvoice(
                            airline,
                            GameObject.GetInstance().GameTime,
                            Invoice.InvoiceType.Purchases,
                            -price);

                        airport.AddAirportFacility(
                            airline,
                            facility,
                            GameObject.GetInstance().GameTime.AddDays(facility.BuildingDays));
                    }
                }
            }
        }

        private static void CheckForAirlineAlliance(Airline airline)
        {
            int airlineAlliances = airline.Alliances.Count;

            if (airlineAlliances == 0)
            {
                const int newAllianceInterval = 10000;
                Boolean newAlliance = Rnd.Next(newAllianceInterval) == 0;

                if (newAlliance)
                {
                    Alliance alliance = GetAirlineAlliance(airline);

                    if (alliance == null)
                    {
                        //creates a new alliance for the airline
                        CreateNewAlliance(airline);
                    }
                        //joins an existing alliance
                    else
                    {
                        if (alliance.IsHumanAlliance)
                        {
                            alliance.AddPendingMember(
                                new PendingAllianceMember(
                                    GameObject.GetInstance().GameTime,
                                    alliance,
                                    airline,
                                    PendingAllianceMember.AcceptType.Request));
                            GameObject.GetInstance()
                                      .NewsBox.AddNews(
                                          new News(
                                              News.NewsType.AllianceNews,
                                              GameObject.GetInstance().GameTime,
                                              "Request to join alliance",
                                              $"[LI airline={airline.Profile.IATACode}] has requested to joined {alliance.Name}. The request can be accepted or declined on the alliance page"));
                        }
                        else
                        {
                            if (CanJoinAlliance(airline, alliance))
                            {
                                alliance.AddMember(new AllianceMember(airline, GameObject.GetInstance().GameTime));
                                GameObject.GetInstance()
                                          .NewsBox.AddNews(
                                              new News(
                                                  News.NewsType.AllianceNews,
                                                  GameObject.GetInstance().GameTime,
                                                  "Joined alliance",
                                                  $"[LI airline={airline.Profile.IATACode}] has joined {alliance.Name}"));
                            }
                        }
                    }
                }
            }
            else
            {
                CheckForInviteToAlliance(airline);
            }
        }

        private static void CheckForAirlineCodesharing(Airline airline)
        {
            int airlineCodesharings = airline.Codeshares.Count;

            double newCodesharingInterval = 0;
            switch (airline.Mentality)
            {
                case Airline.AirlineMentality.Aggressive:
                    newCodesharingInterval = 85000;
                    break;
                case Airline.AirlineMentality.Moderate:
                    newCodesharingInterval = 850000;
                    break;
                case Airline.AirlineMentality.Safe:
                    newCodesharingInterval = 8500000;
                    break;
            }
            newCodesharingInterval *= GameObject.GetInstance().Difficulty.AILevel;

            Boolean newCodesharing = !airline.IsSubsidiary
                                     && Rnd.Next(Convert.ToInt32(newCodesharingInterval)*(airlineCodesharings + 1))
                                     == 0;

            if (newCodesharing)
            {
                InviteToCodesharing(airline);
            }
        }

        //checks for any airliners without routes
        private static void CheckForAirlinersWithoutRoutes(Airline airline)
        {
            lock (airline.Fleet)
            {
                var fleet =
                    new List<FleetAirliner>(
                        airline.Fleet.FindAll(
                            a => a.Airliner.BuiltDate <= GameObject.GetInstance().GameTime && !a.HasRoute));
                int max = fleet.Count;

                Boolean outlease = max > 0 && Rnd.Next(1000/max) == 0;

                if (outlease)
                {
                    IOrderedEnumerable<FleetAirliner> sFleet = fleet.OrderBy(f => f.Airliner.BuiltDate.Year);
                    sFleet.ToList()[0].Airliner.Status = Airliner.StatusTypes.Leasing;
                }

                fleet =
                    new List<FleetAirliner>(
                        airline.Fleet.FindAll(
                            a => a.Airliner.BuiltDate <= GameObject.GetInstance().GameTime && !a.HasRoute));

                if (fleet.Count > 0)
                    CreateNewRoute(airline);
            }
        }

        private static void CheckForInviteToAlliance(Airline airline)
        {
            Alliance alliance = airline.Alliances[0];

            int members = alliance.Members.Count;
            const int inviteToAllianceInterval = 100000;
            bool inviteToAlliance = Rnd.Next(inviteToAllianceInterval*members) == 0;

            if (inviteToAlliance)
            {
                InviteToAlliance(airline, alliance);
            }
        }

        //checks for ordering new airliners

        //checks for etablishing a new hub
        private static void CheckForNewHub(Airline airline)
        {
            int hubs = airline.GetHubs().Count;

            int newHubInterval = 0;
            switch (airline.Mentality)
            {
                case Airline.AirlineMentality.Aggressive:
                    newHubInterval = 100000;
                    break;
                case Airline.AirlineMentality.Moderate:
                    newHubInterval = 1000000;
                    break;
                case Airline.AirlineMentality.Safe:
                    newHubInterval = 10000000;
                    break;
            }

            bool newHub = Rnd.Next(newHubInterval*hubs) == 0;

            if (newHub)
            {
                //creates a new hub for the airline
                CreateNewHub(airline);
            }
        }

        private static void CheckForNewRoute(Airline airline)
        {
            int airlinersInOrder;
            lock (airline.Fleet)
            {
                var fleet = new List<FleetAirliner>(airline.Fleet);
                airlinersInOrder = fleet.Count(a => a.Airliner.BuiltDate > GameObject.GetInstance().GameTime);
            }

            int newRouteInterval = 0;
            switch (airline.Mentality)
            {
                case Airline.AirlineMentality.Aggressive:
                    newRouteInterval = 10000;
                    break;
                case Airline.AirlineMentality.Moderate:
                    newRouteInterval = 100000;
                    break;
                case Airline.AirlineMentality.Safe:
                    newRouteInterval = 1000000;
                    break;
            }

            bool newRoute = Rnd.Next(newRouteInterval*(airlinersInOrder + 1))/1100 == 0;

            if (newRoute)
            {
                //creates a new route for the airline
                CreateNewRoute(airline);
            }
        }

        private static void CheckForOrderOfAirliners(Airline airline)
        {
            int newAirlinersInterval = 0;

            var fleet = new List<FleetAirliner>(airline.Fleet);

            int airliners = fleet.Count + 1;

            int airlinersWithoutRoute = fleet.Count(a => !a.HasRoute) + 1;

            switch (airline.Mentality)
            {
                case Airline.AirlineMentality.Aggressive:
                    newAirlinersInterval = 10000;
                    break;
                case Airline.AirlineMentality.Moderate:
                    newAirlinersInterval = 100000;
                    break;
                case Airline.AirlineMentality.Safe:
                    newAirlinersInterval = 1000000;
                    break;
            }
            bool newAirliners = Rnd.Next(newAirlinersInterval*(airliners/2)*airlinersWithoutRoute) == 0;

            if (newAirliners && airline.Profile.PrimaryPurchasing != AirlineProfile.PreferedPurchasing.Leasing)
            {
                //order new airliners for the airline
                OrderAirliners(airline);
            }
        }

        //creates a new hub for an airline

        //checks for the creation of a subsidiary airline for an airline
        private static void CheckForSubsidiaryAirline(Airline airline)
        {
            int subAirlines = airline.Subsidiaries.Count;

            double newSubInterval = 0;
            switch (airline.Mentality)
            {
                case Airline.AirlineMentality.Aggressive:
                    newSubInterval = 100000;
                    break;
                case Airline.AirlineMentality.Moderate:
                    newSubInterval = 1000000;
                    break;
                case Airline.AirlineMentality.Safe:
                    newSubInterval = 10000000;
                    break;
            }
            newSubInterval *= GameObject.GetInstance().Difficulty.AILevel;

            //newSubInterval = 0;

            bool newSub = !airline.IsSubsidiary && Rnd.Next(Convert.ToInt32(newSubInterval)*(subAirlines + 1)) == 0
                             && airline.FutureAirlines.Count > 0 && airline.Money > airline.StartMoney/5;

            if (newSub)
            {
                //creates a new subsidiary airline for the airline
                CreateSubsidiaryAirline(airline);
            }
        }

        private static void CheckForUpdateRoute(Airline airline)
        {
            int totalHours = Rnd.Next(24*7, 24*13);
            foreach (
                Route route in
                    airline.Routes.FindAll(
                        r => GameObject.GetInstance().GameTime.Subtract(r.LastUpdated).TotalHours > totalHours))
            {
                if (route.HasAirliner)
                {
                    double balance = route.GetBalance(route.LastUpdated, GameObject.GetInstance().GameTime);

                    Route.RouteType routeType = route.Type;

                    if (routeType == Route.RouteType.Mixed || routeType == Route.RouteType.Passenger || routeType == Route.RouteType.Helicopter)
                    {
                        if (balance < -1000)
                        {
                            if (route.FillingDegree > 0.50 && (((PassengerRoute) route).IncomePerPassenger < 0.50))
                            {
                                foreach (RouteAirlinerClass rac in ((PassengerRoute) route).Classes)
                                {
                                    rac.FarePrice += 10;
                                }
                                route.LastUpdated = GameObject.GetInstance().GameTime;
                            }
                            if (route.FillingDegree >= 0.2 && route.FillingDegree <= 0.50)
                            {
                                ChangeRouteServiceLevel((PassengerRoute) route);
                            }
                            if (route.FillingDegree < 0.2)
                            {
                                airline.RemoveRoute(route);

                                if (route.HasAirliner)
                                {
                                    route.GetAirliners().ForEach(a => a.RemoveRoute(route));
                                }

                                if (airline.Routes.Count == 0)
                                {
                                    CreateNewRoute(airline);
                                }

                                NewsFeeds.AddNewsFeed(
                                    new NewsFeed(
                                        GameObject.GetInstance().GameTime,
                                        string.Format(
                                            Translator.GetInstance().GetString("NewsFeed", "1002"),
                                            airline.Profile.Name,
                                            new AirportCodeConverter().Convert(route.Destination1),
                                            new AirportCodeConverter().Convert(route.Destination2))));
                            }
                        }
                    }
                    else
                    {
                        if (balance < -1000)
                        {
                            if (route.FillingDegree > 0.45)
                            {
                                ((CargoRoute) route).PricePerUnit += 10;
                            }
                            if (route.FillingDegree <= 0.45)
                            {
                                airline.RemoveRoute(route);

                                if (route.HasAirliner)
                                {
                                    route.GetAirliners().ForEach(a => a.RemoveRoute(route));
                                }

                                if (airline.Routes.Count == 0)
                                {
                                    CreateNewRoute(airline);
                                }

                                NewsFeeds.AddNewsFeed(
                                    new NewsFeed(
                                        GameObject.GetInstance().GameTime,
                                        string.Format(
                                            Translator.GetInstance().GetString("NewsFeed", "1002"),
                                            airline.Profile.Name,
                                            new AirportCodeConverter().Convert(route.Destination1),
                                            new AirportCodeConverter().Convert(route.Destination2))));
                            }
                        }
                    }
                }
                if (route.Banned)
                {
                    airline.RemoveRoute(route);

                    if (route.HasAirliner)
                    {
                        route.GetAirliners().ForEach(a => a.RemoveRoute(route));
                    }

                    if (airline.Routes.Count == 0)
                    {
                        CreateNewRoute(airline);
                    }

                    NewsFeeds.AddNewsFeed(
                        new NewsFeed(
                            GameObject.GetInstance().GameTime,
                            string.Format(
                                Translator.GetInstance().GetString("NewsFeed", "1002"),
                                airline.Profile.Name,
                                new AirportCodeConverter().Convert(route.Destination1),
                                new AirportCodeConverter().Convert(route.Destination2))));
                }
            }
        }

        private static void CreateBusinessRouteTimeTable(Route route, FleetAirliner airliner)
        {
            TimeSpan minFlightTime =
                route.GetFlightTime(airliner.Airliner.Type)
                     .Add(new TimeSpan(FleetAirlinerHelpers.GetMinTimeBetweenFlights(airliner).Ticks));

            const int maxHours = 10 - 6; //from 06:00 to 10:00 and from 18:00 to 22:00

            int flightsPerDay = Convert.ToInt16(maxHours*60/(2*minFlightTime.TotalMinutes));

            string flightCode1 = airliner.Airliner.Airline.GetNextFlightCode(0);
            string flightCode2 = airliner.Airliner.Airline.GetNextFlightCode(1);

            route.TimeTable = CreateBusinessRouteTimeTable(route, airliner, flightsPerDay, flightCode1, flightCode2);
        }

        private static void CreateNewAlliance(Airline airline)
        {
            string name = Alliance.GenerateAllianceName();
            Airport headquarter =
                airline.Airports.FirstOrDefault(
                    a => a.GetCurrentAirportFacility(airline, AirportFacility.FacilityType.Service).TypeLevel > 0);


            if (headquarter != null)
            {
                var alliance = new Alliance(GameObject.GetInstance().GameTime, name, headquarter);
                alliance.AddMember(new AllianceMember(airline, GameObject.GetInstance().GameTime));

                Alliances.AddAlliance(alliance);

                GameObject.GetInstance()
                          .NewsBox.AddNews(
                              new News(
                                  News.NewsType.StandardNews,
                                  GameObject.GetInstance().GameTime,
                                  "New alliance",
                                  $"A new alliance: {name} has been created by [LI airline={airline.Profile.IATACode}]"));

                InviteToAlliance(airline, alliance);
            }
        }

        private static void CreateNewHub(Airline airline)
        {
            var type = HubType.TypeOfHub.FocusCity;

            if (airline.MarketFocus == AirlineFocus.Domestic
                || airline.MarketFocus == AirlineFocus.Local)
            {
                type = HubType.TypeOfHub.FocusCity;
            }

            if (airline.MarketFocus == AirlineFocus.Global)
            {
                type = HubType.TypeOfHub.Hub;
            }

            if (airline.MarketFocus == AirlineFocus.Regional)
            {
                type = HubType.TypeOfHub.RegionalHub;
            }

            List<Airport> airports = airline.Airports.FindAll(a => AirlineHelpers.CanCreateHub(airline, a, HubTypes.GetHubType(type)));

            if (airports.Count > 0)
            {
                HubType hubtype = HubTypes.GetHubType(type);

                Airport airport = (from a in airports orderby a.Profile.Size descending select a).First();

                airport.AddHub(new Hub(airline, hubtype));

                AirlineHelpers.AddAirlineInvoice(
                    airline,
                    GameObject.GetInstance().GameTime,
                    Invoice.InvoiceType.Purchases,
                    AirportHelpers.GetHubPrice(airport, hubtype));

                // NewsFeeds.AddNewsFeed(new NewsFeed(GameObject.GetInstance().GameTime, string.Format(Translator.GetInstance().GetString("NewsFeed", "1003"), airline.Profile.Name, new AirportCodeConverter().Convert(airport), airport.Profile.Town.Name, airport.Profile.Town.Country.ShortName)));
            }
        }

        private static void CreateNewRoute(Airline airline)
        {
            Airport airport = GetRouteStartDestination(airline);

            if (airport != null)
            {
                Airport destination = GetDestinationAirport(airline, airport);

                if (destination != null)
                {
                    bool doLeasing = airline.Profile.PrimaryPurchasing == AirlineProfile.PreferedPurchasing.Leasing
                                        || (airline.Profile.PrimaryPurchasing
                                            == AirlineProfile.PreferedPurchasing.Random
                                            && (Rnd.Next(5) > 1 || airline.Money < 10000000));

                    KeyValuePair<Airliner, bool>? airliner = GetAirlinerForRoute(
                        airline,
                        airport,
                        destination,
                        doLeasing,
                        airline.AirlineRouteFocus);

                    FleetAirliner fAirliner = GetFleetAirliner(airline, airport, destination);

                    if (airliner.HasValue || fAirliner != null)
                    {
                        if (!AirportHelpers.HasFreeGates(destination, airline))
                        {
                            AirportHelpers.RentGates(destination, airline, AirportContract.ContractType.LowService);
                        }

                        if (!airline.Airports.Contains(destination))
                        {
                            airline.AddAirport(destination);
                        }

                        Guid id = Guid.NewGuid();

                        Route route = null;

                        if (airline.AirlineRouteFocus == Route.RouteType.Passenger)
                        {
                            double price = PassengerHelpers.GetPassengerPrice(airport, destination);

                            route = new PassengerRoute(
                                id.ToString(),
                                airport,
                                destination,
                                GameObject.GetInstance().GameTime,
                                price);

                            RouteClassesConfiguration configuration = GetRouteConfiguration((PassengerRoute) route);

                            foreach (RouteClassConfiguration classConfiguration in configuration.GetClasses())
                            {
                                ((PassengerRoute) route).GetRouteAirlinerClass(classConfiguration.Type).FarePrice = price
                                                                                                                    *GeneralHelpers
                                                                                                                         .ClassToPriceFactor
                                                                                                                         (
                                                                                                                             classConfiguration
                                                                                                                                 .Type);

                                foreach (RouteFacility facility in classConfiguration.GetFacilities())
                                {
                                    ((PassengerRoute) route).GetRouteAirlinerClass(classConfiguration.Type)
                                                            .AddFacility(facility);
                                }
                            }
                        }
                        if (airline.AirlineRouteFocus == Route.RouteType.Helicopter)
                        {
                            double price = PassengerHelpers.GetPassengerPrice(airport, destination);

                            route = new HelicopterRoute(
                                id.ToString(),
                                airport,
                                destination,
                                GameObject.GetInstance().GameTime,
                                price);

                            RouteClassesConfiguration configuration = GetRouteConfiguration((PassengerRoute) route);

                            foreach (RouteClassConfiguration classConfiguration in configuration.GetClasses())
                            {
                                ((HelicopterRoute) route).GetRouteAirlinerClass(classConfiguration.Type).FarePrice = price
                                                                                                                     *GeneralHelpers
                                                                                                                          .ClassToPriceFactor
                                                                                                                          (
                                                                                                                              classConfiguration
                                                                                                                                  .Type);

                                foreach (RouteFacility facility in classConfiguration.GetFacilities())
                                {
                                    ((HelicopterRoute) route).GetRouteAirlinerClass(classConfiguration.Type)
                                                             .AddFacility(facility);
                                }
                            }
                        }
                        if (airline.AirlineRouteFocus == Route.RouteType.Cargo)
                        {
                            route = new CargoRoute(
                                id.ToString(),
                                airport,
                                destination,
                                GameObject.GetInstance().GameTime,
                                PassengerHelpers.GetCargoPrice(airport, destination));
                        }

                        bool isDeptOk = true;
                        bool isDestOk = true;

                        if (!AirportHelpers.HasFreeGates(airport, airline))
                        {
                            isDeptOk = AirportHelpers.RentGates(
                                airport,
                                airline,
                                AirportContract.ContractType.LowService);
                        }

                        if (!AirportHelpers.HasFreeGates(destination, airline))
                        {
                            isDestOk = AirportHelpers.RentGates(
                                airport,
                                airline,
                                AirportContract.ContractType.LowService);
                        }

                        if (isDestOk && isDeptOk)
                        {
                            bool humanHasRoute =
                                Airlines.GetHumanAirlines()
                                        .SelectMany(a => a.Routes)
                                        .ToList()
                                        .Exists(
                                            r =>
                                            route != null && ((r.Destination1 == route.Destination1
                                                               && r.Destination2 == route.Destination2)
                                                              || (r.Destination1 == route.Destination2
                                                                  && r.Destination2 == route.Destination1)));

                            if (humanHasRoute && Infrastructure.Settings.GetInstance().MailsOnAirlineRoutes)
                            {
                                if (route != null)
                                    GameObject.GetInstance()
                                              .NewsBox.AddNews(
                                                  new News(
                                                      News.NewsType.AirlineNews,
                                                      GameObject.GetInstance().GameTime,
                                                      Translator.GetInstance().GetString("News", "1013"),
                                                      string.Format(
                                                          Translator.GetInstance().GetString("News", "1013", "message"),
                                                          airline.Profile.IATACode,
                                                          route.Destination1.Profile.IATACode,
                                                          route.Destination2.Profile.IATACode)));
                            }

                            Country newDestination =
                                airline.Routes.Count(
                                    r =>
                                    r.Destination1.Profile.Country == airport.Profile.Country
                                    || r.Destination2.Profile.Country == airport.Profile.Country) == 0
                                    ? airport.Profile.Country
                                    : null;

                            newDestination =
                                airline.Routes.Count(
                                    r =>
                                    r.Destination1.Profile.Country == destination.Profile.Country
                                    || r.Destination2.Profile.Country == destination.Profile.Country) == 0
                                    ? destination.Profile.Country
                                    : newDestination;

                            if (newDestination != null && Infrastructure.Settings.GetInstance().MailsOnAirlineRoutes)
                            {
                                GameObject.GetInstance()
                                          .NewsBox.AddNews(
                                              new News(
                                                  News.NewsType.AirlineNews,
                                                  GameObject.GetInstance().GameTime,
                                                  Translator.GetInstance().GetString("News", "1014"),
                                                  string.Format(
                                                      Translator.GetInstance().GetString("News", "1014", "message"),
                                                      airline.Profile.IATACode,
                                                      ((Country) new CountryCurrentCountryConverter().Convert(newDestination))
                                                          .Name)));
                            }

                            if (!AirportHelpers.HasFreeGates(airport, airline))
                            {
                                AirportHelpers.RentGates(airport, airline, AirportContract.ContractType.LowService);
                            }

                            if (!AirportHelpers.HasFreeGates(destination, airline))
                            {
                                AirportHelpers.RentGates(airport, airline, AirportContract.ContractType.LowService);
                            }

                            //Console.WriteLine("{3}: {0} has created a route between {1} and {2}", airline.Profile.Name, route.Destination1.Profile.Name, route.Destination2.Profile.Name,GameObject.GetInstance().GameTime.ToShortDateString());

                            if (fAirliner == null)
                            {
                                if (Countries.GetCountryFromTailNumber(airliner.Value.Key.TailNumber).Name
                                    != airline.Profile.Country.Name)
                                {
                                    airliner.Value.Key.TailNumber =
                                        airline.Profile.Country.TailNumbers.GetNextTailNumber();
                                }

                                if (airliner.Value.Value) //loan
                                {
                                    double amount = airliner.Value.Key.GetPrice() - airline.Money + 20000000;

                                    var loan = new Loan(
                                        GameObject.GetInstance().GameTime,
                                        amount,
                                        120,
                                        GeneralHelpers.GetAirlineLoanRate(airline));

                                    double payment = loan.GetMonthlyPayment();

                                    airline.AddLoan(loan);
                                    AirlineHelpers.AddAirlineInvoice(
                                        airline,
                                        loan.Date,
                                        Invoice.InvoiceType.Loans,
                                        loan.Amount);
                                }
                                else
                                {
                                    if (doLeasing)
                                    {
                                        AirlineHelpers.AddAirlineInvoice(
                                            airline,
                                            GameObject.GetInstance().GameTime,
                                            Invoice.InvoiceType.Rents,
                                            -airliner.Value.Key.LeasingPrice*2);

                                        if (airliner.Value.Key.Owner != null && airliner.Value.Key.Owner.IsHuman)
                                            NewsFeeds.AddNewsFeed(new NewsFeed(GameObject.GetInstance().GameTime,
                                                                               string.Format(Translator.GetInstance().GetString("NewsFeed", "1004"), airline.Profile.Name, airliner.Value.Key.TailNumber)));
                                    }
                                    else
                                    {
                                        AirlineHelpers.AddAirlineInvoice(
                                            airline,
                                            GameObject.GetInstance().GameTime,
                                            Invoice.InvoiceType.Purchases,
                                            -airliner.Value.Key.GetPrice());
                                    }
                                }

                                fAirliner =
                                    new FleetAirliner(
                                        doLeasing
                                            ? FleetAirliner.PurchasedType.Leased
                                            : FleetAirliner.PurchasedType.Bought,
                                        GameObject.GetInstance().GameTime,
                                        airline,
                                        airliner.Value.Key,
                                        airport);
                                airline.Fleet.Add(fAirliner);

                                AirlinerHelpers.CreateAirlinerClasses(fAirliner.Airliner);
                            }

                            //NewsFeeds.AddNewsFeed(new NewsFeed(GameObject.GetInstance().GameTime, string.Format(Translator.GetInstance().GetString("NewsFeed", "1001"), airline.Profile.Name, new AirportCodeConverter().Convert(route.Destination1), new AirportCodeConverter().Convert(route.Destination2))));

                            if (route.Type == Route.RouteType.Passenger || route.Type == Route.RouteType.Mixed || route.Type == Route.RouteType.Helicopter)
                            {
                                //creates a business route
                                if (IsBusinessRoute(route, fAirliner))
                                {
                                    CreateBusinessRouteTimeTable(route, fAirliner);
                                }
                                else
                                {
                                    CreateRouteTimeTable(route, fAirliner);
                                }
                            }
                            if (route.Type == Route.RouteType.Cargo)
                            {
                                CreateCargoRouteTimeTable(route, fAirliner);
                            }

                            fAirliner.Status = FleetAirliner.AirlinerStatus.ToRouteStart;
                            AirlineHelpers.HireAirlinerPilots(fAirliner);

                            route.LastUpdated = GameObject.GetInstance().GameTime;
                        }
                        airline.AddRoute(route);

                        fAirliner?.AddRoute(route);

                        AirportFacility checkinFacility =
                            AirportFacilities.GetFacilities(AirportFacility.FacilityType.CheckIn)
                                             .Find(f => f.TypeLevel == 1);
                        AirportFacility cargoTerminal =
                            AirportFacilities.GetFacilities(AirportFacility.FacilityType.Cargo)
                                             .Find(f => f.TypeLevel > 0);

                        /*
                        if (destination.getAirportFacility(airline, AirportFacility.FacilityType.CheckIn).TypeLevel == 0)
                        {
                            destination.addAirportFacility(airline, checkinFacility, GameObject.GetInstance().GameTime);
                            AirlineHelpers.AddAirlineInvoice(airline, GameObject.GetInstance().GameTime, Invoice.InvoiceType.Purchases, -checkinFacility.Price);

                        }
                        if (airport.getAirportFacility(airline, AirportFacility.FacilityType.CheckIn).TypeLevel == 0)
                        {
                            airport.addAirportFacility(airline, checkinFacility, GameObject.GetInstance().GameTime);
                            AirlineHelpers.AddAirlineInvoice(airline, GameObject.GetInstance().GameTime, Invoice.InvoiceType.Purchases, -checkinFacility.Price);

                        }
                        */

                        if (destination.GetAirportFacility(airline, AirportFacility.FacilityType.Cargo).TypeLevel == 0
                            && destination.GetAirportFacility(null, AirportFacility.FacilityType.Cargo).TypeLevel == 0
                            && route.Type == Route.RouteType.Cargo)
                        {
                            destination.AddAirportFacility(airline, cargoTerminal, GameObject.GetInstance().GameTime);
                            AirlineHelpers.AddAirlineInvoice(
                                airline,
                                GameObject.GetInstance().GameTime,
                                Invoice.InvoiceType.Purchases,
                                -cargoTerminal.Price);
                        }

                        if (airport.GetAirportFacility(airline, AirportFacility.FacilityType.Cargo).TypeLevel == 0
                            && airport.GetAirportFacility(null, AirportFacility.FacilityType.Cargo).TypeLevel == 0
                            && route.Type == Route.RouteType.Cargo)
                        {
                            airport.AddAirportFacility(airline, cargoTerminal, GameObject.GetInstance().GameTime);
                            AirlineHelpers.AddAirlineInvoice(
                                airline,
                                GameObject.GetInstance().GameTime,
                                Invoice.InvoiceType.Purchases,
                                -cargoTerminal.Price);
                        }
                    }
                }
            }
        }

        //creates a new subsidiary airline for the airline
        private static void CreateSubsidiaryAirline(Airline airline)
        {
            FutureSubsidiaryAirline futureAirline = airline.FutureAirlines[Rnd.Next(airline.FutureAirlines.Count)];

            airline.FutureAirlines.Remove(futureAirline);

            SubsidiaryAirline sAirline = AirlineHelpers.CreateSubsidiaryAirline(
                airline,
                airline.Money/5,
                futureAirline.Name,
                futureAirline.IATA,
                futureAirline.Mentality,
                futureAirline.Market,
                futureAirline.AirlineRouteFocus,
                futureAirline.PreferedAirport);
            sAirline.Profile.AddLogo(new AirlineLogo(futureAirline.Logo));
            sAirline.Profile.Color = airline.Profile.Color;

            CreateNewRoute(sAirline);

            GameObject.GetInstance()
                      .NewsBox.AddNews(
                          new News(
                              News.NewsType.AirlineNews,
                              GameObject.GetInstance().GameTime,
                              "Created subsidiary",
                              $"[LI airline={airline.Profile.IATACode}] has created a new subsidiary airline [LI airline={sAirline.Profile.IATACode}]"));
        }

        //checks for the creation of code sharing for an airline

        //returns a "good" alliance for an airline to join
        private static Alliance GetAirlineAlliance(Airline airline)
        {
            Alliance bestAlliance = (from a in Alliances.GetAlliances()
                                     where !a.Members.ToList().Exists(m => m.Airline == airline)
                                     orderby GetAirlineAllianceScore(airline, a, true) descending
                                     select a).FirstOrDefault();

            if (bestAlliance != null && GetAirlineAllianceScore(airline, bestAlliance, true) > 50)
            {
                return bestAlliance;
            }
            return null;
        }

        //returns the "score" for an airline compared to an alliance
        private static double GetAirlineAllianceScore(Airline airline, Alliance alliance, Boolean forAlliance)
        {
            IEnumerable<Country> sameCountries =
                alliance.Members.SelectMany(m => m.Airline.Airports)
                        .Select(a => a.Profile.Country)
                        .Distinct()
                        .Intersect(airline.Airports.Select(a => a.Profile.Country).Distinct());
            IEnumerable<Airport> sameDestinations =
                alliance.Members.SelectMany(m => m.Airline.Airports).Distinct().Intersect(airline.Airports);

            double airlineRoutes = airline.Routes.Count;
            double allianceRoutes = alliance.Members.SelectMany(m => m.Airline.Routes).Count();

            double coeff = forAlliance ? allianceRoutes*10 : airlineRoutes*10;

            double score = coeff + (5 - sameCountries.Count())*5 + (5 - sameDestinations.Count())*5;

            return score;
        }

        //returns the best fit airline for an alliance
        private static Airline GetAllianceAirline(Alliance alliance)
        {
            Airline bestAirline = (from a in Airlines.GetAllAirlines()
                                   where !alliance.Members.ToList().Exists(m => m.Airline == a) && a.Alliances.Count == 0
                                   orderby GetAirlineAllianceScore(a, alliance, false) descending
                                   select a).FirstOrDefault();

            if (GetAirlineAllianceScore(bestAirline, alliance, false) > 50)
            {
                return bestAirline;
            }
            return null;
        }

        private static int GetCodesharingScore(Airline asker, Airline airline)
        {
            int diffCountries =
                asker.Airports.Select(a => a.Profile.Country)
                     .Intersect(airline.Airports.Select(a => a.Profile.Country))
                     .Distinct()
                     .Count();
            int diffRoutes = asker.Routes.Count - airline.Routes.Count;
            int coeff = asker.Airports.Select(a => a.Profile.Country).Distinct().Count()
                        > airline.Airports.Select(a => a.Profile.Country).Distinct().Count()
                            ? 1
                            : -1;
            int askerRoutes = airline.Routes.Count;

            return (diffRoutes*7) + (diffCountries*coeff*5) + (askerRoutes*3);
        }

        //creates a new alliance for an airline

        //returns an airliner from the fleet which fits a route
        private static FleetAirliner GetFleetAirliner(Airline airline, Airport destination1, Airport destination2)
        {
            //Order new airliner
            List<FleetAirliner> fleet =
                airline.Fleet.FindAll(
                    f =>
                    !f.HasRoute && f.Airliner.BuiltDate <= GameObject.GetInstance().GameTime
                    && f.Airliner.Range
                    > MathHelpers.GetDistance(
                        destination1.Profile.Coordinates.ConvertToGeoCoordinate(),
                        destination2.Profile.Coordinates.ConvertToGeoCoordinate()));

            if (fleet.Count > 0)
            {
                return (from f in fleet orderby f.Airliner.Range select f).First();
            }
            return null;
        }

        //returns the best fit for an airliner for sale for a route true for loan

        //returns a free gate for an airline
        private static Gate GetFreeAirlineGate(Airline airline, Airport airport, DayOfWeek day, TimeSpan time)
        {
            List<Gate> airlineGates = airport.Terminals.GetGates(airline);

            return airlineGates.FirstOrDefault();
        }

        private static Airport GetRouteStartDestination(Airline airline)
        {
            List<Airport> homeAirports;

            lock (airline.Airports)
            {
                homeAirports = AirlineHelpers.GetHomebases(airline);
            }
            homeAirports.AddRange(airline.GetHubs());

            Terminal.TerminalType terminaltype = airline.AirlineRouteFocus == Route.RouteType.Cargo ? Terminal.TerminalType.Cargo : Terminal.TerminalType.Passenger;

            Airport airport = homeAirports.Find(a => AirportHelpers.HasFreeGates(a, airline, terminaltype));

            if (airport == null)
            {
                airport = homeAirports.Find(a => a.Terminals.GetFreeGates(terminaltype) > 0);
                if (airport != null)
                {
                    AirportHelpers.RentGates(airport, airline, AirportContract.ContractType.LowService, terminaltype);
                }
                else
                {
                    airport = GetServiceAirport(airline);
                    if (airport != null)
                    {
                        AirportHelpers.RentGates(airport, airline, AirportContract.ContractType.LowService, terminaltype);
                    }
                }
            }

            return airport;
        }

        private static Airport GetServiceAirport(Airline airline)
        {
            AirportFacility facility =
                AirportFacilities.GetFacilities(AirportFacility.FacilityType.Service).Find(f => f.TypeLevel == 1);

            IOrderedEnumerable<Airport> airports =
                from a in
                    airline.Airports.FindAll(aa => aa.Terminals.GetFreeGates(airline.AirlineRouteFocus == Route.RouteType.Cargo ? Terminal.TerminalType.Cargo : Terminal.TerminalType.Passenger) > 0)
                orderby a.Profile.Size descending
                select a;

            if (airports.Any())
            {
                Airport airport = airports.First();

                if (airport.GetAirlineAirportFacility(airline, AirportFacility.FacilityType.Service).Facility.TypeLevel
                    == 0 && !airport.HasContractType(airline, AirportContract.ContractType.FullService))

                    if (!airport.IsBuildingFacility(airline, AirportFacility.FacilityType.Service))
                    {
                        airport.AddAirportFacility(
                            airline,
                            facility,
                            GameObject.GetInstance().GameTime.AddDays(facility.BuildingDays));

                        double price = facility.Price;

                        if (airport.Profile.Country != airline.Profile.Country)
                        {
                            price = price*1.25;
                        }

                        AirlineHelpers.AddAirlineInvoice(
                            airline,
                            GameObject.GetInstance().GameTime,
                            Invoice.InvoiceType.Purchases,
                            -price);
                    }
                return airport;
            }


            return null;
        }

        private static void InviteToAlliance(Airline airline, Alliance alliance)
        {
            Airline bestFitAirline = GetAllianceAirline(alliance);

            if (bestFitAirline != null)
            {
                if (bestFitAirline == GameObject.GetInstance().HumanAirline)
                {
                    alliance.AddPendingMember(
                        new PendingAllianceMember(
                            GameObject.GetInstance().GameTime,
                            alliance,
                            bestFitAirline,
                            PendingAllianceMember.AcceptType.Invitation));
                    GameObject.GetInstance()
                              .NewsBox.AddNews(
                                  new News(
                                      News.NewsType.AllianceNews,
                                      GameObject.GetInstance().GameTime,
                                      "Invitation to join alliance",
                                      $"[LI airline={airline.Profile.IATACode}] has invited you to join {alliance.Name}. The invitation can be accepted or declined on the alliance page"));
                }
                else
                {
                    if (DoAcceptAllianceInvitation(bestFitAirline, alliance))
                    {
                        GameObject.GetInstance()
                                  .NewsBox.AddNews(
                                      new News(
                                          News.NewsType.AllianceNews,
                                          GameObject.GetInstance().GameTime,
                                          "Joined alliance",
                                          $"[LI airline={bestFitAirline.Profile.IATACode}] has joined {alliance.Name}"));
                        alliance.AddMember(new AllianceMember(bestFitAirline, GameObject.GetInstance().GameTime));
                    }
                }
            }
        }

        private static void InviteToCodesharing(Airline airline)
        {
            //find the best airline for codesharing
            IEnumerable<Airline> airlines =
                Airlines.GetAllAirlines()
                        .Where(a => a != airline && (!a.IsSubsidiary || ((SubsidiaryAirline) a).Airline != airline));

            const int bestscore = 0;
            Airline bestAirline;

            foreach (Airline tAirline in airlines)
            {
                int score = GetCodesharingScore(airline, tAirline);

                if (score > bestscore)
                {
                    bestAirline = tAirline;
                }
            }

            const int minValue = 50;

            if (bestscore > minValue)
            {
                bool acceptInvitation = AirlineHelpers.AcceptCodesharing(
                    bestAirline,
                    airline,
                    CodeshareAgreement.CodeshareType.BothWays);

                if (acceptInvitation)
                {
                    if (bestAirline.IsHuman)
                    {
                        var agreement = new CodeshareAgreement(
                            bestAirline,
                            airline,
                            CodeshareAgreement.CodeshareType.BothWays);

                        var news = new News(
                            News.NewsType.AllianceNews,
                            GameObject.GetInstance().GameTime,
                            "Codeshare Agreement",
                            $"[LI airline={airline.Profile.IATACode}] has asked you for a codeshare agreement. Do you accept it?",
                            true) {ActionObject = agreement};
                        news.Action += news_Action;

                        GameObject.GetInstance().NewsBox.AddNews(news);
                    }
                    else
                    {
                        var agreement = new CodeshareAgreement(
                            bestAirline,
                            airline,
                            CodeshareAgreement.CodeshareType.BothWays);
                        bestAirline.AddCodeshareAgreement(agreement);
                        airline.AddCodeshareAgreement(agreement);

                        GameObject.GetInstance()
                                  .NewsBox.AddNews(
                                      new News(
                                          News.NewsType.AllianceNews,
                                          GameObject.GetInstance().GameTime,
                                          "Codeshare Agreement",
                                          $"[LI airline={airline.Profile.IATACode}] and [LI airline={bestAirline.Profile.IATACode}] have made a codeshare agreement"));
                    }
                }
            }
        }

        private static bool IsBusinessRoute(Route route, FleetAirliner airliner)
        {
            double maxBusinessRouteTime = new TimeSpan(2, 0, 0).TotalMinutes;

            TimeSpan minFlightTime =
                MathHelpers.GetFlightTime(
                    route.Destination1.Profile.Coordinates.ConvertToGeoCoordinate(),
                    route.Destination2.Profile.Coordinates.ConvertToGeoCoordinate(),
                    airliner.Airliner.Type)
                           .Add(new TimeSpan(FleetAirlinerHelpers.GetMinTimeBetweenFlights(airliner).Ticks));

            return minFlightTime.TotalMinutes <= maxBusinessRouteTime;
        }

        private static void OrderAirliners(Airline airline)
        {
            List<AirlinerType> airlineAircrafts = airline.Profile.PreferedAircrafts;

            int airliners = airline.Fleet.Count;
            int airlinersWithoutRoute = airline.Fleet.Count(a => !a.HasRoute);

            int numberToOrder = Rnd.Next(1, 3 - (int) airline.Mentality);

            List<Airport> homeAirports = AirlineHelpers.GetHomebases(airline);

            var airportsList = homeAirports.ToDictionary(a => a, a => (int) a.Profile.Size);
            //Parallel.ForEach(homeAirports, a => { airportsList.Add(a, (int)a.Profile.Size); });

            if (airportsList.Count > 0)
            {
                Airport homeAirport = GetRandomItem(airportsList);

                List<AirlinerType> types =
                    AirlinerTypes.GetTypes(
                        t =>
                        t.Produced.From <= GameObject.GetInstance().GameTime
                        && t.Produced.To >= GameObject.GetInstance().GameTime && t.Price*numberToOrder < airline.Money);

                if (airline.AirlineRouteFocus == Route.RouteType.Cargo)
                {
                    types.RemoveAll(a => a.TypeAirliner == AirlinerType.TypeOfAirliner.Passenger);
                    types.RemoveAll(a => a.TypeAirliner == AirlinerType.TypeOfAirliner.Helicopter);
                }

                if (airline.AirlineRouteFocus == Route.RouteType.Passenger)
                {
                    types.RemoveAll(a => a.TypeAirliner == AirlinerType.TypeOfAirliner.Cargo);
                    types.RemoveAll(a => a.TypeAirliner == AirlinerType.TypeOfAirliner.Helicopter);
                }
                if (airline.AirlineRouteFocus == Route.RouteType.Helicopter)
                {
                    types.RemoveAll(a => a.TypeAirliner == AirlinerType.TypeOfAirliner.Cargo);
                    types.RemoveAll(a => a.TypeAirliner == AirlinerType.TypeOfAirliner.Passenger);
                }
                types = types.OrderBy(t => t.Price).ToList();

                var list = types.ToDictionary(type => type, type => (int) ((type.Range/(Convert.ToDouble(type.Price)/100000))) + (airlineAircrafts.Contains(type) ? 10 : 0));

                /*
                Parallel.ForEach(types, t =>
                    {
                        list.Add(t, (int)((t.Range / (t.Price / 100000))));
                    });*/

                if (list.Keys.Count > 0)
                {
                    AirlinerType type = GetRandomItem(list);

                    var orders = new List<AirlinerOrder> {new AirlinerOrder(type, AirlinerHelpers.GetAirlinerClasses(type), numberToOrder, false)};

                    DateTime deliveryDate = AirlinerHelpers.GetOrderDeliveryDate(orders);
                    AirlineHelpers.OrderAirliners(airline, orders, homeAirport, deliveryDate);
                }
            }
        }

        private static void news_Action(object o)
        {
            var agreement = (CodeshareAgreement) o;

            agreement.Airline1.AddCodeshareAgreement(agreement);
            agreement.Airline2.AddCodeshareAgreement(agreement);
        }

        #endregion

        //check if an airline can join an alliance
    }
}