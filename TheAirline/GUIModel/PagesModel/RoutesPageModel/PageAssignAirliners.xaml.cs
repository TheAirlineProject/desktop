﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TheAirline.GraphicsModel.UserControlModel.MessageBoxModel;
using TheAirline.GraphicsModel.UserControlModel.PopUpWindowsModel;
using TheAirline.GUIModel.HelpersModel;
using TheAirline.Model.AirlinerModel;
using TheAirline.Model.GeneralModel;
using TheAirline.Model.PassengerModel;
using TheAirline.Model.PilotModel;

namespace TheAirline.GUIModel.PagesModel.RoutesPageModel
{
    /// <summary>
    /// Interaction logic for PageAssignAirliners.xaml
    /// </summary>
    public partial class PageAssignAirliners : Page
    {
        public List<FlightRestriction> Restrictions { get; set; }
        public List<FleetAirliner> Airliners { get; set; }
        public PageAssignAirliners()
        {
            this.Restrictions = FlightRestrictions.GetRestrictions().FindAll(r => r.StartDate < GameObject.GetInstance().GameTime && r.EndDate > GameObject.GetInstance().GameTime);
            this.Airliners = GameObject.GetInstance().HumanAirline.Fleet;

            this.Loaded += PageAssignAirliners_Loaded;
            
            InitializeComponent();
        }

        private void PageAssignAirliners_Loaded(object sender, RoutedEventArgs e)
        {
            TabControl tab_main = UIHelpers.FindChild<TabControl>(this.Tag as Page, "tabMenu");

            if (tab_main != null)
            {
                var matchingItem =
     tab_main.Items.Cast<TabItem>()
       .Where(item => item.Tag.ToString() == "Route")
       .FirstOrDefault();

                matchingItem.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {

        }

        private void hlAirliner_Click(object sender, RoutedEventArgs e)
        {
            FleetAirliner airliner = (FleetAirliner)((Hyperlink)sender).Tag;

            if (airliner.NumberOfPilots == airliner.Airliner.Type.CockpitCrew)
            {

                PopUpAirlinerAutoRoutes.ShowPopUp(airliner);
                ICollectionView view = CollectionViewSource.GetDefaultView(lvFleet.ItemsSource);
                view.Refresh();
            }
            else
            {
                int missingPilots = airliner.Airliner.Type.CockpitCrew - airliner.NumberOfPilots;
                if (GameObject.GetInstance().HumanAirline.Pilots.FindAll(p => p.Airliner == null).Count >= missingPilots)
                {
                    WPFMessageBoxResult result = WPFMessageBox.Show(Translator.GetInstance().GetString("MessageBox", "2505"), string.Format(Translator.GetInstance().GetString("MessageBox", "2505", "message")), WPFMessageBoxButtons.YesNo);

                    if (result == WPFMessageBoxResult.Yes)
                    {
                        var unassignedPilots = GameObject.GetInstance().HumanAirline.Pilots.FindAll(p => p.Airliner == null).ToList();

                        for (int i = 0; i < missingPilots; i++)
                        {
                            unassignedPilots[i].Airliner = airliner;
                            airliner.addPilot(unassignedPilots[i]);
                        }

                        // PopUpAirlinerRoutes.ShowPopUp(airliner, true);
                        PopUpAirlinerAutoRoutes.ShowPopUp(airliner);

                        ICollectionView view = CollectionViewSource.GetDefaultView(lvFleet.ItemsSource);
                        view.Refresh();
                    }
                }
                else
                {
                    Random rnd = new Random();
                    WPFMessageBoxResult result = WPFMessageBox.Show(Translator.GetInstance().GetString("MessageBox", "2506"), string.Format(Translator.GetInstance().GetString("MessageBox", "2506", "message"), missingPilots), WPFMessageBoxButtons.YesNo);

                    if (result == WPFMessageBoxResult.Yes)
                    {
                        while (airliner.Airliner.Type.CockpitCrew > airliner.NumberOfPilots)
                        {
                            var pilots = Pilots.GetUnassignedPilots(p => p.Profile.Town.Country == airliner.Airliner.Airline.Profile.Country);

                            if (pilots.Count == 0)
                                pilots = Pilots.GetUnassignedPilots(p => p.Profile.Town.Country.Region == airliner.Airliner.Airline.Profile.Country.Region);

                            if (pilots.Count == 0)
                                pilots = Pilots.GetUnassignedPilots();

                            Pilot pilot = pilots.First();

                            airliner.Airliner.Airline.addPilot(pilot);
                            pilot.Airliner = airliner;
                            airliner.addPilot(pilot);
                        }

                        PopUpAirlinerAutoRoutes.ShowPopUp(airliner);

                        ICollectionView view = CollectionViewSource.GetDefaultView(lvFleet.ItemsSource);
                        view.Refresh();


                    }
                }
            }
        }
    }
}