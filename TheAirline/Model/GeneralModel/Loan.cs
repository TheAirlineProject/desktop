﻿namespace TheAirline.Model.GeneralModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    //the class for a loan
    [Serializable]
    public class Loan : ISerializable
    {
        #region Constructors and Destructors

        public Loan(DateTime date, double amount, int length, double rate)
        {
            this.Amount = amount;
            this.Rate = rate;
            this.Length = length;
            this.Date = date;
            this.PaymentLeft = amount;
        }

        private Loan(SerializationInfo info, StreamingContext ctxt)
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

        #region Public Properties

        [Versioning("amount")]
        public double Amount { get; set; }

        [Versioning("date")]
        public DateTime Date { get; set; }

        public Boolean IsActive
        {
            get
            {
                return this.hasPaymentLeft();
            }
            set
            {
                ;
            }
        }

        [Versioning("length")]
        public int Length { get; set; }

        public double MonthlyPayment
        {
            get
            {
                return this.getMonthlyPayment();
            }
            set
            {
                ;
            }
        }

        public int MonthsLeft
        {
            get
            {
                return this.getMonthsLeft();
            }
            set
            {
                ;
            }
        }

        [Versioning("paymentleft")]
        public double PaymentLeft { get; set; }

        [Versioning("rate")]
        public double Rate { get; set; }

        #endregion

        #region Public Methods and Operators

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

        //returns the monthly payment for the loan

        public double getMonthlyPayment()
        {
            double basePayment = MathHelpers.GetMonthlyPayment(this.Amount, this.Rate, this.Length);

            return basePayment * GameObject.GetInstance().Difficulty.LoanLevel;
        }

        #endregion

        //checks if there is still payment left on the loan

        //returns the amount of months left on the loan

        #region Methods

        private int getMonthsLeft()
        {
            return (int)Math.Ceiling(this.PaymentLeft / this.MonthlyPayment);
        }

        private Boolean hasPaymentLeft()
        {
            return this.PaymentLeft > 0;
        }

        #endregion
    }
}