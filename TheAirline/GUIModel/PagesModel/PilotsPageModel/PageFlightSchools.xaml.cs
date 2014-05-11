﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using TheAirline.Model.AirportModel;
using TheAirline.Model.GeneralModel;
using TheAirline.Model.GeneralModel.Helpers;
using TheAirline.Model.PilotModel;

namespace TheAirline.GUIModel.PagesModel.PilotsPageModel
{
    /// <summary>
    /// Interaction logic for PageFlightSchools.xaml
    /// </summary>
    public partial class PageFlightSchools : Page
    {
        public ObservableCollection<Instructor> AllInstructors { get; set; }
        public ObservableCollection<FlightSchool> FlightSchools { get; set; }
        public PageFlightSchools()
        {

            this.AllInstructors = new ObservableCollection<Instructor>();
            this.FlightSchools = new ObservableCollection<FlightSchool>();

            Instructors.GetUnassignedInstructors().ForEach(i => this.AllInstructors.Add(i));
            GameObject.GetInstance().HumanAirline.FlightSchools.ForEach(f => this.FlightSchools.Add(f));

            this.Loaded += PageFlightSchools_Loaded;
            InitializeComponent();
        }

        private void PageFlightSchools_Loaded(object sender, RoutedEventArgs e)
        {
            TabControl tab_main = UIHelpers.FindChild<TabControl>(this.Tag as Page, "tabMenu");

            if (tab_main != null)
            {
                var matchingItem =
     tab_main.Items.Cast<TabItem>()
       .Where(item => item.Tag.ToString() == "Flightschool")
       .FirstOrDefault();

                matchingItem.Visibility = System.Windows.Visibility.Collapsed;

                matchingItem =
    tab_main.Items.Cast<TabItem>()
      .Where(item => item.Tag.ToString() == "Pilot")
      .FirstOrDefault();

                matchingItem.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void lnkFlightSchool_Click(object sender, RoutedEventArgs e)
        {
            FlightSchool fs = (FlightSchool)((Hyperlink)sender).Tag;

            TabControl tab_main = UIHelpers.FindChild<TabControl>(this.Tag as Page, "tabMenu");

            if (tab_main != null)
            {
                var matchingItem =
     tab_main.Items.Cast<TabItem>()
       .Where(item => item.Tag.ToString() == "Flightschool")
       .FirstOrDefault();

                //matchingItem.IsSelected = true;
                matchingItem.Header = fs.Name;
                matchingItem.Visibility = System.Windows.Visibility.Visible;

                tab_main.SelectedItem = matchingItem;
            }

            Frame frmContent = UIHelpers.FindChild<Frame>(this.Tag as Page, "frmContent");

            if (frmContent != null)
            {
                frmContent.Navigate(new PageShowFlightSchool(fs) { Tag = this.Tag });

            }
        }
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            FlightSchool fs = (FlightSchool)((Button)sender).Tag;

            WPFMessageBoxResult result = WPFMessageBox.Show(Translator.GetInstance().GetString("MessageBox", "2812"), string.Format(Translator.GetInstance().GetString("MessageBox", "2812", "message"), fs.Airport.Profile.Name), WPFMessageBoxButtons.YesNo);

            if (result == WPFMessageBoxResult.Yes)
            {
                GameObject.GetInstance().HumanAirline.removeFlightSchool(fs);
                this.FlightSchools.Remove(fs);

                if (GameObject.GetInstance().HumanAirline.FlightSchools.Count > 0)
                {
                    ComboBox cbFlightSchools = new ComboBox();
                    cbFlightSchools.SetResourceReference(ComboBox.StyleProperty, "ComboBoxTransparentStyle");
                    cbFlightSchools.Width = 200;
                    cbFlightSchools.SelectedValuePath = "Name";
                    cbFlightSchools.DisplayMemberPath = "Name";
                    cbFlightSchools.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

                    foreach (FlightSchool fSchool in GameObject.GetInstance().HumanAirline.FlightSchools.Where(f => f.NumberOfInstructors + fs.NumberOfInstructors <= FlightSchool.MaxNumberOfInstructors && f != fs))
                        cbFlightSchools.Items.Add(fSchool);

                    cbFlightSchools.SelectedIndex = 0;

                    if (PopUpSingleElement.ShowPopUp(Translator.GetInstance().GetString("PageFlightSchools", "1009"), cbFlightSchools) == PopUpSingleElement.ButtonSelected.OK && cbFlightSchools.SelectedItem != null)
                    {
                        FlightSchool nFlightSchool = (FlightSchool)cbFlightSchools.SelectedItem;

                        foreach (Instructor instructor in fs.Instructors)
                        {
                            instructor.FlightSchool = nFlightSchool;
                            nFlightSchool.addInstructor(instructor);
                        }

                        var aircrafts = new List<TrainingAircraft>(fs.TrainingAircrafts);
                        foreach (TrainingAircraft aircraft in aircrafts)
                        {
                            aircraft.FlightSchool = nFlightSchool;
                            nFlightSchool.addTrainingAircraft(aircraft);
                        }

                        ICollectionView view = CollectionViewSource.GetDefaultView(lvFlightSchools.ItemsSource);
                        view.Refresh();
                    }
                    else
                    {
                        foreach (TrainingAircraft aircraft in fs.TrainingAircrafts)
                        {
                            double price = aircraft.Type.Price * 0.75;
                            AirlineHelpers.AddAirlineInvoice(GameObject.GetInstance().HumanAirline, GameObject.GetInstance().GameTime, Invoice.InvoiceType.Airline_Expenses, price);

                        }
                        // if terminal built on behalf of subsidiary and closing sub before terminal is already built: should't the main airline receive it then or only the airport? I did it and the gates were only useable by sub, not possible to hire these gates afterwards by main airline.

                    }
                }



            }

        }
        private void btnBuild_Click(object sender, RoutedEventArgs e)
        {
            double price = GeneralHelpers.GetInflationPrice(267050);

            ComboBox cbAirport = new ComboBox();
            cbAirport.SetResourceReference(ComboBox.StyleProperty, "ComboBoxTransparentStyle");
            cbAirport.Width = 200;
            cbAirport.SelectedValuePath = "Profile.Town.Name";
            cbAirport.DisplayMemberPath = "Profile.Town.Name";
            cbAirport.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

            List<Airport> homeAirports = GameObject.GetInstance().HumanAirline.Airports.FindAll(a => a.getCurrentAirportFacility(GameObject.GetInstance().HumanAirline, AirportFacility.FacilityType.Service).TypeLevel > 0);
            homeAirports.AddRange(GameObject.GetInstance().HumanAirline.Airports.FindAll(a => a.IsHub)); //hubs
            homeAirports = homeAirports.Distinct().ToList();


            foreach (Airport airport in homeAirports)
            {
                if (GameObject.GetInstance().HumanAirline.FlightSchools.Find(f => f.Airport == airport) == null)
                    cbAirport.Items.Add(airport);
            }

            cbAirport.SelectedIndex = 0;

            if (PopUpSingleElement.ShowPopUp(Translator.GetInstance().GetString("PagePilots", "1004"), cbAirport) == PopUpSingleElement.ButtonSelected.OK && cbAirport.SelectedItem != null)
            {
                Airport airport = (Airport)cbAirport.SelectedItem;

                FlightSchool fs = new FlightSchool(airport);

                GameObject.GetInstance().HumanAirline.addFlightSchool(fs);
                this.FlightSchools.Add(fs);

                AirlineHelpers.AddAirlineInvoice(GameObject.GetInstance().HumanAirline, GameObject.GetInstance().GameTime, Invoice.InvoiceType.Airline_Expenses, -price);

                ICollectionView view = CollectionViewSource.GetDefaultView(lvInstructors.ItemsSource);
                view.Refresh();


            }
        }

        private void btnHire_Click(object sender, RoutedEventArgs e)
        {
            Instructor instructor = (Instructor)((Button)sender).Tag;

            ComboBox cbFlightSchools = new ComboBox();
            cbFlightSchools.SetResourceReference(ComboBox.StyleProperty, "ComboBoxTransparentStyle");
            cbFlightSchools.Width = 200;
            cbFlightSchools.SelectedValuePath = "Name";
            cbFlightSchools.DisplayMemberPath = "Name";
            cbFlightSchools.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

            foreach (FlightSchool fs in GameObject.GetInstance().HumanAirline.FlightSchools.Where(f => f.NumberOfInstructors < FlightSchool.MaxNumberOfInstructors))
                cbFlightSchools.Items.Add(fs);

            cbFlightSchools.SelectedIndex = 0;

            if (PopUpSingleElement.ShowPopUp(Translator.GetInstance().GetString("PanelFlightSchool", "1008"), cbFlightSchools) == PopUpSingleElement.ButtonSelected.OK && cbFlightSchools.SelectedItem != null)
            {
                FlightSchool flightSchool = (FlightSchool)cbFlightSchools.SelectedItem;

                flightSchool.addInstructor(instructor);
                instructor.FlightSchool = flightSchool;

                this.AllInstructors.Remove(instructor);

                ICollectionView view = CollectionViewSource.GetDefaultView(lvFlightSchools.ItemsSource);
                view.Refresh();
            }
        }


    }
}
