﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheAirline.GUIModel.HelpersModel;
using TheAirline.Helpers.Workers;
using TheAirline.Infrastructure;
using TheAirline.Models.Airliners;
using TheAirline.Models.Airlines;
using TheAirline.Models.Airports;
using TheAirline.Models.General;
using TheAirline.Models.General.Scenarios;
using TheAirline.Models.General.Statistics;
using TheAirline.Models.Routes;
using TheAirline.Views.Airline;

namespace TheAirline.Helpers
{
    //the helpers class for scenarios
    public class ScenarioHelpers
    {
        #region Public Methods and Operators

        public static void SetupScenario(Scenario scenario)
        {
            Airline airline = scenario.Airline;

            GameObject.GetInstance().DayRoundEnabled = true;
            GameObject.GetInstance().TimeZone = scenario.Homebase.Profile.TimeZone;
            GameObject.GetInstance().Difficulty = scenario.Difficulty;
            GameObject.GetInstance().GameTime = new DateTime(scenario.StartYear, 1, 1);
            GameObject.GetInstance().StartDate = GameObject.GetInstance().GameTime;
            //sets the fuel price
            GameObject.GetInstance().FuelPrice =
                Inflations.GetInflation(GameObject.GetInstance().GameTime.Year).FuelPrice;

            GameObject.GetInstance().SetHumanAirline(airline);
            GameObject.GetInstance().MainAirline = GameObject.GetInstance().HumanAirline;
            //GameObject.GetInstance().HumanAirline.Money = scenario.StartCash;

            GameObject.GetInstance().Scenario = new ScenarioObject(scenario);

            Airport airport = scenario.Homebase;

            SetupScenarioAirport(airline, airport);

            // PassengerHelpers.CreateDestinationPassengers();

            AirlinerHelpers.CreateStartUpAirliners();

            int pilotsPool = 100*Airlines.GetAllAirlines().Count;

            GeneralHelpers.CreatePilots(pilotsPool);

            int instructorsPool = 75*Airlines.GetAllAirlines().Count;

            GeneralHelpers.CreateInstructors(instructorsPool);

            SetupScenarioAirlines(scenario);
            SetupScenario();
            Setup.SetupAlliances();

            PassengerHelpers.CreateAirlineDestinationDemand();

            GeneralHelpers.CreateHolidays(GameObject.GetInstance().GameTime.Year);
            GameObjectWorker.GetInstance().Start();

            PageNavigator.NavigateTo(new PageAirline(GameObject.GetInstance().HumanAirline));

            PageNavigator.ClearNavigator();

            // GameObject.GetInstance().HumanAirline.Money = 1000000000;

            GameObject.GetInstance()
                      .NewsBox.AddNews(
                          new News(
                              News.NewsType.StandardNews,
                              GameObject.GetInstance().GameTime,
                              Translator.GetInstance().GetString("News", "1001"),
                              string.Format(
                                  Translator.GetInstance().GetString("News", "1001", "message"),
                                  GameObject.GetInstance().HumanAirline.Profile.CEO,
                                  GameObject.GetInstance().HumanAirline.Profile.IATACode)));

            Action<object> action = obj =>
                {
                    PassengerHelpers.CreateDestinationDemand();

                    SetupScenarioPassengerDemand(scenario);
                };

            Task t2 = Task.Factory.StartNew(action, "passengers");
        }

        public static void UpdateScenario(ScenarioObject scenario)
        {
            double monthsSinceStart = MathHelpers.GetMonthsBetween(
                GameObject.GetInstance().StartDate,
                GameObject.GetInstance().GameTime);

            List<ScenarioFailure> failuresToCheck =
                scenario.Scenario.Failures.FindAll(f => f.CheckMonths == 1 || f.CheckMonths.Equals(monthsSinceStart));
            foreach (ScenarioFailure failure in failuresToCheck)
            {
                bool failureOk = true;
                if (failure.Type == ScenarioFailure.FailureType.Cash)
                {
                    double totalMoney = GameObject.GetInstance().MainAirline.Money;

                    totalMoney += GameObject.GetInstance().MainAirline.Subsidiaries.Sum(s => s.Money);

                    failureOk = totalMoney > Convert.ToInt64(failure.Value);
                }
                if (failure.Type == ScenarioFailure.FailureType.Fleet)
                {
                    int fleetSize = GameObject.GetInstance().MainAirline.Fleet.Count
                                    + GameObject.GetInstance().MainAirline.Subsidiaries.Sum(s => s.Fleet.Count);
                    failureOk = fleetSize > Convert.ToInt32(failure.Value);
                }
                if (failure.Type == ScenarioFailure.FailureType.Domestic)
                {
                    int domesticDestinations =
                        GameObject.GetInstance()
                                  .MainAirline.Airports.FindAll(
                                      a => a.Profile.Country == GameObject.GetInstance().MainAirline.Profile.Country)
                                  .Count;

                    domesticDestinations +=
                        GameObject.GetInstance()
                                  .MainAirline.Subsidiaries.Sum(
                                      s => s.Airports.Count(a => a.Profile.Country == s.Profile.Country));

                    failureOk = domesticDestinations > Convert.ToInt32(failure.Value);
                }
                if (failure.Type == ScenarioFailure.FailureType.Intl)
                {
                    int intlDestinations =
                        GameObject.GetInstance()
                                  .MainAirline.Airports.FindAll(
                                      a => a.Profile.Country != GameObject.GetInstance().MainAirline.Profile.Country)
                                  .Count;

                    intlDestinations +=
                        GameObject.GetInstance()
                                  .MainAirline.Subsidiaries.Sum(
                                      s => s.Airports.Count(a => a.Profile.Country != s.Profile.Country));

                    failureOk = intlDestinations > Convert.ToInt32(failure.Value);
                }
                if (failure.Type == ScenarioFailure.FailureType.PaxGrowth)
                {
                    double paxLastYear =
                        GameObject.GetInstance()
                                  .MainAirline.Statistics.GetStatisticsValue(
                                      GameObject.GetInstance().GameTime.Year - 2,
                                      StatisticsTypes.GetStatisticsType("Passengers"))
                        + GameObject.GetInstance()
                                    .MainAirline.Subsidiaries.Sum(
                                        s =>
                                        s.Statistics.GetStatisticsValue(
                                            GameObject.GetInstance().GameTime.Year - 2,
                                            StatisticsTypes.GetStatisticsType("Passengers")));
                    double paxCurrentYear =
                        GameObject.GetInstance()
                                  .MainAirline.Statistics.GetStatisticsValue(
                                      GameObject.GetInstance().GameTime.Year - 1,
                                      StatisticsTypes.GetStatisticsType("Passengers"))
                        + GameObject.GetInstance()
                                    .MainAirline.Subsidiaries.Sum(
                                        s =>
                                        s.Statistics.GetStatisticsValue(
                                            GameObject.GetInstance().GameTime.Year - 1,
                                            StatisticsTypes.GetStatisticsType("Passengers")));

                    double change = (paxCurrentYear - paxLastYear)/paxLastYear*100;

                    failureOk = change > Convert.ToDouble(failure.Value);
                }
                if (failure.Type == ScenarioFailure.FailureType.FleetAge)
                {
                    double avgFleetAge = (GameObject.GetInstance().MainAirline.GetAverageFleetAge()
                                          + GameObject.GetInstance()
                                                      .MainAirline.Subsidiaries.Sum(s => s.GetAverageFleetAge()))
                                         /(1 + GameObject.GetInstance().MainAirline.Subsidiaries.Count);
                    failureOk = Convert.ToDouble(failure.Value) > avgFleetAge;
                }
                if (failure.Type == ScenarioFailure.FailureType.Pax)
                {
                    double totalPassengers =
                        GameObject.GetInstance()
                                  .MainAirline.Statistics.GetStatisticsValue(StatisticsTypes.GetStatisticsType("Passengers"))
                        + GameObject.GetInstance()
                                    .MainAirline.Subsidiaries.Sum(
                                        s => s.Statistics.GetStatisticsValue(StatisticsTypes.GetStatisticsType("Passengers")));

                    failureOk = Convert.ToDouble(failure.Value)*1000 < totalPassengers;
                }
                if (failure.Type == ScenarioFailure.FailureType.Bases)
                {
                    int homeBases =
                        GameObject.GetInstance()
                                  .MainAirline.Airports.FindAll(
                                      a =>
                                      a.GetCurrentAirportFacility(
                                          GameObject.GetInstance().MainAirline,
                                          AirportFacility.FacilityType.Service).TypeLevel > 0)
                                  .Count;
                    homeBases +=
                        GameObject.GetInstance()
                                  .MainAirline.Subsidiaries.Sum(
                                      s =>
                                      s.Airports.Count(
                                          a =>
                                          a.GetCurrentAirportFacility(s, AirportFacility.FacilityType.Service)
                                           .TypeLevel > 0));

                    failureOk = homeBases <= Convert.ToInt32(failure.Value);
                }
                if (failure.Type == ScenarioFailure.FailureType.Debt)
                {
                    double debt = GameObject.GetInstance().MainAirline.Loans.Sum(l => l.PaymentLeft)
                                  + GameObject.GetInstance().MainAirline.Money;
                    debt +=
                        GameObject.GetInstance()
                                  .MainAirline.Subsidiaries.Sum(s => s.Loans.Sum(l => l.PaymentLeft) + s.Money);

                    failureOk = debt <= Convert.ToDouble(failure.Value);
                }

                if (failure.Type == ScenarioFailure.FailureType.JetRation)
                {
                    double totalFleet = Convert.ToDouble(GameObject.GetInstance().MainAirline.Fleet.Count)
                                        + Convert.ToDouble(
                                            GameObject.GetInstance().MainAirline.Subsidiaries.Sum(s => s.Fleet.Count));
                    double totalJets =
                        Convert.ToDouble(
                            GameObject.GetInstance()
                                      .MainAirline.Fleet.Count(f => f.Airliner.Type.Engine == AirlinerType.TypeOfEngine.Jet))
                        + Convert.ToDouble(
                            GameObject.GetInstance()
                                      .MainAirline.Subsidiaries.Sum(
                                          s => s.Fleet.Count(f => f.Airliner.Type.Engine == AirlinerType.TypeOfEngine.Jet)));

                    double jetRation = totalJets/totalFleet;

                    failureOk = jetRation >= Convert.ToDouble(failure.Value);
                }

                if (!failureOk)
                {
                    if (failure.MonthsOfFailure == 1)
                    {
                        EndScenario(failure);
                    }
                    else
                    {
                        bool failingScenario = UpdateFailureValue(scenario, failure);

                        if (failingScenario)
                        {
                            EndScenario(failure);
                        }
                    }
                }
                //( Safety, Debt, Security,  Crime)
            }
            UpdatePassengerDemands(GameObject.GetInstance().Scenario);

            if (GameObject.GetInstance().Scenario.Scenario.EndYear == GameObject.GetInstance().GameTime.Year)
            {
                GameObject.GetInstance().Scenario.IsSuccess = true;
            }
        }

        #endregion

        #region Methods

        private static void EndScenario(ScenarioFailure failure)
        {
            GameObject.GetInstance().Scenario.ScenarioFailed = failure;
        }

        //sets up the passenger demand for a scenario

        //sets up the human airline 
        private static void SetupHumanAirline(Scenario scenario)
        {
            foreach (Airport destination in scenario.Destinations)
            {
                SetupScenarioAirport(GameObject.GetInstance().HumanAirline, destination);
            }

            foreach (var fleetAirliner in scenario.Fleet)
            {
                for (int i = 0; i < fleetAirliner.Value; i++)
                {
                    GameObject.GetInstance()
                              .HumanAirline.AddAirliner(
                                  AirlineHelpers.CreateAirliner(GameObject.GetInstance().HumanAirline, fleetAirliner.Key));
                }
            }
            foreach (ScenarioAirlineRoute route in scenario.Routes)
            {
                SetupScenarioRoute(route, GameObject.GetInstance().HumanAirline);
            }
        }

        private static void SetupOpponentAirline(ScenarioAirline airline)
        {
            AirportHelpers.RentGates(airline.Homebase, airline.Airline, AirportContract.ContractType.Full,
                                     airline.Airline.AirlineRouteFocus == Route.RouteType.Cargo ? Terminal.TerminalType.Cargo : Terminal.TerminalType.Passenger);

            AirportFacility checkinFacility =
                AirportFacilities.GetFacilities(AirportFacility.FacilityType.CheckIn).Find(f => f.TypeLevel == 1);
            AirportFacility facility =
                AirportFacilities.GetFacilities(AirportFacility.FacilityType.Service)
                                 .Find((f => f.TypeLevel == 1));

            airline.Homebase.AddAirportFacility(airline.Airline, facility, GameObject.GetInstance().GameTime);
            airline.Homebase.AddAirportFacility(airline.Airline, checkinFacility, GameObject.GetInstance().GameTime);

            foreach (ScenarioAirlineRoute saroute in airline.Routes)
            {
                SetupScenarioRoute(saroute, airline.Airline);
            }
        }

        //sets up the different scenario setting
        private static void SetupScenario()
        {
            Parallel.ForEach(
                Airports.GetAllAirports(),
                airport =>
                    {
                        foreach (Airline airline in Airlines.GetAllAirlines())
                        {
                            foreach (
                                AirportFacility.FacilityType type in Enum.GetValues(typeof (AirportFacility.FacilityType)))
                            {
                                AirportFacility noneFacility =
                                    AirportFacilities.GetFacilities(type)
                                                     .Find((facility => facility.TypeLevel == 0));

                                airport.AddAirportFacility(airline, noneFacility, GameObject.GetInstance().GameTime);
                            }
                        }
                        AirportHelpers.CreateAirportWeather(airport);
                    });

            foreach (Airline airline in Airlines.GetAllAirlines())
            {
                airline.Money = GameObject.GetInstance().StartMoney;

                if (airline.IsHuman)
                {
                    GameObject.GetInstance().HumanMoney = airline.Money;
                }

                airline.StartMoney = airline.Money;

                airline.Fees = new AirlineFees();
                airline.AddAirlinePolicy(new AirlinePolicy("Cancellation Minutes", 150));
            }
        }

        private static void SetupScenarioAirlines(Scenario scenario)
        {
            var airlines = new List<Airline> {scenario.Airline};

            foreach (ScenarioAirline airline in scenario.OpponentAirlines)
            {
                airlines.Add(airline.Airline);
                SetupOpponentAirline(airline);
            }

            Airlines.Clear();

            airlines.ForEach(Airlines.AddAirline);

            SetupHumanAirline(scenario);
        }

        private static void SetupScenarioAirport(Airline airline, Airport airport, int quantity = 2)
        {
            for (int i = 0; i < quantity; i++)
            {
                if (!AirportHelpers.HasFreeGates(airport, airline))
                {
                    AirportHelpers.RentGates(airport, airline, AirportContract.ContractType.Full);
                }
            }

            AirportFacility checkinFacility =
                AirportFacilities.GetFacilities(AirportFacility.FacilityType.CheckIn).Find(f => f.TypeLevel == 1);
            AirportFacility facility =
                AirportFacilities.GetFacilities(AirportFacility.FacilityType.Service)
                                 .Find((f => f.TypeLevel == 1));

            airport.AddAirportFacility(airline, facility, GameObject.GetInstance().GameTime);
            airport.AddAirportFacility(airline, checkinFacility, GameObject.GetInstance().GameTime);
        }

        private static void SetupScenarioPassengerDemand(Scenario scenario)
        {
            foreach (ScenarioPassengerDemand demand in scenario.PassengerDemands)
            {
                if (demand.Airport != null)
                {
                    PassengerHelpers.ChangePaxDemand(demand.Factor);
                }
                if (demand.Country != null)
                {
                    ScenarioPassengerDemand demand1 = demand;
                    PassengerHelpers.ChangePaxDemand(
                        Airports.GetAllAirports(a => a.Profile.Country == demand1.Country),
                        demand.Factor);
                }
            }
        }

        private static void SetupScenarioRoute(ScenarioAirlineRoute saroute, Airline airline)
        {
            SetupScenarioAirport(airline, saroute.Destination1, saroute.Quantity);
            SetupScenarioAirport(airline, saroute.Destination2, saroute.Quantity);

            double price = PassengerHelpers.GetPassengerPrice(saroute.Destination1, saroute.Destination2);

            for (int i = 0; i < saroute.Quantity; i++)
            {
                Guid id = Guid.NewGuid();

                var route = new PassengerRoute(
                    id.ToString(),
                    saroute.Destination1,
                    saroute.Destination2,
                    GameObject.GetInstance().GameTime,
                    price);

                RouteClassesConfiguration configuration = AIHelpers.GetRouteConfiguration(route);

                foreach (RouteClassConfiguration classConfiguration in configuration.GetClasses())
                {
                    route.GetRouteAirlinerClass(classConfiguration.Type).FarePrice = price
                                                                                     *GeneralHelpers.ClassToPriceFactor
                                                                                          (classConfiguration.Type);

                    foreach (RouteFacility rfacility in classConfiguration.GetFacilities())
                    {
                        route.GetRouteAirlinerClass(classConfiguration.Type).AddFacility(rfacility);
                    }
                }

                airline.AddRoute(route);

                FleetAirliner fAirliner = AirlineHelpers.CreateAirliner(airline, saroute.AirlinerType);
                airline.AddAirliner(fAirliner);

                fAirliner.AddRoute(route);

                AIHelpers.CreateRouteTimeTable(route, fAirliner);

                fAirliner.Status = FleetAirliner.AirlinerStatus.ToRouteStart;
                AirlineHelpers.HireAirlinerPilots(fAirliner);

                route.LastUpdated = GameObject.GetInstance().GameTime;
            }
        }

        //updates the pax demands for the scenario

        //adds another month for where the scenario parameter has not been fulfilled and returns if failing scenario
        private static bool UpdateFailureValue(ScenarioObject scenario, ScenarioFailure failure)
        {
            ScenarioFailureObject failureObject = scenario.GetScenarioFailure(failure);
            int monthsSinceLastFailure = MathHelpers.GetMonthsBetween(
                failureObject.LastFailureTime,
                GameObject.GetInstance().GameTime);

            if (monthsSinceLastFailure == 1)
            {
                failureObject.Failures++;
            }
            else
            {
                failureObject.Failures = 1;
            }

            failureObject.LastFailureTime = GameObject.GetInstance().GameTime;

            return failureObject.Failures == failure.MonthsOfFailure;
        }

        private static void UpdatePassengerDemands(ScenarioObject scenario)
        {
            foreach (ScenarioPassengerDemand demand in scenario.Scenario.PassengerDemands)
            {
                if (GameObject.GetInstance().GameTime.ToShortDateString() == demand.EndDate.ToShortDateString())
                {
                    if (demand.Airport != null)
                    {
                        PassengerHelpers.ChangePaxDemand(-demand.Factor);
                    }
                    if (demand.Country != null)
                    {
                        ScenarioPassengerDemand demand1 = demand;
                        PassengerHelpers.ChangePaxDemand(
                            Airports.GetAllAirports(a => a.Profile.Country == demand1.Country),
                            -demand.Factor);
                    }
                }
            }
        }

        #endregion

        //sets up a scenario

        //ends a scenario
    }
}