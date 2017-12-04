using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This class represent a collection of currency rates
    /// </summary>
    public class FixerRates
    {
        #region Variables

        [JsonProperty("base")]
        public string base_currency { get; set; }
        public DateTime date { get; set; }
        public IDictionary<string, decimal> rates { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new post with default properties
        /// </summary>
        public FixerRates()
        {
            // Set values for instance variables
            this.base_currency = "";
            this.date = new DateTime(2000, 1, 1);
            this.rates = new Dictionary<string, decimal>();

        } // End of the constructor

        #endregion

        #region Get methods

        /// <summary>
        /// Convert the object to a json string
        /// </summary>
        /// <returns>A json formatted string</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);

        } // End of the ToString method

        #endregion

    } // End of the class

} // End of the namespace