using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Annytab.Doxservr.Client.V1;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This class represent a fortnox authorization client
    /// </summary>
    public class FixerClient : IFixerClient
    {
        #region Variables

        private readonly HttpClient client;
        private readonly DefaultValues default_values;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new repository
        /// </summary>
        public FixerClient(HttpClient http_client, IOptions<DefaultValues> options)
        {
            // Set values for instance variables
            this.client = http_client;
            this.default_values = options.Value;

            // Set values for the client
            this.client.BaseAddress = new Uri("http://api.fixer.io");
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        } // End of the constructor

        #endregion

        #region Update methods

        /// <summary>
        /// Update currency rates
        /// </summary>
        public async Task<DoxservrResponse<FixerRates>> UpdateCurrencyRates(string directory)
        {
            // Create the response to return
            DoxservrResponse<FixerRates> dr = new DoxservrResponse<FixerRates>();

            // Set the file path
            string file_path = directory + "\\currency_rates.json";

            try
            {
                // Get currency rates
                FixerRates file = JsonConvert.DeserializeObject<FixerRates>(System.IO.File.ReadAllText(file_path, Encoding.UTF8));

                // Check if currency rates are up to date
                if (DateTime.Now.Date <= file.date.Date.AddDays(4))
                {
                    return dr;
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // File not found, it will be created
            }
            catch (Exception ex)
            {
                // Add error data
                dr.error = $"UpdateCurrencyRates: {ex.ToString()}";
            }

            // Get the base currency
            string base_currency = string.IsNullOrEmpty(this.default_values.BaseCurrency) == false ? this.default_values.BaseCurrency : "SEK";

            try
            {
                // Get the response
                HttpResponseMessage response = await this.client.GetAsync($"/latest?base={base_currency}");

                // Get the data
                if (response.IsSuccessStatusCode == true)
                {
                    // Get string data
                    string data = await response.Content.ReadAsStringAsync();

                    // Save currency rates to a file
                    System.IO.File.WriteAllText(file_path, data);

                    // Get fixer rates
                    dr.model = JsonConvert.DeserializeObject<FixerRates>(data);
                }
                else
                {
                    // Get string data
                    string data = await response.Content.ReadAsStringAsync();

                    // Add error data
                    dr.error = $"UpdateCurrencyRates: {data}";
                }
            }
            catch (Exception ex)
            {
                // Add exception data
                dr.error = $"UpdateCurrencyRates: {ex.ToString()}";
            }

            // Return the response
            return dr;

        } // End of the UpdateCurrencyRates

        #endregion

    } // End of the class

} // End of the namespace