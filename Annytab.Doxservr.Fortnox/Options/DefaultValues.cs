namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This class represent default values
    /// </summary>
    public class DefaultValues
    {
        #region Variables

        public string BaseCurrency { get; set; }
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
        public bool OnlyAllowTrustedSenders { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new post
        /// </summary>
        public DefaultValues()
        {
            // Set values for instance variables
            this.BaseCurrency = "SEK";
            this.PriceList = "";
            this.PenaltyInterest = 0M;
            this.SalesVatTypeSE = "";
            this.SalesAccountSE25 = "";
            this.SalesAccountSE12 = "";
            this.SalesAccountSE6 = "";
            this.SalesAccountSE0 = "";
            this.SalesAccountSEREVERSEDVAT = "";
            this.SalesAccountEUVAT = "";
            this.SalesAccountEUREVERSEDVAT = "";
            this.SalesAccountEXPORT = "";
            this.PurchaseAccount = "";
            this.OnlyAllowTrustedSenders = false;

        } // End of the constructor

        #endregion

    } // End of the class

} // End of the namespace