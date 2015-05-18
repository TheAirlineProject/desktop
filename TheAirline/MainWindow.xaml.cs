﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using NLog;
using TheAirline.GUIModel.HelpersModel;
using TheAirline.GUIModel.PagesModel.GamePageModel;
using TheAirline.GraphicsModel.UserControlModel.MessageBoxModel;
using TheAirline.Helpers.Workers;
using TheAirline.Infrastructure;
using TheAirline.Model.GeneralModel;
using TheAirline.Models.Airliners;
using TheAirline.Models.Airports;
using TheAirline.Models.General;

namespace TheAirline
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();

            Setup.SetupGame();

            if (Settings.GetInstance().Mode == Settings.ScreenMode.Fullscreen)
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                Focus();
            }

            PageNavigator.MainWindow = this;

            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;

            if (AppSettings.GetInstance().HasLanguage())
                frmContent.Navigate(new PageStartMenu());
            else
                frmContent.Navigate(new PageSelectLanguage());
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                WPFMessageBoxResult result = WPFMessageBox.Show(Translator.GetInstance().GetString("MessageBox", "1003"), Translator.GetInstance().GetString("MessageBox", "1003", "message"),
                                                                WPFMessageBoxButtons.YesNo);

                if (result == WPFMessageBoxResult.Yes)
                    Close();
            }
            if (e.Key == Key.F8)
            {
                string text = $"Gameobjectworker paused: {GameObjectWorker.GetInstance().IsPaused}\n";
                text += $"Gameobjectworker finished: {GameObjectWorker.GetInstance().IsFinish}\n";
                text += $"Gameobjectworker errored: {GameObjectWorker.GetInstance().IsError}\n";

                Console.WriteLine(text);

                WPFMessageBox.Show("Threads states", text, WPFMessageBoxButtons.Ok);
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F12)
            {
                //var file = new StreamWriter(AppSettings.GetCommonApplicationDataPath() + "\\theairline.log");

                if (Airports.Count() >= 5)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Airport airport = Airports.GetAllAirports()[i];

                        //file.WriteLine("Airport demand for {0} of size {1}", airport.Profile.Name, airport.Profile.Size);
                        Logger.Info("Airport demand for {0} of size {1}", airport.Profile.Name, airport.Profile.Size);

                        foreach (Airport demand in airport.GetDestinationDemands())
                        {
                            //file.WriteLine("    Demand to {0} ({2}) is {1}", demand.Profile.Name, airport.GetDestinationPassengersRate(demand, AirlinerClass.ClassType.EconomyClass),
                            //               demand.Profile.Size);
                            Logger.Info("Demand to {0} ({2}) is {1}", demand.Profile.Name, airport.GetDestinationPassengersRate(demand, AirlinerClass.ClassType.EconomyClass), demand.Profile.Size);
                        }
                    }
                }

                WPFMessageBox.Show("Demand has been dumped", "The demand has been dumped to the log file", WPFMessageBoxButtons.Ok);

                //file.Close();
            }
        }

        //clears the navigator
        public void ClearNavigator()
        {
            frmContent.NavigationService.LoadCompleted += NavigationService_LoadCompleted;

            // Remove back entries
            while (frmContent.NavigationService.CanGoBack)
                frmContent.NavigationService.RemoveBackEntry();
        }

        private void NavigationService_LoadCompleted(object sender, NavigationEventArgs e)
        {
            frmContent.NavigationService.RemoveBackEntry();

            frmContent.NavigationService.LoadCompleted -= NavigationService_LoadCompleted;
        }

        //returns if navigator can go forward
        public Boolean CanGoForward()
        {
            return frmContent.NavigationService.CanGoForward;
        }

        //returns if navigator can go back
        public Boolean CanGoBack()
        {
            return frmContent.NavigationService.CanGoBack;
        }

        //navigates to a new page
        public void NavigateTo(Page page)
        {
            frmContent.Navigate(page);
            frmContent.NavigationService.RemoveBackEntry();
        }

        //moves the navigator forward
        public void NavigateForward()
        {
            if (frmContent.NavigationService.CanGoForward)
                frmContent.NavigationService.GoForward();
        }

        //moves the navigator back
        public void NavigateBack()
        {
            if (frmContent.NavigationService.CanGoBack)
                frmContent.NavigationService.GoBack();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //social groups + passenger happiness
        }
    }
}