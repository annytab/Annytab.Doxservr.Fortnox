namespace Annytab.Doxservr.Fortnox.Functions
{
    /// <summary>
    /// This class represents a blob log
    /// </summary>
    public class BlobLog
    {
        #region Variables

        public string log_date { get; set; } // yyyy-MM-ddThh:mm:ss
        public string log_name { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new post
        /// </summary>
        public BlobLog()
        {
            // Set values for instance variables
            this.log_date = null;
            this.log_name = null;

        } // End of the constructor

        /// <summary>
        /// Create a new post
        /// </summary>
        public BlobLog(string log_date, string log_name)
        {
            // Set values for instance variables
            this.log_date = log_date;
            this.log_name = log_name;

        } // End of the constructor

        #endregion

    } // End of the class

} // End of the namespace