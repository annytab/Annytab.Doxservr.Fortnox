using System;
using System.IO;
using System.Text;
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
        private readonly IFortnoxClient nox_client;
        private readonly IDoxservrFilesClient dox_files_client;
        private readonly IFixerClient fixer_client;
        private readonly IFortnoxImporter fortnox_importer;
        private readonly IFortnoxExporter fortnox_exporter;
        private readonly DefaultValues default_values;
        
        #endregion

        #region Constructors

        /// <summary>
        /// Create a new repository
        /// </summary>
        public WorkerRepository(ILogger<IWorkerRepository> logger, IFortnoxClient nox_client, IDoxservrFilesClient dox_files_client, IFixerClient fixer_client, IFortnoxImporter fortnox_importer, 
            IFortnoxExporter fortnox_exporter, IOptions<DefaultValues> default_values)
        {
            // Set values for instance variables
            this.logger = logger;
            this.nox_client = nox_client;
            this.dox_files_client = dox_files_client;
            this.fixer_client = fixer_client;
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

            // Run import and export
            await RunImport(directory, dox_files_client, nox_client);
            await RunExport(directory, dox_files_client, nox_client);

            // Log application end
            this.logger.LogInformation("---- APPLICATION ENDS! ----");

        } // End of the run method

        /// <summary>
        /// Import to Fortnox
        /// </summary>
        private async Task RunImport(string directory, IDoxservrFilesClient dox_files_client, IFortnoxClient nox_client)
        {
            // Log the start
            this.logger.LogInformation("START: Importing documents to Fortnox!");

            // Get files from doxservr
            await GetDoxservrFiles(directory);

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
                    await this.fortnox_importer.AddAccount(account);
                }
            }

            // Add a price list
            if (string.IsNullOrEmpty(this.default_values.PriceList) == false)
            {
                this.default_values.PriceList = this.default_values.PriceList.ToUpper();
                await this.fortnox_importer.AddPriceList(this.default_values.PriceList);
            }

            // Upsert currency rates
            //DoxservrResponse<FixerRates> dr_fixer_rates = await this.fixer_client.UpdateCurrencyRates(directory);
            //if(dr_fixer_rates.model != null)
            //{
            //    await this.fortnox_importer.UpsertCurrencies(dr_fixer_rates.model);
            //}

            // Get email senders
            EmailSendersRoot email_senders = null;
            if(this.default_values.OnlyAllowTrustedSenders == true)
            {
                email_senders = await this.fortnox_importer.GetTrustedEmailSenders();
            }

            // Get downloaded metadata files
            string[] metadata_files = System.IO.Directory.GetFiles(directory + "\\Files\\Meta\\");

            // Loop metadata files
            foreach(string meta_path in metadata_files)
            {
                // Metadata
                FileDocument post = null;

                try
                {
                    // Get the meta data
                    string meta_data = System.IO.File.ReadAllText(meta_path, Encoding.UTF8);

                    // Make sure that there is meta data
                    if (string.IsNullOrEmpty(meta_data) == true)
                    {
                        this.logger.LogError($"File is empty: {meta_path}");
                        continue;
                    }

                    // Get the post
                    post = JsonConvert.DeserializeObject<FileDocument>(meta_data);
                }
                catch (Exception ex)
                {
                    // Log the error
                    this.logger.LogError(ex, $"Deserialize file: {meta_path}", null);
                    continue;
                }

                // Make sure that the post not is null
                if(post == null)
                {
                    // Log the error
                    this.logger.LogError($"Post is null: {meta_path}", null);
                    continue;
                }
                
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

                // Get the file path
                string file_path = directory + "\\Files\\" + post.id + CommonTools.GetExtensions(post.filename);

                // Make sure that the file exists
                if(System.IO.File.Exists(file_path) == false)
                {
                    // Log the error
                    this.logger.LogError($"File not found: {file_path}.");
                    continue;
                }

                // Document
                AnnytabDoxTrade doc = null;

                try
                {
                    // Get file data
                    string file_data = System.IO.File.ReadAllText(file_path, CommonTools.GetEncoding(post.file_encoding, Encoding.UTF8));

                    // Make sure that there is file data
                    if(string.IsNullOrEmpty(file_data) == true)
                    {
                        // Log the error
                        this.logger.LogError($"File is empty: {file_path}.");
                        continue;
                    }

                    // Get the document
                    doc = JsonConvert.DeserializeObject<AnnytabDoxTrade>(file_data);
                }
                catch(Exception ex)
                {
                    // Log the error
                    this.logger.LogError(ex, $"Deserialize file: {file_path}", null);
                    continue;
                }

                // Make sure that the document not is null
                if (doc == null)
                {
                    // Log the error
                    this.logger.LogError($"Post is null: {file_path}", null);
                    continue;
                }

                // Create an error variable
                bool error = false;

                // Check the document type
                if (doc.document_type == "request_for_quotation")
                {
                    // Log information
                    this.logger.LogInformation($"Starts to import offer {post.id}.json to Fortnox.");

                    // Import as offer
                    OfferRoot offer_root = await this.fortnox_importer.AddOffer(sender.email, doc);

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
                    OrderRoot order_root = await this.fortnox_importer.AddOrder(sender.email, doc);

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
                    SupplierInvoiceRoot invoice_root = await this.fortnox_importer.AddSupplierInvoice(sender.email, doc);

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
                    SupplierInvoiceRoot invoice_root = await this.fortnox_importer.AddSupplierInvoice(sender.email, doc);

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
        private async Task RunExport(string directory, IDoxservrFilesClient dox_files_client, IFortnoxClient nox_client)
        {
            // Log the start
            this.logger.LogInformation("START: Exporting documents from Fortnox!");

            // Get offers
            OffersRoot offers_root = await this.fortnox_exporter.GetOffers();

            // Make sure that offers not is null
            if(offers_root != null && offers_root.Offers != null)
            {
                // Log information
                this.logger.LogInformation($"Starts to process {offers_root.Offers.Count} offers!");

                // Loop offers
                foreach (Offer post in offers_root.Offers)
                {
                    // Get the document
                    AnnytabDoxTradeRoot root = await this.fortnox_exporter.GetOffer(post.DocumentNumber);

                    // Continue if the root is null
                    if (root == null)
                    {
                        continue;
                    }

                    // Make sure that there is an email address
                    if (string.IsNullOrEmpty(root.email) == true)
                    {
                        this.logger.LogError($"Offer: {root.document.id}, no email specified!");
                        continue;
                    }

                    // Variables
                    string data = JsonConvert.SerializeObject(root.document);
                    string filename = $"{root.document_type}_{root.document.id}.json";
                    string language_code = string.IsNullOrEmpty(root.language_code) == false ? root.language_code.ToLower() : "en";
                    DoxservrResponse<FileDocument> dr_file_metadata = null;

                    try
                    {
                        // Send the document
                        using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
                        {
                            dr_file_metadata = await this.dox_files_client.Send(stream, root.email, filename, "utf-8", "Annytab Dox Trade v1", language_code, "1");
                        }

                        // Make sure that the file has been sent
                        if (dr_file_metadata.model != null)
                        {
                            // Save the file
                            System.IO.File.WriteAllText(directory + $"\\Files\\Exported\\{filename}", data, Encoding.UTF8);

                            // Mark the offer as sent
                            await this.nox_client.Action<OfferRoot>($"offers/{root.document.id}/externalprint");

                            // Log information
                            this.logger.LogInformation($"Offer, {filename} has been sent to {root.email}!");
                        }
                    }
                    catch(Exception ex)
                    {
                        // Log the exception
                        this.logger.LogError(ex, $"Offer: {root.document.id}", null);
                    }
                }
            }
            
            // Get orders
            OrdersRoot orders_root = await this.fortnox_exporter.GetOrders();

            // Make sure that orders not is null
            if(orders_root != null && orders_root.Orders != null)
            {
                // Log information
                this.logger.LogInformation($"Starts to process {orders_root.Orders.Count} orders!");

                // Loop orders
                foreach (Order post in orders_root.Orders)
                {
                    // Get documents (possibly 1 order and purchase orders)
                    IList<AnnytabDoxTradeRoot> roots = await this.fortnox_exporter.GetOrder(post.DocumentNumber);

                    // Continue if roots is null
                    if (roots == null)
                    {
                        continue;
                    }

                    // Loop documents
                    bool marked_as_sent = false;
                    foreach (AnnytabDoxTradeRoot root in roots)
                    {
                        // Make sure that there is an email address
                        if (string.IsNullOrEmpty(root.email) == true)
                        {
                            this.logger.LogError($"Order: {root.document.id}, no email specified!");
                            continue;
                        }

                        // Variables
                        string data = JsonConvert.SerializeObject(root.document);
                        string filename = $"{root.document_type}_{root.document.id}.json";
                        string language_code = string.IsNullOrEmpty(root.language_code) == false ? root.language_code.ToLower() : "en";
                        DoxservrResponse<FileDocument> dr_file_metadata = null;

                        try
                        {
                            // Send the document
                            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
                            {
                                dr_file_metadata = await this.dox_files_client.Send(stream, root.email, filename, "utf-8", "Annytab Dox Trade v1", language_code, "1");
                            }

                            // Make sure that the file has been sent
                            if (dr_file_metadata.model != null)
                            {
                                // Save the file
                                System.IO.File.WriteAllText(directory + $"\\Files\\Exported\\{filename}", data, Encoding.UTF8);

                                // Mark the order as sent
                                if (marked_as_sent == false)
                                {
                                    await this.nox_client.Action<OrderRoot>($"orders/{root.document.id}/externalprint");
                                    marked_as_sent = true;
                                }

                                // Log information
                                this.logger.LogInformation($"Order, {filename} has been sent to {root.email}!");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log the exception
                            this.logger.LogError(ex, $"Order: {root.document.id}", null);
                        }
                    }
                }
            }

            // Get invoices
            InvoicesRoot invoices_root = await this.fortnox_exporter.GetInvoices();

            // Make sure that invoices not is null
            if(invoices_root != null && invoices_root.Invoices != null)
            {
                // Log information
                this.logger.LogInformation($"Starts to process {invoices_root.Invoices.Count} invoices!");

                // Loop invoices
                foreach (Invoice post in invoices_root.Invoices)
                {
                    // Get the document
                    AnnytabDoxTradeRoot root = await this.fortnox_exporter.GetInvoice(post.DocumentNumber);

                    // Continue if the root is null
                    if (root == null)
                    {
                        continue;
                    }

                    // Make sure that there is an email address
                    if (string.IsNullOrEmpty(root.email) == true)
                    {
                        this.logger.LogError($"Invoice: {root.document.id}, no email specified!");
                        continue;
                    }

                    // Variables
                    string data = JsonConvert.SerializeObject(root.document);
                    string filename = $"{root.document_type}_{root.document.id}.json";
                    string language_code = string.IsNullOrEmpty(root.language_code) == false ? root.language_code.ToLower() : "en";
                    DoxservrResponse<FileDocument> dr = null;

                    try
                    {
                        // Send the document
                        using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
                        {
                            dr = await this.dox_files_client.Send(stream, root.email, filename, "utf-8", "Annytab Dox Trade v1", language_code, "1");
                        }

                        // Make sure that the file has been sent
                        if (dr.model != null)
                        {
                            // Save the file
                            System.IO.File.WriteAllText(directory + $"\\Files\\Exported\\{filename}", data, Encoding.UTF8);

                            // Mark the invoice as sent
                            await this.nox_client.Action<InvoiceRoot>($"invoices/{root.document.id}/externalprint");

                            // Log information
                            this.logger.LogInformation($"Invoice, {filename} has been sent to {root.email}!");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception
                        this.logger.LogError(ex, $"Invoice: {root.document.id}", null);
                    }
                }
            }

            // Log the end of the work
            this.logger.LogInformation("END: Exporting documents from Fortnox!");

        } // End of the RunExport method

        #endregion

        #region Helper methods

        /// <summary>
        /// Get doxservr files
        /// </summary>
        /// <returns></returns>
        private async Task GetDoxservrFiles(string directory)
        {
            // Get new documents from doxservr
            DoxservrResponse<FileDocuments> dr = await this.dox_files_client.GetPage("", 0, -1, 0, 10);

            // Make sure that the model not is null
            if (dr.model == null)
            {
                // Log the error
                this.logger.LogError(dr.error);

                // Return from the method
                return;
            }

            // Loop as long as there is more posts to get
            while (true)
            {
                // Loop documents
                foreach (FileDocument fd in dr.model.items)
                {
                    // Make sure that the file follows a standard
                    if (string.Equals(fd.standard_name, "Annytab Dox Trade v1", StringComparison.OrdinalIgnoreCase) == false)
                    {
                        continue;
                    }

                    // File stream
                    FileStream file_stream = null;

                    try
                    {
                        // Create a file stream
                        file_stream = System.IO.File.OpenWrite(directory + "\\Files\\" + fd.id + CommonTools.GetExtensions(fd.filename));

                        // Get the file
                        DoxservrResponse<bool> file_response = await this.dox_files_client.GetFile(fd.id, file_stream);

                        // Get the file
                        if (file_response.model == false)
                        {
                            // Continue with the loop, the file was not downloaded
                            continue;
                        }

                        // Save metadata to disk
                        System.IO.File.WriteAllText(directory + "\\Files\\Meta\\" + fd.id + ".json", JsonConvert.SerializeObject(fd));
                    }
                    catch (Exception ex)
                    {
                        // Log the error
                        this.logger.LogError(ex, $"Save files to disk: {fd.id}", null);
                        continue;
                    }
                    finally
                    {
                        // Dispose of the stream
                        if (file_stream != null)
                        {
                            file_stream.Dispose();
                        }
                    }
                }

                // Check if there is more files
                if (string.IsNullOrEmpty(dr.model.ct) == false)
                {
                    // Get the next page
                    dr = await this.dox_files_client.GetPage(dr.model.ct, 0, -1, 0, 10);

                    // Make sure that the model not is null
                    if(dr.model == null)
                    {
                        // Log the error
                        this.logger.LogError(dr.error);

                        // Break out from the loop
                        break;
                    }
                }
                else
                {
                    // Break out from the loop
                    break;
                }
            }

        } // End of the GetDoxservrFiles method

        #endregion

    } // End of the class

} // End of the namespace