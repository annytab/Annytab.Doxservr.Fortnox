using Annytab.Dox.Standards.V1;

namespace Annytab.Doxservr.Fortnox.App
{
    /// <summary>
    /// Create a new class
    /// </summary>
    public class AnnytabDoxTradeRoot
    {
        #region Variables

        public string document_type;
        public AnnytabDoxTrade document;
        public string email;
        public string language_code;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new post
        /// </summary>
        public AnnytabDoxTradeRoot()
        {
            // Set values for instance variables
            this.document_type = null;
            this.document = null;
            this.email = null;
            this.language_code = null;

        } // End of the constructor

        #endregion

    } // End of the class

} // End of the namespace