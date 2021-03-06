﻿using System;
using System.Runtime.Serialization;
using TheAirline.Infrastructure;
using TheAirline.Models.Airlines;

namespace TheAirline.Models.Pilots
{
    //the facility for pilot training
    [Serializable]
    public class PilotTrainingFacility : AirlineFacility
    {
        #region Constructors and Destructors

        public PilotTrainingFacility(
            string section,
            string uid,
            double price,
            double monthlyCost,
            int fromYear,
            int serviceLevel,
            int luxuryLevel,
            string airlinerfamily)
            : base(section, uid, price, monthlyCost, fromYear, serviceLevel, luxuryLevel)
        {
            AirlinerFamily = airlinerfamily;
        }

        private PilotTrainingFacility(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }

        #endregion

        #region Public Properties

        [Versioning("family")]
        public string AirlinerFamily { get; set; }

        public override string Name => AirlinerFamily;

        public override string Shortname => AirlinerFamily;

        #endregion

        #region Public Methods and Operators

        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", 1);

            base.GetObjectData(info, context);
        }

        #endregion
    }
}