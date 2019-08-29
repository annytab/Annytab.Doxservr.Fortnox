using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Options;

namespace Annytab.Doxservr.Fortnox.Functions
{
    /// <summary>
    /// This class handles logging to blobs
    /// </summary>
    public class BlobLogger : IBlobLogger
    {
        #region Variables

        private readonly BlobStorageOptions options;
        private readonly CloudBlobClient client;
        private readonly CloudBlobContainer container;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new repository
        /// </summary>
        public BlobLogger(IOptions<BlobStorageOptions> options)
        {
            // Set values for instance variables
            this.options = options.Value;

            // Get a storage account
            CloudStorageAccount account = CloudStorageAccount.Parse(this.options.ConnectionString);

            // Get a client
            this.client = account.CreateCloudBlobClient();

            // Get a container reference
            this.container = this.client.GetContainerReference(this.options.ContainerName);

            // Create a container if it doesn't exist
            this.container.CreateIfNotExists();

        } // End of the constructor

        #endregion

        #region Log methods

        /// <summary>
        /// Log debug
        /// </summary>
        public async Task LogDebug(string blob_name, string message)
        {
            if (string.IsNullOrEmpty(message) == false)
            {
                await WriteToAppendBlob(blob_name, $"{DateTime.UtcNow.ToString("o")} [DBG] {message}" + Environment.NewLine);
            }

        } // End of the LogDebug method

        /// <summary>
        /// Log information
        /// </summary>
        public async Task LogInformation(string blob_name, string message)
        {
            if (string.IsNullOrEmpty(message) == false)
            {
                await WriteToAppendBlob(blob_name, $"{DateTime.UtcNow.ToString("o")} [INF] {message}" + Environment.NewLine);
            }

        } // End of the LogInformation method

        /// <summary>
        /// Log warning
        /// </summary>
        public async Task LogWarning(string blob_name, string message)
        {
            if (string.IsNullOrEmpty(message) == false)
            {
                await WriteToAppendBlob(blob_name, $"{DateTime.UtcNow.ToString("o")} [WRN] {message}" + Environment.NewLine);
            }

        } // End of the LogWarning method

        /// <summary>
        /// Log error
        /// </summary>
        public async Task LogError(string blob_name, string message)
        {
            if(string.IsNullOrEmpty(message) == false)
            {
                await WriteToAppendBlob(blob_name, $"{DateTime.UtcNow.ToString("o")} [ERR] {message}" + Environment.NewLine);
            }

        } // End of the LogError method

        #endregion

        #region Get methods

        /// <summary>
        /// Get an append blob as a stream
        /// </summary>
        public async Task GetLogAsStream(string blob_name, Stream stream)
        {
            // Get a blob object
            CloudAppendBlob blob = this.container.GetAppendBlobReference(blob_name);

            // Download the blob to a stream
            await blob.DownloadToStreamAsync(stream);

        } // End of the GetLogAsStream method

        /// <summary>
        /// Get an append blob as a stream
        /// </summary>
        public async Task<string> GetLogAsString(string blob_name)
        {
            // Create a string to return
            string log = "";

            // Get a blob object
            CloudAppendBlob blob = this.container.GetAppendBlobReference(blob_name);

            // Get the append blob
            using (MemoryStream stream = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                log = Encoding.UTF8.GetString(stream.ToArray());
            }

            // Return the log
            return log;

        } // End of the GetLogAsString method

        /// <summary>
        /// Get blob logs
        /// </summary>
        public IList<BlobLog> GetBlobLogs(string email)
        {
            // Create the variable to return
            IList<BlobLog> logs = new List<BlobLog>();

            // Get a list with blobs
            IEnumerable<IListBlobItem> blobs = this.container.ListBlobs(email + "/", true, BlobListingDetails.None);

            foreach (IListBlobItem item in blobs)
            {
                // Get a blob reference
                CloudBlob blob = (CloudBlob)item;

                // Add the blob log
                logs.Add(new BlobLog(blob.Properties.LastModified.Value.ToString("yyyy-MM-ddTHH:mm:ss"), blob.Name));
            }

            // Return logs
            return logs;

        } // End of the GetBlobLogs method

        #endregion

        #region Property methods

        /// <summary>
        /// Check if a blob exists
        /// </summary>
        public async Task<bool> LogExists(string blob_name)
        {
            // Get a blob reference
            CloudBlob blob = this.container.GetBlobReference(blob_name);

            // Return a boolean
            return await blob.ExistsAsync();

        } // End of the LogExists method


        #endregion

        #region Delete methods

        /// <summary>
        /// Delete an append blob
        /// </summary>
        public async Task Delete(string blob_name)
        {
            // Get a blob object
            CloudBlob blob = this.container.GetBlobReference(blob_name);

            // Delete blob
            await blob.DeleteIfExistsAsync();
            
        } // End of the Delete method

        /// <summary>
        /// Delete blobs older than the specified number of days
        /// </summary>
        public async Task DeleteByLastModifiedDate(Int32 days)
        {
            // Get a list with blobs
            BlobResultSegment blob_segment = await this.container.ListBlobsSegmentedAsync("", true, BlobListingDetails.All, 100, null, null, null);
 
            // Set the date treshold
            DateTime treshold = DateTime.UtcNow.AddDays(days * -1);

            // Create an endless loop
            while(true)
            {
                // Delete blobs
                foreach (IListBlobItem item in blob_segment.Results)
                {
                    // Get a blob reference
                    CloudBlob blob = (CloudBlob)item;

                    // Delete a blob if it is older than the threshold
                    if(blob.Properties.LastModified.Value.UtcDateTime < treshold)
                    {
                        await blob.DeleteIfExistsAsync();
                    }
                }

                // Check if more blobs can be found
                if(blob_segment.ContinuationToken != null)
                {
                    blob_segment = await this.container.ListBlobsSegmentedAsync("", true, BlobListingDetails.All, 100, blob_segment.ContinuationToken, null, null);
                }
                else
                {
                    break;
                }
            }

        } // End of the DeleteByLastModifiedDate method

        #endregion

        #region Helper methods

        /// <summary>
        /// Append text to an append blob
        /// </summary>
        private async Task WriteToAppendBlob(string blob_name, string log)
        {
            // Get a blob reference
            CloudAppendBlob blob = this.container.GetAppendBlobReference(blob_name);

            // Create a blob if it doesn't exist
            if (await blob.ExistsAsync() == false)
            {
                await blob.CreateOrReplaceAsync();
            }

            // Append the log to a blob
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(log)))
            {
                // Append text to the blob
                await blob.AppendBlockAsync(stream);
            }

        } // End of the WriteToAppendBlob method

        #endregion

    } // End of the class

} // End of the namespace