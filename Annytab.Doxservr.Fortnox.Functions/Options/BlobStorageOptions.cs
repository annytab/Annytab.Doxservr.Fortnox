namespace Annytab.Doxservr.Fortnox.Functions
{
    /// <summary>
    /// This class represents azure blob storage options
    /// </summary>
    public class BlobStorageOptions
    {
        #region Variables

        public string ConnectionString { get; set; }
        public string ContainerName { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new post
        /// </summary>
        public BlobStorageOptions()
        {
            // Set values for instance variables
            this.ConnectionString = null;
            this.ContainerName = null;

        } // End of the constructor

        #endregion

    } // End of the class

} // End of the namespace