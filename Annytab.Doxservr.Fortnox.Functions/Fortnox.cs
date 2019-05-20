using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Annytab.Fortnox.Client.V3;

namespace Annytab.Doxservr.Fortnox.Functions
{
    public class Fortnox
    {
        #region Variables

        private readonly IBlobLogger logger;
        private readonly IHttpClientFactory client_factory;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new function
        /// </summary>
        public Fortnox(IBlobLogger logger, IHttpClientFactory client_factory)
        {
            // Set values for instance variables
            this.logger = logger;
            this.client_factory = client_factory;

        } // End of the constructor

        #endregion

        #region Methods

        /// <summary>
        /// Get a fortnox access token
        /// </summary>
        [FunctionName("GetFortnoxAccessToken")]
        public async Task<IActionResult> GetFortnoxAccessToken([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest request)
        {
            // Set the blob file name for logging
            string authorization_code = request.Form["AuthorizationCode"].ToString();

            // Create options
            IOptions<FortnoxOptions> options = Options.Create<FortnoxOptions>(new FortnoxOptions
            {
                ClientSecret = "1fBN6P7jRA",
                AuthorizationCode = authorization_code
            });

            // Create a fortnox authorization client
            FortnoxAuthorizationClient client = new FortnoxAuthorizationClient(this.client_factory.CreateClient("default"), options);

            // Get an access token from fortnox
            AuthorizationResponse response = await client.GetAccessToken();

            // Check if the request was a success or not
            if (response.success == true)
            {
                // Return an access token
                return new OkObjectResult(response.message);
            }
            else
            {
                // Return an error response
                return new BadRequestObjectResult(response.message);
            }

        } // End of the GetFortnoxAccessToken method

        /// <summary>
        /// Import documents to fortnox
        /// </summary>
        [FunctionName("FortnoxImport")]
        public async Task<IActionResult> FortnoxImport([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest request)
        {
            // Create the string to return
            string log = "";

            // Get a guid value
            string blob_name = request.Form["Guid"].ToString();
            
            // Get api values
            FortnoxApiValues nox_values = JsonConvert.DeserializeObject<FortnoxApiValues>(request.Form["FortnoxApiValues"].ToString());
            DoxservrApiValues dox_values = JsonConvert.DeserializeObject<DoxservrApiValues>(request.Form["DoxservrApiValues"].ToString());

            // Make sure that the request contains api values
            if(nox_values == null || dox_values == null)
            {
                return new BadRequestObjectResult("Your request must contain FortnoxApiValues and DoxservrApiValues in the body.");
            }

            // Set the blob name
            blob_name = string.IsNullOrEmpty(blob_name) == false ? dox_values.ApiEmail + "/" + blob_name + ".log" : dox_values.ApiEmail + "/" + Guid.NewGuid().ToString() + ".log";

            // Make sure that the blob does not exist
            if (await this.logger.LogExists(blob_name) == true)
            {
                return new BadRequestObjectResult("This log already exists, use another Guid.");
            }

            // Create a fortnox repository
            FortnoxRepository fortnox_repository = new FortnoxRepository(blob_name, this.logger, this.client_factory.CreateClient("default"), this.client_factory.CreateClient("default"),
            nox_values, dox_values);

            // Run import
            log = await fortnox_repository.RunImport();

            // Return the log
            return new OkObjectResult(log);

        } // End of the FortnoxImport method

        /// <summary>
        /// Export documents from fortnox
        /// </summary>
        [FunctionName("FortnoxExport")]
        public async Task<IActionResult> FortnoxExport([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest request)
        {
            // Create the string to return
            string log = "";

            // Get a guid value
            string blob_name = request.Form["Guid"].ToString();

            // Get api values
            FortnoxApiValues nox_values = JsonConvert.DeserializeObject<FortnoxApiValues>(request.Form["FortnoxApiValues"].ToString());
            DoxservrApiValues dox_values = JsonConvert.DeserializeObject<DoxservrApiValues>(request.Form["DoxservrApiValues"].ToString());

            // Make sure that the request contains api values
            if (nox_values == null || dox_values == null)
            {
                return new BadRequestObjectResult("Your request must contain FortnoxApiValues and DoxservrApiValues in the body.");
            }

            // Set the blob name
            blob_name = string.IsNullOrEmpty(blob_name) == false ? dox_values.ApiEmail + "/" + blob_name + ".log" : dox_values.ApiEmail + "/" + Guid.NewGuid().ToString() + ".log";

            // Make sure that the blob does not exist
            if (await this.logger.LogExists(blob_name) == true)
            {
                return new BadRequestObjectResult("This log already exists, use another Guid.");
            }

            // Create a fortnox repository
            FortnoxRepository fortnox_repository = new FortnoxRepository(blob_name, this.logger, this.client_factory.CreateClient("default"), this.client_factory.CreateClient("default"),
            nox_values, dox_values);

            // Run export
            log = await fortnox_repository.RunExport();

            // Return the log
            return new OkObjectResult(log);

        } // End of the FortnoxExport method

        /// <summary>
        /// Get a log file
        /// </summary>
        [FunctionName("GetLogs")]
        public IActionResult GetLogs([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest request)
        {
            // Set the blob file name for logging
            string email = request.Form["Email"].ToString();

            // Return the log
            return new OkObjectResult(this.logger.GetBlobLogs(email));

        } // End of the GetLogs method

        /// <summary>
        /// Get a log file
        /// </summary>
        [FunctionName("GetLog")]
        public async Task<IActionResult> GetLog([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest request)
        {
            // Set the blob file name for logging
            string blob_name = request.Form["LogName"].ToString();

            // Return the log
            return new OkObjectResult(await this.logger.GetLogAsString(blob_name));

        } // End of the GetLog method

        /// <summary>
        /// Delete a log file
        /// </summary>
        [FunctionName("DeleteLog")]
        public async Task<IActionResult> DeleteLog([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest request)
        {
            // Set the blob file name for logging
            string blob_name = request.Form["LogName"].ToString();

            // Delete a blob
            await this.logger.Delete(blob_name);

            // Return a success response
            return new OkResult();

        } // End of the GetLog method

        /// <summary>
        /// Clean up log files
        /// </summary>
        [FunctionName("CleanUpLogs")]
        public async Task<IActionResult> CleanUpLogs([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest request)
        {
            // Set the blob file name for logging
            Int32 days = Convert.ToInt32(request.Form["Days"].ToString());

            // Clean up logs
            await this.logger.DeleteByLastModifiedDate(days);

            // Return a success response
            return new OkResult();

        } // End of the CleanUp method

        #endregion

    } // End of the class

} // End of the namespace