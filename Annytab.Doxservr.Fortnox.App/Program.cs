using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Annytab.Fortnox.Client.V3;
using Annytab.Doxservr.Client.V1;

namespace Annytab.Doxservr.Fortnox.App
{
    /// <summary>
    /// This is the main program class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The is the entry point for the program
        /// </summary>
        public async static Task Main(string[] args)
        {
            // Set the default directory path for storage
            string directory = "D:\\home\\AnnytabDoxservrFortnox";

            // Check if there is a directory argument
            if(args.Length > 0)
            {
                directory = args[0];
            }

            // Validate configuration
            ValidateConfiguration validate = new ValidateConfiguration(directory);
            if(await validate.Run() == false)
            {
                return;
            }

            // Create configuration
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(directory);
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            builder.AddJsonFile("appsettings.development.json", optional: true);
            builder.AddEnvironmentVariables(); // Important to add Azure variables

            // Add configuration
            IConfigurationRoot configuration = builder.Build();

            // Create a service collection
            IServiceCollection services = new ServiceCollection();

            // Add logging and options as services
            services.AddLogging(logging => {
                logging.AddConfiguration(configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
            });
            services.AddOptions();

            // Create api options
            services.Configure<DoxservrOptions>(configuration.GetSection("DoxservrOptions"));
            services.Configure<FortnoxOptions>(configuration.GetSection("FortnoxOptions"));
            services.Configure<DefaultValues>(configuration.GetSection("DefaultValues"));

            // Add clients
            services.AddHttpClient<IFortnoxClient, FortnoxClient>().ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
            services.AddHttpClient<IDoxservrFilesClient, DoxservrFilesClient>().ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
            services.AddHttpClient<IFixerClient, FixerClient>().ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });

            // Add repositories
            services.AddScoped<IFortnoxImporter, FortnoxImporter>();
            services.AddScoped<IFortnoxExporter, FortnoxExporter>();
            services.AddScoped<IWorkerRepository, WorkerRepository>();
            
            // Build a service provider
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Configure file logging
            ILoggerFactory loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddFile(directory + "\\Logs\\log-{Date}.txt");

            // Get the worker repository
            IWorkerRepository worker_repository = serviceProvider.GetService<IWorkerRepository>();

            // Run the program and wait for it to finish
            await worker_repository.Run(directory);

            // This delay is needed for the file logger to finish writing to the log file
            await Task.Delay(3000);
            
        } // End of the main method

    } // End of the class

} // End of the namespace