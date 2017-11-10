using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Annytab.Fortnox.Client.V3;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// Check configuration values for errors
    /// </summary>
    public class ValidateConfiguration
    {
        #region Variables

        private string directory;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new post
        /// </summary>
        public ValidateConfiguration(string directory)
        {
            // Set values for instance variables
            this.directory = directory;

        } // End of the constructor

        #endregion

        #region Run methods

        /// <summary>
        /// Do the work
        /// </summary>
        public async Task<bool> Run()
        {
            // Create variables
            bool success = true;
            string path = this.directory + "\\appsettings.json";
            FileStream file_stream = null;
            StreamReader stream_reader = null;

            // Make sure that the file exists
            if(System.IO.File.Exists(path) == false)
            {
                LogError($"File not found: {path}, create a new appsettings.json file from appsettings.template.json!");
                return false;
            }

            try
            {
                // Create a file stream
                file_stream = System.IO.File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                // Create a stream reader
                stream_reader = new StreamReader(file_stream, Encoding.UTF8);

                // Get the data
                string data = stream_reader.ReadToEnd();

                // Convert data to app settings
                AppSettings app_settings = JsonConvert.DeserializeObject<AppSettings>(data);
                

                // Check for errors
                if (app_settings.DoxservrOptions.ApiHost == "" || app_settings.DoxservrOptions.ApiEmail == "" || app_settings.DoxservrOptions.ApiPassword == ""
                    || app_settings.FortnoxOptions.ClientId == "" || app_settings.FortnoxOptions.ClientSecret == ""
                    || app_settings.FortnoxOptions.AuthorizationCode == "")
                {
                    // Log the error
                    success = false;
                    LogError("You need to set values for ApiOptions in appsettings.json!");                   
                }
                else if (app_settings.FortnoxOptions.AccessToken == "")
                {
                    // Get an access token from fortnox
                    AuthorizationResponse response = await FortnoxRepository.GetAccessToken(app_settings.FortnoxOptions.AuthorizationCode, app_settings.FortnoxOptions.ClientSecret);

                    // Check if the request was a success or not
                    if(response.success == true)
                    {
                        // Set the access token in app settings
                        app_settings.FortnoxOptions.AccessToken = response.message;

                        // Get a byte array
                        byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(app_settings, Formatting.Indented));

                        // Write bytes to the file stream
                        file_stream.Seek(0, SeekOrigin.Begin);
                        file_stream.SetLength(0);
                        file_stream.Flush();
                        file_stream.Write(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        // Log the error
                        LogError(response.message);
                    }             
                }
            }
            catch(Exception ex)
            {
                // Log the exception
                success = false;
                LogError(ex.ToString());
            }
            finally
            {
                if(file_stream != null)
                {
                    file_stream.Dispose();
                }
                if(stream_reader != null)
                {
                    stream_reader.Dispose();
                }
            }

            // Return the success boolean
            return success;

        } // End of the Run method

        #endregion

        #region Helper methods

        /// <summary>
        /// Log an error
        /// </summary>
        private void LogError(string error)
        {
            // Set the file path
            string path = this.directory + "\\errors.configuration.txt";

            // Add the error message to the file
            System.IO.File.AppendAllText(path, $"{DateTime.Now.ToString("o")} [ERR] {error}" + Environment.NewLine);

        } // End of the LogError method

        #endregion

    } // End of the class

} // End of the namespace