using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Annytab.Dox.Standards.V1;
using Annytab.Doxservr.Client.V1;
using Annytab.Fortnox.Client.V3;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This class represent a worker repository
    /// </summary>
    public class WorkerRepository : IWorkerRepository
    {
        #region Variables

        private readonly ILogger logger;
        private readonly IFilesRepository files_repository;
        private readonly IFortnoxRepository fortnox_repository;
        private readonly IFortnoxImporter fortnox_importer;
        private readonly IFortnoxExporter fortnox_exporter;
        private readonly DefaultValues default_values;
        
        #endregion

        #region Constructors

        /// <summary>
        /// Create a new repository
        /// </summary>
        public WorkerRepository(ILogger<IWorkerRepository> logger, IFilesRepository files_repository, IFortnoxRepository fortnox_repository, IFortnoxImporter fortnox_importer, 
            IFortnoxExporter fortnox_exporter, IOptions<DefaultValues> default_values)
        {
            // Set values for instance variables
            this.logger = logger;
            this.files_repository = files_repository;
            this.fortnox_repository = fortnox_repository;
            this.fortnox_importer = fortnox_importer;
            this.fortnox_exporter = fortnox_exporter;
            this.default_values = default_values.Value;

        } // End of the constructor

        #endregion

        #region Run methods

        /// <summary>
        /// Run import and export
        /// </summary>
        public async Task Run(string directory)
        {
            // Log application start
            this.logger.LogInformation("---- APPLICATION STARTS! ----");

            // Get clients
            HttpClient dox_client = this.files_repository.GetClient();
            HttpClient nox_client = this.fortnox_repository.GetClient();

            // Make sure that the doxservr account balance is higher than the lower limit
            if (this.default_values.DoxservrGibPerInvoice > 0)
            {
                await this.files_repository.CreateInvoice(dox_client, this.default_values.DoxservrGibPerInvoice, this.default_values.DoxservrMinimumBytes);
            }

            // Run import and export
            await RunImport(directory, dox_client, nox_client);
            await RunExport(directory, dox_client, nox_client);

            // Dispose of clients
            dox_client.Dispose();
            nox_client.Dispose();

            // Log application end
            this.logger.LogInformation("---- APPLICATION ENDS! ----");

        } // End of the run method

        /// <summary>
        /// Import to Fortnox
        /// </summary>
        private async Task RunImport(string directory, HttpClient dox_client, HttpClient nox_client)
        {
            // Log the start
            this.logger.LogInformation("START: Importing documents to Fortnox!");

            // Get new documents from doxservr
            IList<FileMetadata> files = new List<FileMetadata>();
            FilesMetadata files_tuple = await this.files_repository.GetList(dox_client, "", 0, -1, 0, 10);

            // Loop as long as there is more posts to get
            while(true)
            {
                // Add posts to the list
                foreach(FileMetadata post in files_tuple.posts)
                {
                    // Make sure that the file follows a standard
                    if (string.Equals(post.standard_name, "Annytab Dox Trade v1", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        files.Add(post);
                    }
                }

                // Check if there is more files
                if (files_tuple.ct != null && files_tuple.ct != "")
                {
                    // Get more files
                    files_tuple = await this.files_repository.GetList(dox_client, files_tuple.ct, 0, -1, 0, 10);
                }
                else
                {
                    // Break out from the loop
                    break;
                }            
            }

            // Loop file metadata posts
            foreach (FileMetadata post in files)
            {
                // Save the file to disk
                using (FileStream file_stream = System.IO.File.OpenWrite(directory + "\\Files\\" + post.id + CommonTools.GetExtensions(post.filename)))
                {
                    await this.files_repository.GetFile(dox_client, post.id, file_stream);
                }

                // Save metadata to disk
                System.IO.File.WriteAllText(directory + "\\Files\\Meta\\" + post.id + ".json", JsonConvert.SerializeObject(post));
            }

            // Create a list with accounts
            IList<string> accounts = new List<string> { this.default_values.SalesAccountEUREVERSEDVAT, this.default_values.SalesAccountEUVAT,
                this.default_values.SalesAccountEXPORT, this.default_values.SalesAccountSE0, this.default_values.SalesAccountSE12,
                this.default_values.SalesAccountSE25, this.default_values.SalesAccountSE6, this.default_values.SalesAccountSEREVERSEDVAT,
                this.default_values.PurchaseAccount };

            // Add accounts
            foreach(string account in accounts)
            {
                if(string.IsNullOrEmpty(account) == false)
                {
                    await this.fortnox_importer.AddAccount(nox_client, account);
                }
            }

            // Add a price list
            if (string.IsNullOrEmpty(this.default_values.PriceList) == false)
            {
                this.default_values.PriceList = this.default_values.PriceList.ToUpper();
                await this.fortnox_importer.AddPriceList(nox_client, this.default_values.PriceList);
            }

            // Upsert currency rates
            FixerRates fixer_rates = await UpdateCurrencyRates(directory);
            if(fixer_rates != null)
            {
                await this.fortnox_importer.UpsertCurrencies(nox_client, fixer_rates);
            }

            // Get email senders
            EmailSendersRoot email_senders = null;
            if(this.default_values.OnlyAllowTrustedSenders == true)
            {
                email_senders = await this.fortnox_repository.Get<EmailSendersRoot>(nox_client, "emailsenders");
            }

            // Get downloaded metadata files
            string[] metadata_files = System.IO.Directory.GetFiles(directory + "\\Files\\Meta\\");
            Int32 length = metadata_files != null ? metadata_files.Length : 0;

            // Loop metadata files
            foreach(string meta_path in metadata_files)
            {
                // Get the meta data
                FileMetadata post = JsonConvert.DeserializeObject<FileMetadata>(System.IO.File.ReadAllText(meta_path, Encoding.UTF8));

                // Get the sender
                Party sender = null;
                foreach (Party party in post.parties)
                {
                    if (party.is_sender == 1)
                    {
                        sender = party;
                        break;
                    }
                }

                // Check if we only should allow trusted senders
                if(this.default_values.OnlyAllowTrustedSenders == true)
                {
                    // Check if the sender is a trusted sender
                    bool trusted = false;

                    foreach(EmailSender email_sender in email_senders.EmailSenders.TrustedSenders)
                    {
                        if(email_sender.Email == sender.email)
                        {
                            trusted = true;
                            break;
                        }
                    }

                    // Check if the sender is trusted
                    if(trusted == false)
                    {
                        // Log the error
                        this.logger.LogError($"{sender.email} is not trusted, add the email to the list of trusted email addresses in Fortnox (Inställningar/Arkivplats).");
                        continue;
                    }
                }

                // Get the file
                string file_path = directory + "\\Files\\" + post.id + CommonTools.GetExtensions(post.filename);

                // Make sure that the file exists
                if(System.IO.File.Exists(file_path) == false)
                {
                    // Log the error
                    this.logger.LogError($"File not found: {file_path}.");
                    continue;
                }

                // Get the document
                AnnytabDoxTrade doc = JsonConvert.DeserializeObject<AnnytabDoxTrade>(System.IO.File.ReadAllText(file_path, CommonTools.GetEncoding(post.file_encoding, Encoding.UTF8)));

                // Create an error variable
                bool error = false;

                // Check the document type
                if (doc.document_type == "request_for_quotation")
                {
                    // Log information
                    this.logger.LogInformation($"Starts to import offer {post.id}.json to Fortnox.");

                    // Import as offer
                    OfferRoot offer_root = await this.fortnox_importer.AddOffer(nox_client, sender.email, doc);

                    if (offer_root == null)
                    {
                        // Log the error
                        error = true;
                        this.logger.LogError($"Offer, {post.id}.json was not imported to Fortnox.");
                    }
                    else
                    {
                        // Log information
                        this.logger.LogInformation($"Offer, {post.id}.json was imported to Fortnox.");
                    }
                }
                else if (doc.document_type == "quotation")
                {
                    // Log information
                    this.logger.LogInformation($"Quotation {post.id}.json is not imported to Fortnox.");
                }
                else if (doc.document_type == "order")
                {
                    // Log information
                    this.logger.LogInformation($"Starts to import order {post.id}.json to Fortnox.");

                    // Import as order
                    OrderRoot order_root = await this.fortnox_importer.AddOrder(nox_client, sender.email, doc);

                    if (order_root == null)
                    {
                        // Log the error
                        error = true;
                        this.logger.LogError($"Order, {post.id}.json was not imported to Fortnox.");
                    }
                    else
                    {
                        // Log information
                        this.logger.LogInformation($"Order, {post.id}.json was imported to Fortnox.");
                    }
                }
                else if (doc.document_type == "order_confirmation")
                {
                    // Log information
                    this.logger.LogInformation($"Order confirmation {post.id}.json is not imported to Fortnox.");
                }
                else if (doc.document_type == "invoice")
                {
                    // Log information
                    this.logger.LogInformation($"Starts to import supplier invoice {post.id}.json to Fortnox.");

                    // Import as supplier invoice
                    SupplierInvoiceRoot invoice_root = await this.fortnox_importer.AddSupplierInvoice(nox_client, sender.email, doc);

                    if (invoice_root == null)
                    {
                        // Log the error
                        error = true;
                        this.logger.LogError($"Supplier invoice, {post.id}.json was not imported to Fortnox.");
                    }
                    else
                    {
                        // Log information
                        this.logger.LogInformation($"Supplier invoice, {post.id}.json was imported to Fortnox.");
                    }
                }
                else if (doc.document_type == "credit_invoice")
                {
                    // Log information
                    this.logger.LogInformation($"Starts to import supplier credit invoice {post.id}.json to Fortnox.");

                    // Import as supplier credit invoice
                    SupplierInvoiceRoot invoice_root = await this.fortnox_importer.AddSupplierInvoice(nox_client, sender.email, doc);

                    if (invoice_root == null)
                    {
                        // Log the error
                        error = true;
                        this.logger.LogError($"Supplier credit invoice, {post.id}.json was not imported to Fortnox.");
                    }
                    else
                    {
                        // Log information
                        this.logger.LogInformation($"Supplier credit invoice, {post.id}.json was imported to Fortnox.");
                    }
                }

                // Move files if no error was encountered
                if(error == false)
                {
                    // Create destination paths
                    string meta_destination = directory + $"\\Files\\Meta\\Imported\\{post.id}.json";
                    string file_destination = directory + $"\\Files\\Imported\\{post.id}.json";

                    try
                    {
                        // Delete destination files if the exists
                        if(System.IO.File.Exists(meta_destination) == true)
                        {
                            System.IO.File.Delete(meta_destination);
                        }
                        if(System.IO.File.Exists(file_destination) == true)
                        {
                            System.IO.File.Delete(file_destination);
                        }

                        // Move files
                        System.IO.Directory.Move(meta_path, meta_destination);
                        System.IO.Directory.Move(file_path, file_destination);
                    }
                    catch (Exception ex)
                    {
                        // Log the exception
                        this.logger.LogError(ex, "Moving files", null);
                    }
                }
            }

            // Log the end
            this.logger.LogInformation("END: Importing documents to Fortnox!");

        } // End of the RunImport method

        /// <summary>
        /// Export from fortnox
        /// </summary>
        private async Task RunExport(string directory, HttpClient dox_client, HttpClient nox_client)
        {
            // Log the start
            this.logger.LogInformation("START: Exporting documents from Fortnox!");

            // Get offers
            OffersRoot offers_root = await this.fortnox_exporter.GetOffers(nox_client);

            // Make sure that offers not is null
            if(offers_root != null && offers_root.Offers != null)
            {
                // Log information
                this.logger.LogInformation($"Starts to process {offers_root.Offers.Count} offers!");

                // Loop offers
                foreach (Offer post in offers_root.Offers)
                {
                    // Get the document
                    AnnytabDoxTradeRoot root = await this.fortnox_exporter.GetOffer(nox_client, post.DocumentNumber);

                    // Continue if the root is null
                    if (root == null)
                    {
                        continue;
                    }

                    // Variables
                    string data = JsonConvert.SerializeObject(root.document);
                    string filename = $"{root.document_type}_{root.document.id}.json";
                    string language_code = string.IsNullOrEmpty(root.language_code) == false ? root.language_code.ToLower() : "en";
                    FileMetadata file_metadata = null;

                    // Send the document
                    using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
                    {
                        file_metadata = await this.files_repository.Send(dox_client, stream, root.email, filename, "utf-8", "Annytab Dox Trade v1", language_code, "1");
                    }

                    // Make sure that the file has been sent
                    if (file_metadata != null)
                    {
                        // Save the file
                        System.IO.File.WriteAllText(directory + $"\\Files\\Exported\\{filename}", data, Encoding.UTF8);

                        // Mark the offer as sent
                        await this.fortnox_repository.Action<OfferRoot>(nox_client, $"offers/{root.document.id}/externalprint");

                        // Log information
                        this.logger.LogInformation($"Offer, {filename} has been sent to {root.email}!");
                    }
                }
            }
            
            // Get orders
            OrdersRoot orders_root = await this.fortnox_exporter.GetOrders(nox_client);

            // Make sure that orders not is null
            if(orders_root != null && orders_root.Orders != null)
            {
                // Log information
                this.logger.LogInformation($"Starts to process {orders_root.Orders.Count} orders!");

                // Loop orders
                foreach (Order post in orders_root.Orders)
                {
                    // Get documents (possibly 1 order and purchase orders)
                    IList<AnnytabDoxTradeRoot> roots = await this.fortnox_exporter.GetOrder(nox_client, post.DocumentNumber);

                    // Continue if roots is null
                    if (roots == null)
                    {
                        continue;
                    }

                    // Loop documents
                    bool marked_as_sent = false;
                    foreach (AnnytabDoxTradeRoot root in roots)
                    {
                        // Variables
                        string data = JsonConvert.SerializeObject(root.document);
                        string filename = $"{root.document_type}_{root.document.id}.json";
                        string language_code = string.IsNullOrEmpty(root.language_code) == false ? root.language_code.ToLower() : "en";
                        FileMetadata file_metadata = null;

                        // Send the document
                        using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
                        {
                            file_metadata = await this.files_repository.Send(dox_client, stream, root.email, filename, "utf-8", "Annytab Dox Trade v1", language_code, "1");
                        }

                        // Make sure that the file has been sent
                        if (file_metadata != null)
                        {
                            // Save the file
                            System.IO.File.WriteAllText(directory + $"\\Files\\Exported\\{filename}", data, Encoding.UTF8);

                            // Mark the order as sent
                            if (marked_as_sent == false)
                            {
                                await this.fortnox_repository.Action<OrderRoot>(nox_client, $"orders/{root.document.id}/externalprint");
                                marked_as_sent = true;
                            }

                            // Log information
                            this.logger.LogInformation($"Order, {filename} has been sent to {root.email}!");
                        }
                    }
                }
            }

            // Get invoices
            InvoicesRoot invoices_root = await this.fortnox_exporter.GetInvoices(nox_client);

            // Make sure that invoices not is null
            if(invoices_root != null && invoices_root.Invoices != null)
            {
                // Log information
                this.logger.LogInformation($"Starts to process {invoices_root.Invoices.Count} invoices!");

                // Loop invoices
                foreach (Invoice post in invoices_root.Invoices)
                {
                    // Get the document
                    AnnytabDoxTradeRoot root = await this.fortnox_exporter.GetInvoice(nox_client, post.DocumentNumber);

                    // Continue if the root is null
                    if (root == null)
                    {
                        continue;
                    }

                    // Variables
                    string data = JsonConvert.SerializeObject(root.document);
                    string filename = $"{root.document_type}_{root.document.id}.json";
                    string language_code = string.IsNullOrEmpty(root.language_code) == false ? root.language_code.ToLower() : "en";
                    FileMetadata file_metadata = null;

                    // Send the document
                    using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
                    {
                        file_metadata = await this.files_repository.Send(dox_client, stream, root.email, filename, "utf-8", "Annytab Dox Trade v1", language_code, "1");
                    }

                    // Make sure that the file has been sent
                    if (file_metadata != null)
                    {
                        // Save the file
                        System.IO.File.WriteAllText(directory + $"\\Files\\Exported\\{filename}", data, Encoding.UTF8);

                        // Mark the invoice as sent
                        await this.fortnox_repository.Action<InvoiceRoot>(nox_client, $"invoices/{root.document.id}/externalprint");

                        // Log information
                        this.logger.LogInformation($"Invoice, {filename} has been sent to {root.email}!");
                    }
                }
            }

            // Log the end of the work
            this.logger.LogInformation("END: Exporting documents from Fortnox!");

        } // End of the RunExport method

        #endregion

        #region Helper methods

        /// <summary>
        /// Update currency rates
        /// </summary>
        /// <returns>A reference to a fixer rates post, null if currency rates not was updated</returns>
        private async Task<FixerRates> UpdateCurrencyRates(string directory)
        {
            // Create the post to return
            FixerRates post = null;

            // Set the file path
            string file_path = directory + "\\currency_rates.json";

            // Get currency rates
            try
            {
                FixerRates file = JsonConvert.DeserializeObject<FixerRates>(System.IO.File.ReadAllText(file_path, Encoding.UTF8));

                // Check if currency rates are up to date
                if (DateTime.Now.Date <= file.date.Date.AddDays(4))
                {
                    return null;
                }
            }
            catch(System.IO.FileNotFoundException)
            {
                // File not found, it will be created
            }
            catch(Exception ex)
            {
                this.logger.LogError(ex, "UpdateCurrencyRates", null);
            }
            
            // Create a http client
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.None
            };
            HttpClient client = new HttpClient(handler);
            client.BaseAddress = new Uri("http://api.fixer.io");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("*"));

            // Get the base currency
            string base_currency = string.IsNullOrEmpty(this.default_values.BaseCurrency) == false ? this.default_values.BaseCurrency : "SEK";

            try
            {
                // Get the response
                HttpResponseMessage response = await client.GetAsync($"/latest?base={base_currency}");

                // Get the data
                if (response.IsSuccessStatusCode == true)
                {
                    // Get string data
                    string data = await response.Content.ReadAsStringAsync();

                    // Save currency rates to a file
                    System.IO.File.WriteAllText(file_path, data);

                    // Get fixer rates
                    post = JsonConvert.DeserializeObject<FixerRates>(data);
                }
                else
                {
                    // Get string data
                    string data = await response.Content.ReadAsStringAsync();

                    // Log the error
                    this.logger.LogError(data);
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                this.logger.LogError(ex, "UpdateCurrencyRates", null);

            }
            finally
            {
                // Dispose of the client
                client.Dispose();
            }

            // Return the post
            return post;

        } // End of the UpdateCurrencyRates method

        #endregion

    } // End of the class

} // End of the namespace