using Newtonsoft.Json;

namespace Annytab.Doxservr.Fortnox.Functions
{
    /// <summary>
    /// This class represent doxservr api details
    /// </summary>
    public class DoxservrApiValues
    {
        #region Variables

        public string ApiHost { get; set; }
        public string ApiEmail { get; set; }
        public string ApiPassword { get; set; }
        
        #endregion

        #region Constructors

        /// <summary>
        /// Create a new post with default properties
        /// </summary>
        public DoxservrApiValues()
        {
            // Set values for instance variables
            this.ApiHost = null;
            this.ApiEmail = null;
            this.ApiPassword = null;
            
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