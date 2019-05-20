using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Annytab.Doxservr.Fortnox.Functions.Startup))]

namespace Annytab.Doxservr.Fortnox.Functions
{
    /// <summary>
    /// This class handles function startup
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// Configure the application
        /// </summary>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Add options
            builder.Services.Configure<BlobStorageOptions>(options => 
            {
                options.ConnectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                options.ContainerName = System.Environment.GetEnvironmentVariable("BlobContainerName");
            });

            // Add clients
            builder.Services.AddHttpClient("default").ConfigurePrimaryHttpMessageHandler(() => 
                new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });

            // Add repositories
            builder.Services.AddSingleton<IBlobLogger, BlobLogger>();

        } // End of the Configure method

    } // End of the class

} // End of the namespace