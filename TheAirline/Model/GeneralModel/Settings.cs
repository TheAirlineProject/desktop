﻿namespace TheAirline.Model.GeneralModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    //the class for some game settings
    [Serializable]
    public class Settings : ISerializable
    {
        #region Static Fields

        private static Settings Instance;

        #endregion

        #region Constructors and Destructors

        private Settings()
        {
            this.AirportCodeDisplay = AirportCode.IATA;
            this.DifficultyDisplay = Difficulty.Normal;
            this.GameSpeed = GeneralHelpers.GameSpeedValue.Normal;
            this.MailsOnLandings = false;
            this.MailsOnAirlineRoutes = false;
            this.MailsOnBadWeather = true;
            this.MinutesPerTurn = 60;
            this.CurrencyShorten = true;
            this.Mode = ScreenMode.Windowed;

            this.ClearStats = Intervals.Monthly;
            this.AutoSave = Intervals.Never;
        }

        private Settings(SerializationInfo info, StreamingContext ctxt)
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

        //the setting for which kind of airport code to show

        #region Enums

        public enum AirportCode
        {
            IATA,

            ICAO
        }

        public enum Difficulty
        {
            Easy,

            Normal,

            Hard
        }

        public enum Intervals
        {
            Daily,

            Monthly,

            Yearly,

            Never
        }

        public enum ScreenMode
        {
            Fullscreen,

            Windowed
        }

        #endregion

        #region Public Properties

        [Versioning("airportcode")]
        public AirportCode AirportCodeDisplay { get; set; }

        [Versioning("autosave")]
        public Intervals AutoSave { get; set; }

        [Versioning("clearstats")]
        public Intervals ClearStats { get; set; }

        [Versioning("currencyshorten")]
        public Boolean CurrencyShorten { get; set; }

        [Versioning("difficulty")]
        public Difficulty DifficultyDisplay { get; set; }

        [Versioning("gamespeed")]
        public GeneralHelpers.GameSpeedValue GameSpeed { get; private set; }

        //the setting for receiving mails on landings

        [Versioning("mailsonroutes")]
        public Boolean MailsOnAirlineRoutes { get; set; }

        [Versioning("mailsonweather")]
        public Boolean MailsOnBadWeather { get; set; }

        [Versioning("mailsonlandings")]
        public Boolean MailsOnLandings { get; set; }

        [Versioning("minutes")]
        public int MinutesPerTurn { get; set; }

        [Versioning("mode")]
        public ScreenMode Mode { get; set; }

        #endregion

        //returns the settings instance

        #region Public Methods and Operators

        public static Settings GetInstance()
        {
            if (Instance == null)
            {
                Instance = new Settings();
            }
            return Instance;
        }

        public static void SetInstance(Settings instance)
        {
            Instance = instance;
        }

        //sets the speed of the game

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", 1);

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

        public void setGameSpeed(GeneralHelpers.GameSpeedValue gameSpeed)
        {
            this.GameSpeed = gameSpeed;
        }

        #endregion
    }
}