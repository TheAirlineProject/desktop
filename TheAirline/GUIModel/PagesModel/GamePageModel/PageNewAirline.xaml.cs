﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using Microsoft.Win32;
using TheAirline.GraphicsModel.UserControlModel.MessageBoxModel;
using TheAirline.Infrastructure;
using TheAirline.Infrastructure.Enums;
using TheAirline.Models.Airlines;
using TheAirline.Models.Airports;
using TheAirline.Models.General;
using TheAirline.Models.General.Countries;
using TheAirline.Models.Routes;

namespace TheAirline.GUIModel.PagesModel.GamePageModel
{
    /// <summary>
    ///     Interaction logic for PageNewAirline.xaml
    /// </summary>
    public partial class PageNewAirline : Page
    {
        #region Fields

        private string logoPath;

        #endregion

        #region Constructors and Destructors

        public PageNewAirline()
        {
            Colors = new List<PropertyInfo>();
            AllCountries = Countries.GetCountries().OrderBy(c => c.Name).ToList();

            foreach (PropertyInfo c in typeof(Colors).GetProperties())
            {
                Colors.Add(c);
            }

            InitializeComponent();

            logoPath = AppSettings.GetDataPath() + "\\graphics\\airlinelogos\\default.png";
            imgLogo.Source = new BitmapImage(new Uri(logoPath, UriKind.RelativeOrAbsolute));

            txtCEO.Text = string.Format(
                "{0} {1}",
                Names.GetInstance().GetRandomFirstName(AllCountries[0]),
                Names.GetInstance().GetRandomLastName(AllCountries[0]));
        }

        #endregion

        #region Public Properties

        public List<Country> AllCountries { get; set; }

        public List<PropertyInfo> Colors { get; set; }

        #endregion

        #region Methods

        private void btnCreateAirline_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            string iata = txtIATA.Text.Trim().ToUpper();

            string pattern = @"^[A-Za-z0-9]+$";
            var regex = new Regex(pattern);

            if (name.Length > 0 && iata.Length == 2 && regex.IsMatch(iata))
            {
                Airline airline = Airlines.GetAirline(iata);

                if (airline != null)
                {
                    WPFMessageBoxResult result =
                        WPFMessageBox.Show(
                            Translator.GetInstance().GetString("MessageBox", "2401"),
                            string.Format(
                                Translator.GetInstance().GetString("MessageBox", "2401", "message"),
                                airline.Profile.Name),
                            WPFMessageBoxButtons.YesNo);

                    if (result == WPFMessageBoxResult.Yes)
                    {
                        createAirline();
                    }
                }
                else
                {
                    WPFMessageBoxResult result =
                        WPFMessageBox.Show(
                            Translator.GetInstance().GetString("MessageBox", "2402"),
                            Translator.GetInstance().GetString("MessageBox", "2402", "message"),
                            WPFMessageBoxButtons.YesNo);

                    if (result == WPFMessageBoxResult.Yes)
                    {
                        createAirline();
                    }
                }
            }
            else
            {
                WPFMessageBox.Show(
                    Translator.GetInstance().GetString("MessageBox", "2404"),
                    Translator.GetInstance().GetString("MessageBox", "2404", "message"),
                    WPFMessageBoxButtons.Ok);
            }
        }

        private void btnCreateAndSave_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            string iata = txtIATA.Text.Trim().ToUpper();

            string pattern = @"^[A-Za-z0-9]+$";
            var regex = new Regex(pattern);

            if (name.Length > 0 && iata.Length == 2 && regex.IsMatch(iata))
            {
                Airline airline = Airlines.GetAirline(iata);

                if (airline != null)
                {
                    WPFMessageBoxResult result =
                        WPFMessageBox.Show(
                            Translator.GetInstance().GetString("MessageBox", "2401"),
                            string.Format(
                                Translator.GetInstance().GetString("MessageBox", "2401", "message"),
                                airline.Profile.Name),
                            WPFMessageBoxButtons.YesNo);

                    if (result == WPFMessageBoxResult.Yes)
                    {
                        Airline nAirline = createAirline();
                        saveAirline(nAirline);
                    }
                }
                else
                {
                    WPFMessageBoxResult result =
                        WPFMessageBox.Show(
                            Translator.GetInstance().GetString("MessageBox", "2402"),
                            Translator.GetInstance().GetString("MessageBox", "2402", "message"),
                            WPFMessageBoxButtons.YesNo);

                    if (result == WPFMessageBoxResult.Yes)
                    {
                        Airline nAirline = createAirline();
                        saveAirline(nAirline);
                    }
                }
            }
            else
            {
                WPFMessageBox.Show(
                    Translator.GetInstance().GetString("MessageBox", "2404"),
                    Translator.GetInstance().GetString("MessageBox", "2404", "message"),
                    WPFMessageBoxButtons.Ok);
            }
        }

        private void btnLogo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();

            dlg.DefaultExt = ".png";
            dlg.Filter = "Images (.png)|*.png";
            dlg.InitialDirectory = AppSettings.GetDataPath() + "\\graphics\\airlinelogos\\";

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                logoPath = dlg.FileName;
                imgLogo.Source = new BitmapImage(new Uri(logoPath, UriKind.RelativeOrAbsolute));
            }
        }

        private void cbCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var country = (Country)cbCountry.SelectedItem;

            cbAirport.Items.Clear();

            foreach (Airport airport in Airports.GetAirports(country).OrderBy(a => a.Profile.Name))
            {
                cbAirport.Items.Add(airport);
            }
        }

        //saves the airline

        //creates the airline
        private Airline createAirline()
        {
            string name = txtName.Text.Trim();
            string iata = txtIATA.Text.Trim().ToUpper();
            string ceo = txtCEO.Text.Trim();

            Airline tAirline = Airlines.GetAirline(iata);

            if (tAirline != null)
            {
                Airlines.RemoveAirline(tAirline);
            }

            var country = (Country)cbCountry.SelectedItem;
            string color = ((PropertyInfo)cbColor.SelectedItem).Name;

            var profile = new AirlineProfile(name, iata, color, ceo, false, 1950, 2199);
            profile.Countries = new List<Country> { country };
            profile.Country = country;
            profile.AddLogo(new AirlineLogo(logoPath));
            profile.PreferedAirport = cbAirport.SelectedItem != null ? (Airport)cbAirport.SelectedItem : null;

            Route.RouteType focus = rbPassengerType.IsChecked.Value
                ? Route.RouteType.Passenger
                : Route.RouteType.Cargo;

            var airline = new Airline(
                profile,
                Airline.AirlineMentality.Aggressive,
                AirlineFocus.Local,
                Airline.AirlineLicense.Domestic,
                focus);

            Airlines.AddAirline(airline);

            WPFMessageBox.Show(
                Translator.GetInstance().GetString("MessageBox", "2405"),
                Translator.GetInstance().GetString("MessageBox", "2405", "message"),
                WPFMessageBoxButtons.Ok);

            return airline;
        }

        private void saveAirline(Airline airline)
        {
            string directory = AppSettings.GetCommonApplicationDataPath() + "\\custom airlines";

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string path = string.Format("{0}\\{1}.xml", directory, airline.Profile.Name);

            //saves the airline
            var xmlDoc = new XmlDocument();

            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(xmlDeclaration);

            XmlElement root = xmlDoc.CreateElement("airline");
            xmlDoc.AppendChild(root);

            XmlElement profile = xmlDoc.CreateElement("profile");
            root.AppendChild(profile);

            profile.SetAttribute("name", airline.Profile.Name);
            profile.SetAttribute("iata", airline.Profile.IATACode);
            profile.SetAttribute("color", airline.Profile.Color);
            profile.SetAttribute("country", airline.Profile.Country.Uid);
            profile.SetAttribute("CEO", airline.Profile.CEO);
            profile.SetAttribute("mentality", airline.Mentality.ToString());
            profile.SetAttribute("market", airline.MarketFocus.ToString());
            profile.SetAttribute(
                "preferedairport",
                airline.Profile.PreferedAirport != null ? airline.Profile.PreferedAirport.Profile.IATACode : "");
            profile.SetAttribute("routefocus", airline.AirlineRouteFocus.ToString());

            XmlElement info = xmlDoc.CreateElement("info");
            root.AppendChild(info);

            info.SetAttribute("real", false.ToString());
            info.SetAttribute("from", "1960");
            info.SetAttribute("to", "2199");

            xmlDoc.Save(path);

            //saves the logo
            string logopath = string.Format("{0}\\{1}.png", directory, airline.Profile.IATACode);

            // Construct a new image from the GIF file.
            var logo = new Bitmap(airline.Profile.Logo);
            logo.Save(logopath, ImageFormat.Png);
        }

        #endregion
    }
}