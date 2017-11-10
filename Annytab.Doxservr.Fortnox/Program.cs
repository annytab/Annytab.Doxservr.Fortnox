using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Annytab.Doxservr.Client.V1;
using Annytab.Fortnox.Client.V3;

namespace Annytab.Doxservr.Fortnox
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
            // Validate configuration
            ValidateConfiguration validate = new ValidateConfiguration(Directory.GetCurrentDirectory());
            if(await validate.Run() == false)
            {
                return;
            }

            // Create configuration
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            builder.AddEnvironmentVariables(); // Important to add Azure variables

            // Add configuration
            IConfigurationRoot configuration = builder.Build();

            // Create a service collection
            IServiceCollection services = new ServiceCollection();

            // Add services for logging and for options
            services.AddLogging();
            services.AddOptions();

            // Create api options
            services.Configure<DoxservrOptions>(configuration.GetSection("DoxservrOptions"));
            services.Configure<FortnoxOptions>(configuration.GetSection("FortnoxOptions"));

            // Add repositories
            services.AddScoped<IFortnoxRepository, FortnoxRepository>();
            services.AddTransient<IWorkerRepository, WorkerRepository>();
            
            // Build a service provider
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Configure logging
            ILoggerFactory loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddFile("Logs/Errors-{Date}.txt");

            // Run the program, wait for it to finish
            await serviceProvider.GetService<IWorkerRepository>().Run();
            
        } // End of the main method

    } // End of the class

} // End of the namespace