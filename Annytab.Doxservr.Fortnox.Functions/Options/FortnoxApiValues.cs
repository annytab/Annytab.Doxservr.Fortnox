using System;
using Newtonsoft.Json;

namespace Annytab.Doxservr.Fortnox.Functions
{
    /// <summary>
    /// This class represent fortnox api details
    /// </summary>
    public class FortnoxApiValues
    {
        #region Variables

        public string AccessToken { get; set; }
        public string PriceList { get; set; }
        public decimal PenaltyInterest { get; set; }
        public string SalesVatTypeSE { get; set; }
        public string SalesAccountSE25 { get; set; }
        public string SalesAccountSE12 { get; set; }
        public string SalesAccountSE6 { get; set; }
        public string SalesAccountSE0 { get; set; }
        public string SalesAccountSEREVERSEDVAT { get; set; }
        public string SalesAccountEUVAT { get; set; }
        public string SalesAccountEUREVERSEDVAT { get; set; }
        public string SalesAccountEXPORT { get; set; }
        public string PurchaseAccount { get; set; }
        public bool StockArticle { get; set; }
        public string StockAccount { get; set; }
        public string StockChangeAccount { get; set; }
        public bool OnlyAllowTrustedSenders { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new post with default properties
        /// </summary>
        public FortnoxApiValues()
        {
            // Set values for instance variables
            this.AccessToken = "";
            this.PriceList = "A";
            this.PenaltyInterest = 0.1m;
            this.SalesVatTypeSE = "SEVAT";
            this.SalesAccountSE25 = "3001";
            this.SalesAccountSE12 = "3002";
            this.SalesAccountSE6 = "3003";
            this.SalesAccountSE0 = "3004";
            this.SalesAccountSEREVERSEDVAT = "3231";
            this.SalesAccountEUVAT = "3106";
            this.SalesAccountEUREVERSEDVAT = "3108";
            this.SalesAccountEXPORT = "3105";
            this.PurchaseAccount = "4000";
            this.StockArticle = false;
            this.StockAccount = "1460";
            this.StockChangeAccount = "4990";
            this.OnlyAllowTrustedSenders = false;

        } // End of the constructor

        #endregion

        #region Get methods

        /// <summary>
        /// Convert the object to a json string
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);

        } // End of the ToString method

        #endregion

    } // End of the class

} // End of the namespace