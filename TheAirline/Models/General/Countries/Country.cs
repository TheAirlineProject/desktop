﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TheAirline.GUIModel.HelpersModel;
using TheAirline.Infrastructure;

namespace TheAirline.Models.General.Countries
{
    //the class for a country
    public class Country : BaseUnit
    {
        #region Constructors and Destructors

        public Country(string section, string uid, string shortName, Region region, string tailNumberFormat) : base(uid, shortName)
        {
            Section = section;
            Region = region;
            TailNumberFormat = tailNumberFormat;
            //TailNumbers = new CountryTailNumber(this);
            //Currencies = new List<CountryCurrency>();
        }

        protected Country(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {
        }

        public Country() { }

        #endregion

        #region Public Properties

        public int Id { get; set; }

        public static string Section { get; set; }

        public List<Currency> Currencies { get; set; }

        public bool HasLocalCurrency => Currencies.Count > 0;

        //public override string Name => Translator.GetInstance().GetString(Section, Uid);

        public Region Region { get; set; }

        //the format used for the tail number
        public string TailNumberFormat { get; set; }

        public CountryTailNumber TailNumbers { get; set; }

        public string IsoName { get; set; }

        #endregion

        //#region Public Methods and Operators
        ////adds a currency to the country
        public void AddCurrency(Currency currency)
        {
            Currencies.Add(currency);
        }

        ////returns the current currency
        public Currency GetCurrency(DateTime date)
        {
            if (Currencies.Exists(c => c.From <= date && c.To > date))
            {
                return Currencies.Find(c => c.From <= date && c.To > date);
            }

            return null;
        }

        //#endregion
    }

    [Serializable]
    //the class for a country which is a territory of another country
    public class TerritoryCountry : Country
    {
        #region Public Properties

        [Versioning("maincountry")]
        public Country MainCountry { get; set; }

        #endregion

        #region Public Methods and Operators

        //public override void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    info.AddValue("version", 1);

        //    base.GetObjectData(info, context);
        //}

        #endregion

        #region Constructors and Destructors

        public TerritoryCountry(
            string section,
            string uid,
            string shortName,
            Region region,
            string tailNumberFormat,
            Country mainCountry)
            : base(section, uid, shortName, region, tailNumberFormat)
        {
            MainCountry = mainCountry;
        }

        private TerritoryCountry(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }

        #endregion
    }

    //the collection of countries
    public static class Countries
    {
        #region Static Fields

        private static Dictionary<string, Country> _countries = new Dictionary<string, Country>();

        #endregion

        #region Public Methods and Operators

        public static void AddCountry(Country country)
        {
            _countries.Add(country.Uid, country);
        }

        public static void Clear()
        {
            _countries = new Dictionary<string, Country>();
        }

        public static int Count()
        {
            return _countries.Values.Count - 1; //removes 100 (All)
        }

        //retuns a country

        //returns the list of countries
        public static List<Country> GetAllCountries()
        {
            var tCountries = _countries.Values.ToList();
            tCountries.AddRange(TemporaryCountries.GetCountries());
            return tCountries;
        }

        public static List<Country> GetCountries()
        {
            var netto = _countries.Values.ToList();
            //netto.AddRange(TemporaryCountries.GetCountries());
            netto.Remove(GetCountry("100"));

            var tCountries = (from country in netto
                              where !(country is TerritoryCountry)
                              select (Country)new CountryCurrentCountryConverter().Convert(country)).ToList();
            tCountries.AddRange(from country in TemporaryCountries.GetCountries()
                                where ((TemporaryCountry)country).Type == TemporaryCountry.TemporaryType.ManyToOne
                                select (Country)new CountryCurrentCountryConverter().Convert(country));

            return tCountries.Distinct().ToList();
        }

        //returns the list of countries from a region
        public static List<Country> GetCountries(Region region)
        {
            return GetCountries().FindAll((country => country.Region == region));
        }

        public static Country GetCountry(string uid)
        {
            if (_countries.ContainsKey(uid))
            {
                return _countries[uid];
            }
            if (TemporaryCountries.GetCountry(uid) != null)
            {
                return TemporaryCountries.GetCountry(uid);
            }
            return null;
        }

        //returns a country with a specific tailnumberformat
        public static Country GetCountryFromTailNumber(string tailnumber)
        {
            var country = GetCountries().Find(co => co.TailNumbers.IsMatch(tailnumber));

            return country;
        }

        #endregion
    }
}