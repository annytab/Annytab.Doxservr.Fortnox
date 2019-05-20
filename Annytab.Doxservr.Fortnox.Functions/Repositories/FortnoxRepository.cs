using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Annytab.Fortnox.Client.V3;
using Annytab.Doxservr.Client.V1;
using Annytab.Dox.Standards.V1;
using UnidecodeSharpFork;
using Newtonsoft.Json;

namespace Annytab.Doxservr.Fortnox.Functions
{
    /// <summary>
    /// This class handles imports to and exports from fortnox
    /// </summary>
    public class FortnoxRepository
    {
        #region Variables

        private readonly string blob_name;
        private readonly IBlobLogger logger;
        private readonly FortnoxApiValues nox_api_values;
        private readonly DoxservrApiValues dox_api_values;
        private readonly IFortnoxClient nox_client;
        private readonly IDoxservrFilesClient dox_client;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new repository
        /// </summary>
        public FortnoxRepository(string blob_name, IBlobLogger logger, HttpClient nox_client, HttpClient dox_client,
            FortnoxApiValues nox_api_values, DoxservrApiValues dox_api_values)
        {
            // Set values for instance variables
            this.blob_name = blob_name;
            this.logger = logger;
            this.nox_api_values = nox_api_values;
            this.dox_api_values = dox_api_values;

            // Create fortnox options
            IOptions<FortnoxOptions> nox_options = Options.Create<FortnoxOptions>(new FortnoxOptions
            {
                ClientSecret = "1fBN6P7jRA",
                AccessToken = this.nox_api_values.AccessToken
            });

            // Create doxservr options
            IOptions<DoxservrOptions> dox_options = Options.Create<DoxservrOptions>(new DoxservrOptions
            {
                ApiHost = this.dox_api_values.ApiHost,
                ApiEmail = this.dox_api_values.ApiEmail,
                ApiPassword = this.dox_api_values.ApiPassword,
                TimeoutInSeconds = 30
            });

            // Create a fortnox client
            this.nox_client = new FortnoxClient(nox_client, nox_options);

            // Create a doxservr client
            this.dox_client = new DoxservrFilesClient(dox_client, dox_options);

        } // End of the constructor

        #endregion

        #region Run methods

        /// <summary>
        /// Run an import to fortnox
        /// </summary>
        public async Task<string> RunImport()
        {
            // Start logging
            await this.logger.LogInformation(this.blob_name, "START: Importerar dokument till Fortnox!");

            // Create a list with accounts
            IList<string> accounts = new List<string> { this.nox_api_values.SalesAccountSEREVERSEDVAT, this.nox_api_values.SalesAccountEUVAT,
                this.nox_api_values.SalesAccountEXPORT, this.nox_api_values.SalesAccountSE0, this.nox_api_values.SalesAccountSE12,
                this.nox_api_values.SalesAccountSE25, this.nox_api_values.SalesAccountSE6, this.nox_api_values.SalesAccountEUREVERSEDVAT,
                this.nox_api_values.PurchaseAccount, this.nox_api_values.StockAccount, this.nox_api_values.StockChangeAccount };

            // Add accounts
            foreach (string account in accounts)
            {
                if (string.IsNullOrEmpty(account) == false)
                {
                    await AddAccount(account);
                }
            }

            // Add a price list
            if (string.IsNullOrEmpty(this.nox_api_values.PriceList) == false)
            {
                this.nox_api_values.PriceList = this.nox_api_values.PriceList.ToUpper();
                await AddPriceList(this.nox_api_values.PriceList);
            }

            // Get email senders
            EmailSendersRoot email_senders = null;
            if (this.nox_api_values.OnlyAllowTrustedSenders == true)
            {
                email_senders = await GetTrustedEmailSenders();
            }

            // Get doxservr documents
            DoxservrResponse<FileDocuments> dr = await this.dox_client.GetPage("", 0, -1, 0, 10);

            // Check for an error
            if (dr.model == null)
            {
                // Log error
                await this.logger.LogError(this.blob_name, $"Kan inte hämta filer, {dr.error}");
                return await this.logger.GetLogAsString(this.blob_name);
            }

            // Create an endless loop
            while (true)
            {
                // Log information
                await this.logger.LogInformation(this.blob_name, $"Påbörjar import av {dr.model.items.Count} ej nedladdade filer till Fortnox.");

                // Loop documents
                foreach (FileDocument fd in dr.model.items)
                {
                    // Make sure that the file follows a standard
                    if (string.Equals(fd.standard_name, "Annytab Dox Trade v1", StringComparison.OrdinalIgnoreCase) == false)
                    {
                        await this.logger.LogInformation(this.blob_name, $"Dokument {fd.id} importerades inte, ej Annytab Dox Trade v1.");
                        continue;
                    }

                    // Create variables
                    MemoryStream stream = null;
                    AnnytabDoxTrade doc = null;

                    try
                    {
                        // Create a memory stream
                        stream = new MemoryStream();

                        // Get the file
                        DoxservrResponse<bool> file_response = await this.dox_client.GetFile(fd.id, stream);

                        // Get the file
                        if (file_response.model == false)
                        {
                            // Continue with the loop, the file was not downloaded
                            await this.logger.LogError(this.blob_name, $"Dokument {fd.id}, filen kunde inte laddas ned.");
                            continue;
                        }

                        // Move the pointer to the start of the stream
                        stream.Seek(0, SeekOrigin.Begin);

                        // Get the document
                        doc = JsonConvert.DeserializeObject<AnnytabDoxTrade>(Encoding.UTF8.GetString(stream.ToArray()));

                    }
                    catch (Exception ex)
                    {
                        // Log the exception
                        await this.logger.LogError(this.blob_name, $"Hämta fil {fd.id}, {ex.ToString()}");
                    }
                    finally
                    {
                        // Dispose of the stream
                        if (stream != null)
                        {
                            stream.Dispose();
                        }
                    }

                    // Import the document
                    await ImportDocument(fd, doc, email_senders);

                } // End of the foreach (FileDocument fd in dr.model.items) loop

                // Check if there is more files
                if (string.IsNullOrEmpty(dr.model.ct) == false)
                {
                    // Get the next page
                    dr = await dox_client.GetPage(dr.model.ct, 0, -1, 0, 10);

                    // Check for an error
                    if (dr.model == null)
                    {
                        // Log the error
                        await this.logger.LogError(this.blob_name, $"Kan inte hämta filer, {dr.error}");

                        // Break out from the loop
                        break;
                    }
                }
                else
                {
                    // Break out from the loop
                    break;
                }

            } // End of the while(true) loop

            // End logging
            await this.logger.LogInformation(this.blob_name, "SLUT: Importerar dokument till Fortnox!");

            // Return a log string
            return await this.logger.GetLogAsString(this.blob_name);

        } // End of the RunImport method

        /// <summary>
        /// Run an export from fortnox
        /// </summary>
        public async Task<string> RunExport()
        {
            // Get labels
            IDictionary<string, string> labels = await GetLabels();

            // Get company settings
            CompanySettingsRoot company = await GetCompanySettings();

            // Make sure that company and company settings not is null
            if (company == null || company.CompanySettings == null)
            {
                await this.logger.LogError(this.blob_name, $"Hittade inte några företagsinställningar.");
                return await this.logger.GetLogAsString(this.blob_name);
            }

            // Get offers
            OffersRoot offers = await GetOffers();

            // Make sure that offers not is null
            if (offers != null && offers.Offers != null)
            {
                // Log information
                await this.logger.LogInformation(this.blob_name, $"Påbörjar export av {offers.Offers.Count} offerter!");

                // Loop offers
                foreach (Annytab.Fortnox.Client.V3.Offer post in offers.Offers)
                {
                    // Get the document
                    AnnytabDoxTradeRoot offer = await GetOffer(post.DocumentNumber, labels, company);

                    // Check for errors
                    if (offer == null)
                    {
                        await this.logger.LogInformation(this.blob_name, $"Offert {post.DocumentNumber} exporterades inte (saknar etikett).");
                        continue;
                    }

                    try
                    {
                        // Send the document
                        bool success = await SendFile(offer);

                        // Make sure that the file has been sent
                        if (success == true)
                        {
                            // Mark the offer as sent
                            FortnoxResponse<OfferRoot> fr = await this.nox_client.Action<OfferRoot>($"offers/{offer.document.id}/externalprint");
                            await this.logger.LogError(this.blob_name, fr.error);

                            // Log information
                            await this.logger.LogInformation(this.blob_name, $"Offert, {offer.document_type}_{offer.document.id}.json har skickats till {offer.email}!");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception
                        await this.logger.LogError(this.blob_name, $"Offert: {offer.document.id}, {ex.ToString()}");
                    }
                }
            }

            // Get orders
            OrdersRoot orders = await GetOrders();

            // Make sure that orders not is null
            if (orders != null && orders.Orders != null)
            {
                // Log information
                await this.logger.LogInformation(this.blob_name, $"Påbörjar export av {orders.Orders.Count} ordrar!");

                // Loop orders
                foreach (Order post in orders.Orders)
                {
                    // Get documents (possibly 1 order and purchase orders)
                    IList<AnnytabDoxTradeRoot> order = await GetOrder(post.DocumentNumber, labels, company);

                    // Continue if order is null
                    if (order == null)
                    {
                        await this.logger.LogInformation(this.blob_name, $"Order {post.DocumentNumber} exporterades inte (saknar etikett).");
                        continue;
                    }

                    // Loop documents
                    bool marked_as_sent = false;
                    foreach (AnnytabDoxTradeRoot root in order)
                    {
                        try
                        {
                            // Send the document
                            bool success = await SendFile(root);

                            // Make sure that the file has been sent
                            if (success == true)
                            {
                                // Mark the order as sent
                                if (marked_as_sent == false)
                                {
                                    FortnoxResponse<OrderRoot> fr = await this.nox_client.Action<OrderRoot>($"orders/{root.document.id}/externalprint");
                                    await this.logger.LogError(this.blob_name, fr.error);
                                    marked_as_sent = true;
                                }

                                // Log information
                                await this.logger.LogInformation(this.blob_name, $"Order, {root.document_type}_{root.document.id}.json har skickats till {root.email}!");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log the exception
                            await this.logger.LogError(this.blob_name, $"Order: {root.document.id}, {ex.ToString()}");
                        }
                    }
                }
            }

            // Get invoices
            InvoicesRoot invoices = await GetInvoices();

            // Make sure that invoices not is null
            if (invoices != null && invoices.Invoices != null)
            {
                // Log information
                await this.logger.LogInformation(this.blob_name, $"Påbörjar export av {invoices.Invoices.Count} fakturor!");

                // Loop invoices
                foreach (Invoice post in invoices.Invoices)
                {
                    // Get the document
                    AnnytabDoxTradeRoot invoice = await GetInvoice(post.DocumentNumber, labels, company);

                    // Continue if the model is null
                    if (invoice == null)
                    {
                        await this.logger.LogInformation(this.blob_name, $"Faktura {post.DocumentNumber} exporterades inte (saknar etikett).");
                        continue;
                    }

                    try
                    {
                        // Send the document
                        bool success = await SendFile(invoice);

                        // Make sure that the file has been sent
                        if (success == true)
                        {
                            // Mark the invoice as sent
                            FortnoxResponse<InvoiceRoot> fr = await this.nox_client.Action<InvoiceRoot>($"invoices/{invoice.document.id}/externalprint");
                            await this.logger.LogError(this.blob_name, fr.error);

                            // Log information
                            await this.logger.LogInformation(this.blob_name, $"Faktura, {invoice.document_type}_{invoice.document.id}.json har skickats till {invoice.email}!");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception
                        await this.logger.LogError(this.blob_name, $"Faktura: {invoice.document.id}, {ex.ToString()}");
                    }
                }
            }

            // Return a log string
            return await this.logger.GetLogAsString(this.blob_name);

        } // End of the RunExport method

        #endregion

        #region Import methods

        /// <summary>
        /// Import a document to Fortnox
        /// </summary>
        public async Task ImportDocument(FileDocument fd, AnnytabDoxTrade doc, EmailSendersRoot email_senders)
        {
            // Get the sender
            Party sender = GetSender(fd.parties);

            // Check if we only should allow trusted senders
            if (this.nox_api_values.OnlyAllowTrustedSenders == true)
            {
                // Check if the sender is a trusted sender
                bool trusted = false;

                // Loop trusted email senders
                foreach (EmailSender email_sender in email_senders.EmailSenders.TrustedSenders)
                {
                    if (email_sender.Email == sender.email)
                    {
                        trusted = true;
                        break;
                    }
                }

                // Check if the sender is trusted
                if (trusted == false)
                {
                    // Log information
                    await this.logger.LogInformation(this.blob_name, $"{sender.email} är inte betrodd, lägg till e-postadressen i listan över betrodda e-postadresser i Fortnox (Inställningar/Arkivplats).");
                    return;
                }
            }

            // Check the document type
            if (doc.document_type == "request_for_quotation")
            {
                // Log information
                await this.logger.LogInformation(this.blob_name, $"Påbörjar import av offert {fd.id} till Fortnox.");

                // Import as offer
                OfferRoot offer = await AddOffer(sender.email, doc);
                if (offer != null) { await this.logger.LogInformation(this.blob_name, $"Offert, {fd.id} importerades till Fortnox. (request_for_quotation)"); }
            }
            else if (doc.document_type == "quotation")
            {
                // Log information
                await this.logger.LogInformation(this.blob_name, $"Dokument {fd.id} importerades inte till Fortnox. (quotation)");
            }
            else if (doc.document_type == "order")
            {
                // Log information
                await this.logger.LogInformation(this.blob_name, $"Påbörjar import av order {fd.id} till Fortnox.");

                // Import as order
                OrderRoot order = await AddOrder(sender.email, doc);
                if (order != null) { await this.logger.LogInformation(this.blob_name, $"Order, {fd.id} importerades till Fortnox. (order)"); }
            }
            else if (doc.document_type == "order_confirmation")
            {
                // Log information
                await this.logger.LogInformation(this.blob_name, $"Dokument {fd.id} importerades inte till Fortnox. (order_confirmation)");
            }
            else if (doc.document_type == "invoice")
            {
                // Log information
                await this.logger.LogInformation(this.blob_name, $"Påbörjar import av leverantörsfaktura {fd.id} till Fortnox.");

                // Import as supplier invoice
                SupplierInvoiceRoot supplier_invoice = await AddSupplierInvoice(sender.email, doc);
                if (supplier_invoice != null) { await this.logger.LogInformation(this.blob_name, $"Leverantörsfaktura, {fd.id} importerades till Fortnox. (invoice)"); }
            }
            else if (doc.document_type == "credit_invoice")
            {
                // Log information
                await this.logger.LogInformation(this.blob_name, $"Påbörjar import av leverantörskreditfaktura {fd.id} till Fortnox.");

                // Import as supplier credit invoice
                SupplierInvoiceRoot supplier_invoice = await AddSupplierInvoice(sender.email, doc);
                if (supplier_invoice != null) { await this.logger.LogInformation(this.blob_name, $"Leverantörskreditfaktura, {fd.id} importerades till Fortnox. (credit_invoice)"); }
            }

        } // End of the ImportDocument method

        /// <summary>
        /// Add a term of delivery if it does not exists
        /// </summary>
        public async Task<TermsOfDeliveryRoot> AddTermsOfDelivery(string term_of_delivery)
        {
            // Make sure that the input not is an empty string
            if (term_of_delivery == "")
                return null;

            // Get the root
            FortnoxResponse<TermsOfDeliveryRoot> fr = await this.nox_client.Get<TermsOfDeliveryRoot>($"termsofdeliveries/{term_of_delivery}");
            await this.logger.LogError(this.blob_name, fr.error);

            // Add the post if it does not exist
            if (fr.model == null)
            {
                // Create a new post
                fr.model = new TermsOfDeliveryRoot
                {
                    TermsOfDelivery = new TermsOfDelivery
                    {
                        Code = term_of_delivery,
                        Description = term_of_delivery
                    }
                };

                // Add the post
                fr = await this.nox_client.Add<TermsOfDeliveryRoot>(fr.model, $"termsofdeliveries");
                await this.logger.LogError(this.blob_name, fr.error);
                if (fr.model != null) { await this.logger.LogInformation(this.blob_name, $"{fr.model.TermsOfDelivery.Code} har lagts till i Fortnox. (Leveransvillkor)"); }
            }

            // Return a response
            return fr.model;

        } // End of the AddTermsOfDelivery method

        /// <summary>
        /// Add a term of payment if it does not exists
        /// </summary>
        public async Task<TermsOfPaymentRoot> AddTermsOfPayment(string term_of_payment)
        {
            // Make sure that the input not is an empty string
            if (term_of_payment == "")
                return null;

            // Get the root
            FortnoxResponse<TermsOfPaymentRoot> fr = await this.nox_client.Get<TermsOfPaymentRoot>($"termsofpayments/{term_of_payment}");
            await this.logger.LogError(this.blob_name, fr.error);

            // Add the post if it does not exist
            if (fr.model == null)
            {
                // Create a new post
                fr.model = new TermsOfPaymentRoot
                {
                    TermsOfPayment = new TermsOfPayment
                    {
                        Code = term_of_payment,
                        Description = term_of_payment
                    }
                };

                // Add the post
                fr = await this.nox_client.Add<TermsOfPaymentRoot>(fr.model, $"termsofpayments");
                await this.logger.LogError(this.blob_name, fr.error);
                if (fr.model != null) { await this.logger.LogInformation(this.blob_name, $"{fr.model.TermsOfPayment.Code} har lagts till i Fortnox. (Betalningsvillkor)"); }
            }

            // Return a response
            return fr.model;

        } // End of the AddTermsOfPayment method

        /// <summary>
        /// Add a way of delivery if it does not exists
        /// </summary>
        public async Task<WayOfDeliveryRoot> AddWayOfDelivery(string way_of_delivery)
        {
            // Make sure that the input not is an empty string
            if (way_of_delivery == "")
                return null;

            // Get the root
            FortnoxResponse<WayOfDeliveryRoot> fr = await this.nox_client.Get<WayOfDeliveryRoot>($"wayofdeliveries/{way_of_delivery}");
            await this.logger.LogError(this.blob_name, fr.error);

            // Add the post if it does not exist
            if (fr.model == null)
            {
                // Create a new post
                fr.model = new WayOfDeliveryRoot
                {
                    WayOfDelivery = new WayOfDelivery
                    {
                        Code = way_of_delivery,
                        Description = way_of_delivery
                    }
                };

                // Add the post
                fr = await this.nox_client.Add<WayOfDeliveryRoot>(fr.model, $"wayofdeliveries");
                await this.logger.LogError(this.blob_name, fr.error);
                if (fr.model != null) { await this.logger.LogInformation(this.blob_name, $"{fr.model.WayOfDelivery.Code} har lagts till i Fortnox. (Leveranssätt)"); }
            }

            // Return a response
            return fr.model;

        } // End of the AddWayOfDelivery method

        /// <summary>
        /// Add a currency if it does not exists
        /// </summary>
        public async Task<CurrencyRoot> AddCurrency(string currency_code)
        {
            // Make sure that the input not is an empty string
            if (currency_code == "")
                return null;

            // Get the root
            FortnoxResponse<CurrencyRoot> fr = await this.nox_client.Get<CurrencyRoot>($"currencies/{currency_code}");
            await this.logger.LogError(this.blob_name, fr.error);

            // Add the currency if it does not exist
            if (fr.model == null)
            {
                // Create a new post
                fr.model = new CurrencyRoot
                {
                    Currency = new Currency
                    {
                        Code = currency_code,
                        Description = currency_code
                    }
                };

                // Add the post
                fr = await this.nox_client.Add<CurrencyRoot>(fr.model, $"currencies");
                await this.logger.LogError(this.blob_name, fr.error);
                if (fr.model != null) { await this.logger.LogInformation(this.blob_name, $"{fr.model.Currency.Code} har lagts till i Fortnox. (Valuta)"); }
            }

            // Return a response
            return fr.model;

        } // End of the AddCurrency method

        /// <summary>
        /// Add a unit if it does not exists
        /// </summary>
        public async Task<UnitRoot> AddUnit(string unit_code)
        {
            // Get the root
            FortnoxResponse<UnitRoot> fr = await this.nox_client.Get<UnitRoot>($"units/{unit_code}");
            await this.logger.LogError(this.blob_name, fr.error);

            // Add the unit if it does not exist
            if (fr.model == null)
            {
                // Create a new post
                fr.model = new UnitRoot
                {
                    Unit = new Unit
                    {
                        Code = unit_code,
                        Description = unit_code
                    }
                };

                // Add a unit
                fr = await this.nox_client.Add<UnitRoot>(fr.model, $"units");
                await this.logger.LogError(this.blob_name, fr.error);
                if (fr.model != null) { await this.logger.LogInformation(this.blob_name, $"{fr.model.Unit.Code} har lagts till i Fortnox. (Enhet)"); }
            }

            // Return a response
            return fr.model;

        } // End of the AddUnit method

        /// <summary>
        /// Add a price list if it does not exists
        /// </summary>
        public async Task<PriceListRoot> AddPriceList(string code)
        {
            // Get the root
            FortnoxResponse<PriceListRoot> fr = await this.nox_client.Get<PriceListRoot>($"pricelists/{code}");
            await this.logger.LogError(this.blob_name, fr.error);

            // Add the price list if it does not exist
            if (fr.model == null)
            {
                // Create a new post
                fr.model = new PriceListRoot
                {
                    PriceList = new PriceList
                    {
                        Code = code,
                        Description = code
                    }
                };

                // Add a price list
                fr = await this.nox_client.Add<PriceListRoot>(fr.model, $"pricelists");
                await this.logger.LogError(this.blob_name, fr.error);
                if (fr.model != null) { await this.logger.LogInformation(this.blob_name, $"{fr.model.PriceList.Code} har lagts till i Fortnox. (Prislista)"); }
            }

            // Return a response
            return fr.model;

        } // End of the AddPriceList method

        /// <summary>
        /// Add an account if it does not exist
        /// </summary>
        public async Task<AccountRoot> AddAccount(string account_number)
        {
            // Get the root
            FortnoxResponse<AccountRoot> fr = await this.nox_client.Get<AccountRoot>($"accounts/{account_number}");
            await this.logger.LogError(this.blob_name, fr.error);

            // Add the account if it does not exist
            if (fr.model == null)
            {
                // Create a new post
                fr.model = new AccountRoot
                {
                    Account = new Annytab.Fortnox.Client.V3.Account
                    {
                        Number = account_number,
                        Description = account_number
                    }
                };

                // Add an account
                fr = await this.nox_client.Add<AccountRoot>(fr.model, $"accounts");
                await this.logger.LogError(this.blob_name, fr.error);
                if (fr.model != null) { await this.logger.LogInformation(this.blob_name, $"{fr.model.Account.Number} har lagts till i Fortnox. (Konto)"); }
            }

            // Return a response
            return fr.model;

        } // End of the AddAccount method

        /// <summary>
        /// Add an article if it does not exist
        /// </summary>
        public async Task<ArticleRoot> AddArticle(ProductRow row)
        {
            // Create a reference to an article root
            FortnoxResponse<ArticleRoot> fr = new FortnoxResponse<ArticleRoot>();

            // Make sure that the product code only consists of alphanumeric characters
            row.product_code = string.IsNullOrEmpty(row.product_code) == false ? ConvertToAlphanumeric(row.product_code) : null;

            // Find the article
            if (string.IsNullOrEmpty(row.gtin) == false)
            {
                // Try to get articles on EAN
                FortnoxResponse<ArticlesRoot> fr_page = await this.nox_client.Get<ArticlesRoot>($"articles?ean={row.gtin}");
                await this.logger.LogError(this.blob_name, fr_page.error);

                // Make sure that at least one article was found
                if (fr_page.model != null && fr_page.model.Articles != null && fr_page.model.Articles.Count > 0)
                {
                    // Get an article
                    fr = await this.nox_client.Get<ArticleRoot>($"articles/{fr_page.model.Articles[0].ArticleNumber}");
                    await this.logger.LogError(this.blob_name, fr.error);
                }
            }
            if (fr.model == null && string.IsNullOrEmpty(row.manufacturer_code) == false)
            {
                // Try to get articles on manufacturer code
                FortnoxResponse<ArticlesRoot> fr_page = await this.nox_client.Get<ArticlesRoot>($"articles?manufacturerarticlenumber={row.manufacturer_code}");
                await this.logger.LogError(this.blob_name, fr_page.error);

                // Make sure that at least one article was found
                if (fr_page.model != null && fr_page.model.Articles != null && fr_page.model.Articles.Count > 0)
                {
                    // Get an article
                    fr = await this.nox_client.Get<ArticleRoot>($"articles/{fr_page.model.Articles[0].ArticleNumber}");
                    await this.logger.LogError(this.blob_name, fr.error);
                }
            }
            if (fr.model == null && string.IsNullOrEmpty(row.product_code) == false)
            {
                // Get an article
                fr = await this.nox_client.Get<ArticleRoot>($"articles/{row.product_code}");
                await this.logger.LogError(this.blob_name, fr.error);
            }

            // Add the article if it does not exist
            if (fr.model == null)
            {
                // Create a new article
                fr.model = new ArticleRoot
                {
                    Article = new Article
                    {
                        ArticleNumber = string.IsNullOrEmpty(row.product_code) == false ? row.product_code : null,
                        ConstructionAccount = this.nox_api_values.SalesAccountSEREVERSEDVAT,
                        Description = row.product_name,
                        EAN = string.IsNullOrEmpty(row.gtin) == false ? row.gtin : null,
                        EUAccount = this.nox_api_values.SalesAccountEUREVERSEDVAT,
                        EUVATAccount = this.nox_api_values.SalesAccountEUVAT,
                        ExportAccount = this.nox_api_values.SalesAccountEXPORT,
                        ManufacturerArticleNumber = string.IsNullOrEmpty(row.manufacturer_code) == false ? row.manufacturer_code : null,
                        PurchaseAccount = this.nox_api_values.PurchaseAccount,
                        SalesAccount = GetArticleSalesAccount(row.vat_rate),
                        StockGoods = this.nox_api_values.StockArticle,
                        StockAccount = this.nox_api_values.StockAccount,
                        StockChangeAccount = this.nox_api_values.StockChangeAccount,
                        Unit = string.IsNullOrEmpty(row.unit_code) == false ? row.unit_code : null
                    }
                };

                // Add an article
                fr = await this.nox_client.Add<ArticleRoot>(fr.model, "articles");
                await this.logger.LogError(this.blob_name, fr.error);
                if (fr.model != null) { await this.logger.LogInformation(this.blob_name, $"{fr.model.Article.ArticleNumber} har lagts till i Fortnox. (Artikel)"); }

                // Add a default price
                if (fr.model != null)
                {
                    PriceRoot price = new PriceRoot
                    {
                        Price = new Price
                        {
                            ArticleNumber = fr.model.Article.ArticleNumber,
                            PriceList = this.nox_api_values.PriceList,
                            FromQuantity = 0,
                            Amount = row.unit_price
                        }
                    };

                    // Add a price
                    FortnoxResponse<PriceRoot> fr_price = await this.nox_client.Add<PriceRoot>(price, "prices");
                    await this.logger.LogError(this.blob_name, fr_price.error);
                    if (fr_price.model != null) { await this.logger.LogInformation(this.blob_name, $"{fr.model.Article.ArticleNumber} fick priset {fr_price.model.Price} i Fortnox. (Artikelpris)"); }
                }
            }

            // Return a response
            return fr.model;

        } // End of the AddArticle method

        /// <summary>
        /// Add or update a customer
        /// </summary>
        public async Task<CustomerRoot> UpsertCustomer(string dox_email, AnnytabDoxTrade doc)
        {
            // Create variables
            FortnoxResponse<CustomerRoot> fr = new FortnoxResponse<CustomerRoot>();
            bool customer_exists = false;
            string customer_email = doc.buyer_information != null && string.IsNullOrEmpty(doc.buyer_information.email) == false ? doc.buyer_information.email : dox_email;

            // Find customers on email
            FortnoxResponse<CustomersRoot> fr_page = await this.nox_client.Get<CustomersRoot>($"customers?email={customer_email}");
            await this.logger.LogError(this.blob_name, fr_page.error);

            // Make sure that at least one customer was found
            if (fr_page.model != null && fr_page.model.Customers != null && fr_page.model.Customers.Count > 0)
            {
                // Get a customer
                fr = await this.nox_client.Get<CustomerRoot>($"customers/{fr_page.model.Customers[0].CustomerNumber}");
                await this.logger.LogError( this.blob_name, fr.error);
            }

            // Check if the customer exists
            if (fr.model != null)
            {
                customer_exists = true;
            }
            else
            {
                fr.model = new CustomerRoot { Customer = new Customer() };
            }

            // Update the customer: ONLY SET VAT TYPE, ACCOUNT IS SET IN ARTICLE
            fr.model.Customer.Email = customer_email;
            if (doc.seller_information != null)
            {
                fr.model.Customer.OurReference = string.IsNullOrEmpty(fr.model.Customer.OurReference) == true ? doc.seller_information.contact_name : fr.model.Customer.OurReference;
            }
            if (doc.buyer_information != null)
            {
                fr.model.Customer.Name = string.IsNullOrEmpty(doc.buyer_information.person_name) == false ? doc.buyer_information.person_name : fr.model.Customer.Name;
                fr.model.Customer.OrganisationNumber = string.IsNullOrEmpty(doc.buyer_information.person_id) == false ? doc.buyer_information.person_id : fr.model.Customer.OrganisationNumber;
                fr.model.Customer.VATNumber = string.IsNullOrEmpty(doc.buyer_information.vat_number) == false ? doc.buyer_information.vat_number : fr.model.Customer.VATNumber;
                fr.model.Customer.YourReference = string.IsNullOrEmpty(doc.buyer_information.contact_name) == false ? doc.buyer_information.contact_name : fr.model.Customer.YourReference;
                fr.model.Customer.Phone1 = string.IsNullOrEmpty(doc.buyer_information.phone_number) == false ? doc.buyer_information.phone_number : fr.model.Customer.Phone1;
                fr.model.Customer.Address1 = string.IsNullOrEmpty(doc.buyer_information.address_line_1) == false ? doc.buyer_information.address_line_1 : fr.model.Customer.Address1;
                fr.model.Customer.Address2 = string.IsNullOrEmpty(doc.buyer_information.address_line_2) == false ? doc.buyer_information.address_line_2 : fr.model.Customer.Address2;
                fr.model.Customer.ZipCode = string.IsNullOrEmpty(doc.buyer_information.postcode) == false ? doc.buyer_information.postcode : fr.model.Customer.ZipCode;
                fr.model.Customer.City = string.IsNullOrEmpty(doc.buyer_information.city_name) == false ? doc.buyer_information.city_name : fr.model.Customer.City;
                fr.model.Customer.CountryCode = string.IsNullOrEmpty(doc.buyer_information.country_code) == false ? doc.buyer_information.country_code : fr.model.Customer.CountryCode;
                fr.model.Customer.EmailOffer = string.IsNullOrEmpty(fr.model.Customer.EmailOffer) == true ? customer_email : fr.model.Customer.EmailOffer;
                fr.model.Customer.EmailOrder = string.IsNullOrEmpty(fr.model.Customer.EmailOrder) == true ? customer_email : fr.model.Customer.EmailOrder;
                fr.model.Customer.EmailInvoice = string.IsNullOrEmpty(fr.model.Customer.EmailInvoice) == true ? customer_email : fr.model.Customer.EmailInvoice;
            }
            if (doc.delivery_information != null)
            {
                fr.model.Customer.DeliveryName = string.IsNullOrEmpty(doc.delivery_information.person_name) == false ? doc.delivery_information.person_name : fr.model.Customer.DeliveryName;
                fr.model.Customer.DeliveryPhone1 = string.IsNullOrEmpty(doc.delivery_information.phone_number) == false ? doc.delivery_information.phone_number : fr.model.Customer.DeliveryPhone1;
                fr.model.Customer.DeliveryAddress1 = string.IsNullOrEmpty(doc.delivery_information.address_line_1) == false ? doc.delivery_information.address_line_1 : fr.model.Customer.DeliveryAddress1;
                fr.model.Customer.DeliveryAddress2 = string.IsNullOrEmpty(doc.delivery_information.address_line_2) == false ? doc.delivery_information.address_line_2 : fr.model.Customer.DeliveryAddress2;
                fr.model.Customer.DeliveryCity = string.IsNullOrEmpty(doc.delivery_information.city_name) == false ? doc.delivery_information.city_name : fr.model.Customer.DeliveryCity;
                fr.model.Customer.DeliveryCountryCode = string.IsNullOrEmpty(doc.delivery_information.country_code) == false ? doc.delivery_information.country_code : fr.model.Customer.DeliveryCountryCode;
                fr.model.Customer.DeliveryZipCode = string.IsNullOrEmpty(doc.delivery_information.postcode) == false ? doc.delivery_information.postcode : fr.model.Customer.DeliveryZipCode;
            }
            fr.model.Customer.Currency = string.IsNullOrEmpty(doc.currency_code) == false ? doc.currency_code : fr.model.Customer.Currency;
            fr.model.Customer.TermsOfDelivery = string.IsNullOrEmpty(doc.terms_of_delivery) == false ? doc.terms_of_delivery : fr.model.Customer.TermsOfDelivery;
            fr.model.Customer.TermsOfPayment = string.IsNullOrEmpty(doc.terms_of_payment) == false ? doc.terms_of_payment : fr.model.Customer.TermsOfPayment;
            fr.model.Customer.VATType = GetCustomerVatType(fr.model.Customer);
            fr.model.Customer.WayOfDelivery = string.IsNullOrEmpty(doc.mode_of_delivery) == false ? doc.mode_of_delivery : fr.model.Customer.WayOfDelivery;
            fr.model.Customer.Type = string.IsNullOrEmpty(fr.model.Customer.Type) == true && string.IsNullOrEmpty(fr.model.Customer.VATNumber) == false ? "COMPANY" : fr.model.Customer.Type;
            fr.model.Customer.PriceList = string.IsNullOrEmpty(fr.model.Customer.PriceList) == true ? this.nox_api_values.PriceList : fr.model.Customer.PriceList;

            // Add or update the customer
            if (customer_exists == true)
            {
                fr = await this.nox_client.Update<CustomerRoot>(fr.model, $"customers/{fr.model.Customer.CustomerNumber}");
                await this.logger.LogError(this.blob_name, fr.error);
                if (fr.model != null) { await this.logger.LogInformation(this.blob_name, $"Kund {fr.model.Customer.CustomerNumber} har uppdaterats i Fortnox. (Kund)"); }
            }
            else
            {
                fr = await this.nox_client.Add<CustomerRoot>(fr.model, "customers");
                await this.logger.LogError(this.blob_name, fr.error);
                if (fr.model != null) { await this.logger.LogInformation(this.blob_name, $"Kund {fr.model.Customer.CustomerNumber} har lagts till i Fortnox. (Kund)"); }
            }

            // Return a response
            return fr.model;

        } // End of the UpsertCustomer method

        /// <summary>
        /// Add or update a supplier
        /// </summary>
        public async Task<SupplierRoot> UpsertSupplier(string dox_email, AnnytabDoxTrade doc)
        {
            // Create variables
            FortnoxResponse<SupplierRoot> fr = new FortnoxResponse<SupplierRoot>();
            bool supplier_exists = false;
            string supplier_email = doc.seller_information != null && string.IsNullOrEmpty(doc.seller_information.email) == false ? doc.seller_information.email : dox_email;

            // Find suppliers on email
            FortnoxResponse<SuppliersRoot> fr_page = await this.nox_client.Get<SuppliersRoot>($"suppliers?email={supplier_email}");
            await this.logger.LogError(this.blob_name, fr_page.error);

            // Make sure that at least one supplier was found
            if (fr_page.model != null && fr_page.model.Suppliers != null && fr_page.model.Suppliers.Count > 0)
            {
                // Get a supplier
                fr = await this.nox_client.Get<SupplierRoot>($"suppliers/{fr_page.model.Suppliers[0].SupplierNumber}");
                await this.logger.LogError(this.blob_name, fr.error);
            }

            // Check if the supplier exists
            if (fr.model != null)
            {
                supplier_exists = true;
            }
            else
            {
                fr.model = new SupplierRoot { Supplier = new Supplier() };
            }

            // Update the supplier
            fr.model.Supplier.Email = supplier_email;
            if (doc.buyer_information != null)
            {
                fr.model.Supplier.OurReference = string.IsNullOrEmpty(fr.model.Supplier.OurReference) == true ? doc.buyer_information.contact_name : fr.model.Supplier.OurReference;
            }
            if (doc.seller_information != null)
            {
                fr.model.Supplier.Name = string.IsNullOrEmpty(doc.seller_information.person_name) == false ? doc.seller_information.person_name : fr.model.Supplier.Name;
                fr.model.Supplier.OrganisationNumber = string.IsNullOrEmpty(doc.seller_information.person_id) == false ? doc.seller_information.person_id : fr.model.Supplier.OrganisationNumber;
                fr.model.Supplier.VATNumber = string.IsNullOrEmpty(doc.seller_information.vat_number) == false ? doc.seller_information.vat_number : fr.model.Supplier.VATNumber;
                fr.model.Supplier.YourReference = string.IsNullOrEmpty(doc.seller_information.contact_name) == false ? doc.seller_information.contact_name : fr.model.Supplier.YourReference;
                fr.model.Supplier.Phone1 = string.IsNullOrEmpty(doc.seller_information.phone_number) == false ? doc.seller_information.phone_number : fr.model.Supplier.Phone1;
                fr.model.Supplier.Address1 = string.IsNullOrEmpty(doc.seller_information.address_line_1) == false ? doc.seller_information.address_line_1 : fr.model.Supplier.Address1;
                fr.model.Supplier.Address2 = string.IsNullOrEmpty(doc.seller_information.address_line_2) == false ? doc.seller_information.address_line_2 : fr.model.Supplier.Address2;
                fr.model.Supplier.ZipCode = string.IsNullOrEmpty(doc.seller_information.postcode) == false ? doc.seller_information.postcode : fr.model.Supplier.ZipCode;
                fr.model.Supplier.City = string.IsNullOrEmpty(doc.seller_information.city_name) == false ? doc.seller_information.city_name : fr.model.Supplier.City;
                fr.model.Supplier.CountryCode = string.IsNullOrEmpty(doc.seller_information.country_code) == false ? doc.seller_information.country_code : fr.model.Supplier.CountryCode;
            }
            fr.model.Supplier.Currency = string.IsNullOrEmpty(doc.currency_code) == false ? doc.currency_code : fr.model.Supplier.Currency;
            fr.model.Supplier.TermsOfPayment = string.IsNullOrEmpty(doc.terms_of_payment) == false ? doc.terms_of_payment : fr.model.Supplier.TermsOfPayment;
            fr.model.Supplier.VATType = string.IsNullOrEmpty(fr.model.Supplier.VATType) == true ? "NORMAL" : fr.model.Supplier.VATType;
            fr.model.Supplier.OurCustomerNumber = doc.buyer_references != null && doc.buyer_references.ContainsKey("customer_id") ? doc.buyer_references["customer_id"] : null;
            if (doc.payment_options != null)
            {
                // Loop payment options
                foreach (PaymentOption po in doc.payment_options)
                {
                    // Get the name
                    string name = po.name.ToUpper();

                    // Add information based on name
                    if (name == "IBAN")
                    {
                        fr.model.Supplier.BIC = string.IsNullOrEmpty(po.bank_identifier_code) == false ? po.bank_identifier_code : fr.model.Supplier.BIC;
                        fr.model.Supplier.IBAN = string.IsNullOrEmpty(po.account_reference) == false ? po.account_reference : fr.model.Supplier.IBAN;
                    }
                    else if (name == "BG")
                    {
                        fr.model.Supplier.BG = string.IsNullOrEmpty(po.account_reference) == false ? po.account_reference : fr.model.Supplier.BG;
                    }
                    else if (name == "PG")
                    {
                        fr.model.Supplier.PG = string.IsNullOrEmpty(po.account_reference) == false ? po.account_reference : fr.model.Supplier.PG;
                    }
                    else if (name == "BANK")
                    {
                        fr.model.Supplier.BankAccountNumber = string.IsNullOrEmpty(po.account_reference) == false ? po.account_reference.Replace(" ", "").Replace("-", "") : fr.model.Supplier.BankAccountNumber;
                        fr.model.Supplier.Bank = string.IsNullOrEmpty(po.bank_name) == false ? po.bank_name : fr.model.Supplier.Bank;
                    }
                }
            }

            // Add or update the supplier
            if (supplier_exists == true)
            {
                fr = await this.nox_client.Update<SupplierRoot>(fr.model, $"suppliers/{fr.model.Supplier.SupplierNumber}");
                await this.logger.LogError(this.blob_name, fr.error);
                if (fr.model != null) { await this.logger.LogInformation(this.blob_name, $"Leverantör {fr.model.Supplier.SupplierNumber} har uppdaterats i Fortnox. (Leverantör)"); }
            }
            else
            {
                fr = await this.nox_client.Add<SupplierRoot>(fr.model, "suppliers");
                await this.logger.LogError(this.blob_name, fr.error);
                if (fr.model != null) { await this.logger.LogInformation(this.blob_name, $"Leverantör {fr.model.Supplier.SupplierNumber} har lagts till i Fortnox. (Leverantör)"); }
            }

            // Return a response
            return fr.model;

        } // End of the UpsertSupplier method

        /// <summary>
        /// Get trusted email senders
        /// </summary>
        public async Task<EmailSendersRoot> GetTrustedEmailSenders()
        {
            // Get a response
            FortnoxResponse<EmailSendersRoot> fr = await this.nox_client.Get<EmailSendersRoot>("emailsenders");
            await this.logger.LogError(this.blob_name, fr.error);

            // Return a response
            return fr.model;

        } // End of the GetTrustedEmailSenders method

        /// <summary>
        /// Add an offer
        /// </summary>
        public async Task<OfferRoot> AddOffer(string dox_email, AnnytabDoxTrade doc)
        {
            // Terms of delivery
            if (string.IsNullOrEmpty(doc.terms_of_delivery) == false)
            {
                doc.terms_of_delivery = ConvertToAlphanumeric(doc.terms_of_delivery).ToUpper();
                await AddTermsOfDelivery(doc.terms_of_delivery);
            }

            // Terms of payment
            if (string.IsNullOrEmpty(doc.terms_of_payment) == false)
            {
                doc.terms_of_payment = ConvertToAlphanumeric(doc.terms_of_payment).ToUpper().Replace("-", "");
                await AddTermsOfPayment(doc.terms_of_payment);
            }

            // Way of delivery
            if (string.IsNullOrEmpty(doc.mode_of_delivery) == false)
            {
                doc.mode_of_delivery = ConvertToAlphanumeric(doc.mode_of_delivery).ToUpper();
                await AddWayOfDelivery(doc.mode_of_delivery);
            }

            // Currency
            if (string.IsNullOrEmpty(doc.currency_code) == false)
            {
                doc.currency_code = doc.currency_code.ToUpper();
                await AddCurrency(doc.currency_code);
            }

            // Upsert the customer
            CustomerRoot customer = await UpsertCustomer(dox_email, doc);

            // Return if the customer is null and log error information
            if (customer == null || customer.Customer == null)
            {
                return null;
            }

            // Create a list with offer rows
            IList<OfferRow> rows = new List<OfferRow>();

            // Add offer rows
            if (doc.product_rows != null)
            {
                await AddOfferRows(doc.product_rows, rows);
            }

            // Create an offer
            OfferRoot root = new OfferRoot
            {
                Offer = new Annytab.Fortnox.Client.V3.Offer
                {
                    CustomerNumber = customer.Customer.CustomerNumber,
                    OfferDate = string.IsNullOrEmpty(doc.issue_date) == false ? doc.issue_date : null,
                    DeliveryDate = string.IsNullOrEmpty(doc.delivery_date) == false ? doc.delivery_date : null,
                    ExpireDate = string.IsNullOrEmpty(doc.offer_expires_date) == false ? doc.offer_expires_date : null,
                    YourReferenceNumber = doc.buyer_references != null && doc.buyer_references.ContainsKey("request_for_quotation_id") ? doc.buyer_references["request_for_quotation_id"] : null,
                    Comments = doc.comment,
                    OfferRows = rows,
                    Currency = doc.currency_code,
                    VATIncluded = false
                }
            };

            // Add the offer
            FortnoxResponse<OfferRoot> fr = await this.nox_client.Add<OfferRoot>(root, "offers");
            await this.logger.LogError(this.blob_name, fr.error);

            // Return a response
            return fr.model;

        } // End of the AddOffer method

        /// <summary>
        /// Add an order
        /// </summary>
        public async Task<OrderRoot> AddOrder(string dox_email, AnnytabDoxTrade doc)
        {
            // Terms of delivery
            if (string.IsNullOrEmpty(doc.terms_of_delivery) == false)
            {
                doc.terms_of_delivery = ConvertToAlphanumeric(doc.terms_of_delivery).ToUpper();
                await AddTermsOfDelivery(doc.terms_of_delivery);
            }

            // Terms of payment
            if (string.IsNullOrEmpty(doc.terms_of_payment) == false)
            {
                doc.terms_of_payment = ConvertToAlphanumeric(doc.terms_of_payment).ToUpper().Replace("-", "");
                await AddTermsOfPayment(doc.terms_of_payment);
            }

            // Way of delivery
            if (string.IsNullOrEmpty(doc.mode_of_delivery) == false)
            {
                doc.mode_of_delivery = ConvertToAlphanumeric(doc.mode_of_delivery).ToUpper();
                await AddWayOfDelivery(doc.mode_of_delivery);
            }

            // Currency
            if (string.IsNullOrEmpty(doc.currency_code) == false)
            {
                doc.currency_code = doc.currency_code.ToUpper();
                await AddCurrency(doc.currency_code);
            }

            // Upsert the customer
            CustomerRoot customer = await UpsertCustomer(dox_email, doc);

            // Return if the customer is null and log error information
            if (customer == null || customer.Customer == null)
            {
                return null;
            }

            // Create a list with order rows
            IList<OrderRow> rows = new List<OrderRow>();

            // Add order rows
            if (doc.product_rows != null)
            {
                await AddOrderRows(doc.product_rows, rows);
            }

            // Create an order
            OrderRoot root = new OrderRoot
            {
                Order = new Order
                {
                    CustomerNumber = customer.Customer.CustomerNumber,
                    OrderDate = string.IsNullOrEmpty(doc.issue_date) == false ? doc.issue_date : null,
                    DeliveryDate = string.IsNullOrEmpty(doc.delivery_date) == false ? doc.delivery_date : null,
                    YourOrderNumber = doc.buyer_references != null && doc.buyer_references.ContainsKey("order_id") ? doc.buyer_references["order_id"] : null,
                    ExternalInvoiceReference1 = string.IsNullOrEmpty(doc.payment_reference) == false ? doc.payment_reference : null,
                    ExternalInvoiceReference2 = string.IsNullOrEmpty(doc.id) == false ? doc.id : null,
                    Comments = doc.comment,
                    OrderRows = rows,
                    Currency = doc.currency_code,
                    VATIncluded = false
                }
            };

            // Add the order
            FortnoxResponse<OrderRoot> fr = await this.nox_client.Add<OrderRoot>(root, "orders");
            await this.logger.LogError(this.blob_name, fr.error);

            // Return a response
            return fr.model;

        } // End of the AddOrder method

        /// <summary>
        /// Add an supplier invoice
        /// </summary>
        public async Task<SupplierInvoiceRoot> AddSupplierInvoice(string dox_email, AnnytabDoxTrade doc)
        {
            // Terms of payment
            if (string.IsNullOrEmpty(doc.terms_of_payment) == false)
            {
                doc.terms_of_payment = ConvertToAlphanumeric(doc.terms_of_payment).ToUpper().Replace("-", "");
                await AddTermsOfPayment(doc.terms_of_payment);
            }

            // Currency
            if (string.IsNullOrEmpty(doc.currency_code) == false)
            {
                doc.currency_code = doc.currency_code.ToUpper();
                await AddCurrency(doc.currency_code);
            }

            // Upsert the supplier
            SupplierRoot supplier = await UpsertSupplier(dox_email, doc);

            // Return if the supplier_root is null and log error information
            if (supplier == null || supplier.Supplier == null)
            {
                return null;
            }

            // Create a list with supplier invoice rows
            IList<SupplierInvoiceRow> rows = new List<SupplierInvoiceRow>();

            // Add accounts payable amount
            if (doc.total != null && doc.total != 0M)
            {
                rows.Add(new SupplierInvoiceRow
                {
                    Code = "TOT",
                    Total = doc.total * -1
                });
            }

            // Add value added tax
            if (doc.vat_total != null && doc.vat_total != 0M)
            {
                rows.Add(new SupplierInvoiceRow
                {
                    Code = "VAT",
                    Total = doc.vat_total
                });
            }

            // Add rounding
            if (doc.rounding != null && doc.rounding != 0M)
            {
                rows.Add(new SupplierInvoiceRow
                {
                    Code = "ROV",
                    Total = doc.rounding
                });
            }

            // Add supplier invoice rows
            if (doc.product_rows != null)
            {
                await AddSupplierInvoiceRows(doc.product_rows, rows);
            }

            // Create a supplier invoice
            SupplierInvoiceRoot root = new SupplierInvoiceRoot
            {
                SupplierInvoice = new SupplierInvoice
                {
                    SupplierNumber = supplier.Supplier.SupplierNumber,
                    InvoiceNumber = string.IsNullOrEmpty(doc.payment_reference) == false ? doc.payment_reference : null,
                    InvoiceDate = string.IsNullOrEmpty(doc.issue_date) == false ? doc.issue_date : null,
                    DueDate = string.IsNullOrEmpty(doc.due_date) == false ? doc.due_date : null,
                    Currency = doc.currency_code,
                    Comments = doc.comment,
                    SupplierInvoiceRows = rows
                }
            };

            // Add a supplier invoice
            FortnoxResponse<SupplierInvoiceRoot> fr = await this.nox_client.Add<SupplierInvoiceRoot>(root, "supplierinvoices");
            await this.logger.LogError(this.blob_name, fr.error);

            // Return a response
            return fr.model;

        } // End of the AddSupplierInvoice method

        /// <summary>
        /// Add offer rows recursively
        /// </summary>
        private async Task AddOfferRows(IList<ProductRow> product_rows, IList<OfferRow> offer_rows)
        {
            // Loop product rows
            foreach (ProductRow row in product_rows)
            {
                // Unit
                if (string.IsNullOrEmpty(row.unit_code) == false)
                {
                    row.unit_code = ConvertToAlphanumeric(row.unit_code).ToLower();
                    await AddUnit(row.unit_code);
                }

                // Article, add if there is an identifier
                ArticleRoot article = null;
                if (string.IsNullOrEmpty(row.product_code) == false || string.IsNullOrEmpty(row.manufacturer_code) == false || string.IsNullOrEmpty(row.gtin) == false)
                {
                    article = await AddArticle(row);
                }

                // Add a offer row
                offer_rows.Add(new OfferRow
                {
                    ArticleNumber = article != null ? article.Article.ArticleNumber : null,
                    Description = row.product_name,
                    Quantity = row.quantity,
                    Price = row.unit_price,
                    Unit = article != null ? article.Article.Unit : row.unit_code
                });

                // Check if there is sub rows
                if (row.subrows != null && row.subrows.Count > 0)
                {
                    await AddOfferRows(row.subrows, offer_rows);
                }
            }

        } // End of the AddOfferRows method

        /// <summary>
        /// Add order rows recursively
        /// </summary>
        private async Task AddOrderRows(IList<ProductRow> product_rows, IList<OrderRow> order_rows)
        {
            // Loop product rows
            foreach (ProductRow row in product_rows)
            {
                // Unit
                if (string.IsNullOrEmpty(row.unit_code) == false)
                {
                    row.unit_code = ConvertToAlphanumeric(row.unit_code).ToLower();
                    await AddUnit(row.unit_code);
                }

                // Article, add if there is an identifier
                ArticleRoot article = null;
                if (string.IsNullOrEmpty(row.product_code) == false || string.IsNullOrEmpty(row.manufacturer_code) == false || string.IsNullOrEmpty(row.gtin) == false)
                {
                    article = await AddArticle(row);
                }

                // Add a order row
                order_rows.Add(new OrderRow
                {
                    ArticleNumber = article != null ? article.Article.ArticleNumber : null,
                    Description = row.product_name,
                    OrderedQuantity = row.quantity,
                    DeliveredQuantity = row.quantity,
                    Price = row.unit_price,
                    Unit = article != null ? article.Article.Unit : row.unit_code
                });

                // Check if there is sub rows
                if (row.subrows != null && row.subrows.Count > 0)
                {
                    await AddOrderRows(row.subrows, order_rows);
                }
            }

        } // End of the AddOrderRows method

        /// <summary>
        /// Add supplier invoice rows recursively
        /// </summary>
        private async Task AddSupplierInvoiceRows(IList<ProductRow> product_rows, IList<SupplierInvoiceRow> supplier_invoice_rows)
        {
            // Loop product rows
            foreach (ProductRow row in product_rows)
            {
                // Unit
                if (string.IsNullOrEmpty(row.unit_code) == false)
                {
                    row.unit_code = ConvertToAlphanumeric(row.unit_code).ToLower();
                    await AddUnit(row.unit_code);
                }

                // Article, add if there is an identifier
                ArticleRoot article = null;
                if (string.IsNullOrEmpty(row.product_code) == false || string.IsNullOrEmpty(row.manufacturer_code) == false || string.IsNullOrEmpty(row.gtin) == false)
                {
                    article = await AddArticle(row);
                }

                // Add a supplier invoice row
                supplier_invoice_rows.Add(new SupplierInvoiceRow
                {
                    ArticleNumber = article != null ? article.Article.ArticleNumber : null,
                    Account = article == null ? this.nox_api_values.PurchaseAccount : null,
                    ItemDescription = row.product_name,
                    Quantity = row.quantity,
                    Price = row.unit_price
                    //Unit = article != null ? article.Article.Unit : row.unit_code
                });

                // Check if there is sub rows
                if (row.subrows != null && row.subrows.Count > 0)
                {
                    await AddSupplierInvoiceRows(row.subrows, supplier_invoice_rows);
                }
            }

        } // End of the AddSupplierInvoiceRows method

        #endregion

        #region Export methods

        /// <summary>
        /// Get labels
        /// </summary>
        public async Task<IDictionary<string, string>> GetLabels()
        {
            // Create the response to return
            IDictionary<string, string> dictionary = new Dictionary<string, string>();

            // Get labels
            FortnoxResponse<LabelsRoot> fr_lbl = await this.nox_client.Get<LabelsRoot>("labels");
            await this.logger.LogError(this.blob_name, fr_lbl.error);

            // Make sure that root and root.Labels not is null
            if (fr_lbl.model == null || fr_lbl.model.Labels == null)
            {
                return dictionary;
            }

            // Loop the list
            foreach (Label label in fr_lbl.model.Labels)
            {
                dictionary.Add(label.Id, label.Description);
            }

            // Return a response
            return dictionary;

        } // End of the GetLabels method

        /// <summary>
        /// Get company settings
        /// </summary>
        public async Task<CompanySettingsRoot> GetCompanySettings()
        {
            // Get company settings
            FortnoxResponse<CompanySettingsRoot> fr = await this.nox_client.Get<CompanySettingsRoot>("settings/company");
            await this.logger.LogError(this.blob_name, fr.error);

            // Return a response
            return fr.model;

        } // End of the GetCompanySettings method

        /// <summary>
        /// Get offers
        /// </summary>
        public async Task<OffersRoot> GetOffers()
        {
            // Variables
            Int32 page = 1;

            // Get a page with offers
            FortnoxResponse<OffersRoot> fr = await this.nox_client.Get<OffersRoot>($"offers?sent=false&limit=10&page={page}");
            await this.logger.LogError(this.blob_name, fr.error);

            // Calculate the total number of pages
            Int32? total_pages = fr.model != null && fr.model.MetaInformation != null ? fr.model.MetaInformation.TotalPages : 1;

            // Loop while there is more pages to get
            while (page < total_pages)
            {
                // Increase the page number
                page += 1;

                // Get the next page
                FortnoxResponse<OffersRoot> fr_next_page = await this.nox_client.Get<OffersRoot>($"offers?sent=false&limit=10&page={page}");
                await this.logger.LogError(this.blob_name, fr_next_page.error);

                // Add posts
                if (fr_next_page.model != null && fr_next_page.model.Offers != null)
                {
                    foreach (Annytab.Fortnox.Client.V3.Offer offer in fr_next_page.model.Offers)
                    {
                        fr.model.Offers.Add(offer);
                    }
                }
            }

            // Return a response
            return fr.model;

        } // End of the GetOffers method

        /// <summary>
        /// Get an offer to export
        /// </summary>
        public async Task<AnnytabDoxTradeRoot> GetOffer(string id, IDictionary<string, string> labels, CompanySettingsRoot company)
        {
            // Get the offer
            FortnoxResponse<OfferRoot> fr_offer = await this.nox_client.Get<OfferRoot>($"offers/{id}");
            await this.logger.LogError(this.blob_name, fr_offer.error);

            // Return if model or model.Offer is null
            if (fr_offer.model == null || fr_offer.model.Offer == null)
            {
                // Return null
                return null;
            }

            // Check if the offer should be exported
            bool export_offer = false;
            foreach (Label label in fr_offer.model.Offer.Labels)
            {
                if (labels.ContainsKey(label.Id) && labels[label.Id] == "a-dox-trade-v1")
                {
                    // Export the offer
                    export_offer = true;
                    break;
                }
            }

            // Return if the offer not should be exported
            if (export_offer == false)
            {
                return null;
            }

            // Get the customer
            FortnoxResponse<CustomerRoot> fr_customer = await this.nox_client.Get<CustomerRoot>($"customers/{fr_offer.model.Offer.CustomerNumber}");
            await this.logger.LogError(this.blob_name, fr_customer.error);

            // Return if modle or model.Customer is null
            if (fr_customer.model == null || fr_customer.model.Customer == null)
            {
                // Return null
                return null;
            }

            // Make sure that there is an email to the customer
            if (string.IsNullOrEmpty(fr_customer.model.Customer.Email))
            {
                await this.logger.LogError(this.blob_name, $"GetOffer: {id}, ingen e-post har angivits för kund {fr_customer.model.Customer.CustomerNumber}.");
                return null;
            }

            // Create a quotation
            AnnytabDoxTrade post = await CreateQuotation(company, fr_offer.model, fr_customer.model);

            // Return a response
            return new AnnytabDoxTradeRoot { document_type = post.document_type, document = post, email = fr_customer.model.Customer.Email, language_code = fr_offer.model.Offer.Language };

        } // End of the GetOffer method

        /// <summary>
        /// Get orders
        /// </summary>
        public async Task<OrdersRoot> GetOrders()
        {
            // Variables
            Int32 page = 1;

            // Get a page with orders
            FortnoxResponse<OrdersRoot> fr = await this.nox_client.Get<OrdersRoot>($"orders?sent=false&limit=10&page={page}");
            await this.logger.LogError(this.blob_name, fr.error);

            // Calculate the total number of pages
            Int32? total_pages = fr.model != null && fr.model.MetaInformation != null ? fr.model.MetaInformation.TotalPages : 1;

            // Loop while there is more pages to get
            while (page < total_pages)
            {
                // Increase the page number
                page += 1;

                // Get the next page
                FortnoxResponse<OrdersRoot> fr_next_page = await this.nox_client.Get<OrdersRoot>($"orders?sent=false&limit=10&page={page}");
                await this.logger.LogError(this.blob_name, fr_next_page.error);

                // Add posts
                if (fr_next_page.model != null && fr_next_page.model.Orders != null)
                {
                    foreach (Order order in fr_next_page.model.Orders)
                    {
                        fr.model.Orders.Add(order);
                    }
                }
            }

            // Return a response
            return fr.model;

        } // End of the GetOrders method

        /// <summary>
        /// Get an order to export
        /// </summary>
        public async Task<IList<AnnytabDoxTradeRoot>> GetOrder(string id, IDictionary<string, string> labels, CompanySettingsRoot company)
        {
            // Get the order
            FortnoxResponse<OrderRoot> fr_order = await this.nox_client.Get<OrderRoot>($"orders/{id}");
            await this.logger.LogError(this.blob_name, fr_order.error);

            // Return if model or model.Order is null
            if (fr_order.model == null || fr_order.model.Order == null)
            {
                // Return null
                return null;
            }

            // Check if the order should be exported
            bool export_order = false;
            bool export_purchase_orders = false;
            foreach (Label label in fr_order.model.Order.Labels)
            {
                if (labels.ContainsKey(label.Id) && labels[label.Id] == "a-dox-trade-v1")
                {
                    // Export the order
                    export_order = true;
                }
                else if (labels.ContainsKey(label.Id) && labels[label.Id] == "a-dox-trade-v1-po")
                {
                    // Export purchase orders
                    export_purchase_orders = true;
                }
            }

            // Return if nothing should be exported
            if (export_order == false && export_purchase_orders == false)
            {
                return null;
            }

            // Get the customer
            FortnoxResponse<CustomerRoot> fr_customer = await this.nox_client.Get<CustomerRoot>($"customers/{fr_order.model.Order.CustomerNumber}");
            await this.logger.LogError(this.blob_name, fr_customer.error);

            // Return if model or model.Customer is null
            if (fr_customer.model == null || fr_customer.model.Customer == null)
            {
                // Return null
                return null;
            }

            // Make sure that there is an email to the customer
            if (string.IsNullOrEmpty(fr_customer.model.Customer.Email))
            {
                await this.logger.LogError(this.blob_name, $"GetOrder: {id}, ingen e-post har angivits för kund {fr_customer.model.Customer.CustomerNumber}.");
                return null;
            }

            // Create the list to return
            IList<AnnytabDoxTradeRoot> posts = new List<AnnytabDoxTradeRoot>();

            // Create an order confirmation
            if (export_order == true)
            {
                AnnytabDoxTrade post = await CreateOrderConfirmation(company, fr_order.model, fr_customer.model);
                posts.Add(new AnnytabDoxTradeRoot { document_type = post.document_type, document = post, email = fr_customer.model.Customer.Email, language_code = fr_order.model.Order.Language });
            }

            // Create purchase orders
            if (export_purchase_orders == true)
            {
                // Create variables
                IDictionary<string, SupplierRoot> suppliers = new Dictionary<string, SupplierRoot>();
                IDictionary<string, IList<ProductRow>> supplier_rows = new Dictionary<string, IList<ProductRow>>();
                decimal? total_weight = 0M;

                // Get suppliers
                foreach (OrderRow row in fr_order.model.Order.OrderRows)
                {
                    // Get the article
                    FortnoxResponse<ArticleRoot> fr_article = await this.nox_client.Get<ArticleRoot>($"articles/{row.ArticleNumber}");
                    await this.logger.LogError(this.blob_name, fr_article.error);

                    // Make sure that the article was found
                    if (fr_article.model != null && fr_article.model.Article != null)
                    {
                        // Get the supplier
                        if (string.IsNullOrEmpty(fr_article.model.Article.SupplierNumber) == false)
                        {
                            // Check if the supplier exists
                            if (suppliers.ContainsKey(fr_article.model.Article.SupplierNumber) == false)
                            {
                                // Get the supplier
                                FortnoxResponse<SupplierRoot> fr_supplier = await this.nox_client.Get<SupplierRoot>($"suppliers/{fr_article.model.Article.SupplierNumber}");
                                await this.logger.LogError(this.blob_name, fr_supplier.error);

                                // Add the supplier
                                if (fr_supplier != null && fr_supplier.model != null)
                                {
                                    // Add the supplier
                                    suppliers.Add(fr_article.model.Article.SupplierNumber, fr_supplier.model);
                                }
                                else
                                {
                                    // Return null
                                    return null;
                                }
                            }

                            // Check if the supplier has order rows
                            if (supplier_rows.ContainsKey(fr_article.model.Article.SupplierNumber) == false && suppliers.ContainsKey(fr_article.model.Article.SupplierNumber) == true)
                            {
                                // Add the row
                                supplier_rows.Add(fr_article.model.Article.SupplierNumber, new List<ProductRow>());
                            }

                            // Add to the total weight
                            total_weight += fr_article.model.Article.Weight != null ? (fr_article.model.Article.Weight * row.OrderedQuantity) / 1000M : 0;

                            // Add the row
                            supplier_rows[fr_article.model.Article.SupplierNumber].Add(new ProductRow
                            {
                                product_code = fr_article.model.Article.ArticleNumber,
                                manufacturer_code = fr_article.model.Article.ManufacturerArticleNumber,
                                gtin = fr_article.model.Article.EAN,
                                product_name = fr_article.model.Article.Description,
                                vat_rate = row.VAT / 100,
                                quantity = row.OrderedQuantity,
                                unit_code = row.Unit,
                                unit_price = fr_article.model.Article.PurchasePrice, // 0M
                                subrows = null
                            });
                        }
                    }
                    else
                    {
                        // Return null
                        return null;
                    }
                }

                // Create a purchase order to each supplier
                foreach (KeyValuePair<string, SupplierRoot> entry in suppliers)
                {
                    // Make sure that there is an email to the supplier
                    if (string.IsNullOrEmpty(entry.Value.Supplier.Email))
                    {
                        await this.logger.LogError(this.blob_name, $"GetOrder: {id}, ingen e-post har angivits för leverantör {entry.Value.Supplier.SupplierNumber}.");
                        return null;
                    }

                    // Create a purchase order
                    AnnytabDoxTrade post = CreatePurchaseOrder(company, fr_order.model, entry.Value, supplier_rows[entry.Key], total_weight);

                    // Add the document
                    posts.Add(new AnnytabDoxTradeRoot
                    {
                        document_type = "purchase_order_" + entry.Value.Supplier.SupplierNumber,
                        document = post,
                        email = entry.Value.Supplier.Email,
                        language_code = "en"
                    });
                }
            }

            // Return a response
            return posts;

        } // End of the GetOrder method

        /// <summary>
        /// Get invoices
        /// </summary>
        public async Task<InvoicesRoot> GetInvoices()
        {
            // Variables
            Int32 page = 1;

            // Get a page with invoices
            FortnoxResponse<InvoicesRoot> fr = await this.nox_client.Get<InvoicesRoot>($"invoices?sent=false&limit=10&page={page}");
            await this.logger.LogError(this.blob_name, fr.error);

            // Calculate the total number of pages
            Int32? total_pages = fr.model != null && fr.model.MetaInformation != null ? fr.model.MetaInformation.TotalPages : 1;

            // Loop while there is more pages to get
            while (page < total_pages)
            {
                // Increase the page number
                page += 1;

                // Get the next page
                FortnoxResponse<InvoicesRoot> fr_next_page = await this.nox_client.Get<InvoicesRoot>($"invoices?sent=false&limit=10&page={page}");
                await this.logger.LogError(this.blob_name, fr_next_page.error);

                // Add posts
                if (fr_next_page.model != null && fr_next_page.model.Invoices != null)
                {
                    foreach (Invoice invoice in fr_next_page.model.Invoices)
                    {
                        fr.model.Invoices.Add(invoice);
                    }
                }
            }

            // Return a response
            return fr.model;

        } // End of the GetInvoices method

        /// <summary>
        /// Get an invoice to export
        /// </summary>
        public async Task<AnnytabDoxTradeRoot> GetInvoice(string id, IDictionary<string, string> labels, CompanySettingsRoot company)
        {
            // Get the invoice
            FortnoxResponse<InvoiceRoot> fr_invoice = await this.nox_client.Get<InvoiceRoot>($"invoices/{id}");
            await this.logger.LogError(this.blob_name, fr_invoice.error);

            // Return if model or model.Invoice is null
            if (fr_invoice.model == null || fr_invoice.model.Invoice == null)
            {
                // Return null
                return null;
            }

            // Check if the invoice should be exported
            bool export_invoice = false;
            foreach (Label label in fr_invoice.model.Invoice.Labels)
            {
                if (labels.ContainsKey(label.Id) && labels[label.Id] == "a-dox-trade-v1")
                {
                    // Export the invoice
                    export_invoice = true;
                    break;
                }
            }

            // Return if the invoice not should be exported
            if (export_invoice == false)
            {
                return null;
            }

            // Get the customer
            FortnoxResponse<CustomerRoot> fr_customer = await this.nox_client.Get<CustomerRoot>($"customers/{fr_invoice.model.Invoice.CustomerNumber}");
            await this.logger.LogError(this.blob_name, fr_customer.error);

            // Return if model or model.Customer is null
            if (fr_customer.model == null || fr_customer.model.Customer == null)
            {
                // Return null
                return null;
            }

            // Make sure that there is an email to the supplier
            if (string.IsNullOrEmpty(fr_customer.model.Customer.Email))
            {
                await this.logger.LogError(this.blob_name, $"GetInvoice: {id}, ingen e-post har angivits för kund {fr_customer.model.Customer.CustomerNumber}.");
                return null;
            }

            // Create an invoice
            AnnytabDoxTrade post = await CreateInvoice(company, fr_invoice.model, fr_customer.model);

            // Return a response
            return new AnnytabDoxTradeRoot { document_type = post.document_type, document = post, email = fr_customer.model.Customer.Email, language_code = fr_invoice.model.Invoice.Language };

        } // End of the GetInvoice method

        /// <summary>
        /// Send a file
        /// </summary>
        public async Task<bool> SendFile(AnnytabDoxTradeRoot root)
        {
            // Create a success boolean
            bool success = false;

            // Make sure that there is an email address
            if (string.IsNullOrEmpty(root.email) == true)
            {
                await this.logger.LogError(this.blob_name, $"Order: {root.document.id}, ingen e-post specificerad!");
                return success;
            }

            // Variables
            string data = JsonConvert.SerializeObject(root.document);
            string filename = $"{root.document_type}_{root.document.id}.json";
            string language_code = string.IsNullOrEmpty(root.language_code) == false ? root.language_code.ToLower() : "en";
            DoxservrResponse<FileDocument> dr_file_metadata = null;

            // Send the document
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                dr_file_metadata = await this.dox_client.Send(stream, root.email, filename, "utf-8", "Annytab Dox Trade v1", language_code, "1");
            }

            // Make sure that the file has been sent
            if (dr_file_metadata.model != null)
            {
                // Success
                success = true;
            }

            // Return a success boolean
            return success;

        } // End of the SendFile method

        /// <summary>
        /// Create a quotation
        /// </summary>
        private async Task<AnnytabDoxTrade> CreateQuotation(CompanySettingsRoot company, OfferRoot root, CustomerRoot customer_root)
        {
            // Create a Annytab Dox Trade document
            AnnytabDoxTrade post = new AnnytabDoxTrade();
            post.id = root.Offer.DocumentNumber;
            post.document_type = "quotation";
            post.issue_date = root.Offer.OfferDate;
            post.delivery_date = root.Offer.DeliveryDate;
            post.offer_expires_date = root.Offer.ExpireDate;
            post.buyer_references = new Dictionary<string, string>();
            post.buyer_references.Add("customer_id", root.Offer.CustomerNumber);
            post.buyer_references.Add("request_for_quotation_id", root.Offer.YourReferenceNumber);
            post.terms_of_delivery = root.Offer.TermsOfDelivery;
            post.terms_of_payment = root.Offer.TermsOfPayment;
            post.mode_of_delivery = root.Offer.WayOfDelivery;
            post.total_weight_kg = 0M;
            post.penalty_interest = this.nox_api_values.PenaltyInterest;
            post.currency_code = root.Offer.Currency;
            post.vat_country_code = company.CompanySettings.CountryCode;
            post.comment = root.Offer.Remarks;
            post.seller_information = GetCompanyParty(company, root.Offer.OurReference);
            post.buyer_information = new PartyInformation
            {
                person_id = root.Offer.OrganisationNumber,
                person_name = root.Offer.CustomerName,
                address_line_1 = root.Offer.Address1,
                address_line_2 = root.Offer.Address2,
                postcode = root.Offer.ZipCode,
                city_name = root.Offer.City,
                country_name = root.Offer.Country,
                contact_name = root.Offer.YourReference,
                phone_number = root.Offer.Phone1,
                email = customer_root.Customer.Email
            };
            post.delivery_information = new PartyInformation
            {
                person_name = root.Offer.DeliveryName,
                address_line_1 = root.Offer.DeliveryAddress1,
                address_line_2 = root.Offer.DeliveryAddress2,
                postcode = root.Offer.DeliveryZipCode,
                city_name = root.Offer.DeliveryCity,
                country_name = root.Offer.DeliveryCountry,
            };
            post.payment_options = GetPaymentOptions(company);
            post.product_rows = new List<ProductRow>();
            foreach (OfferRow row in root.Offer.OfferRows)
            {
                // Get the article
                FortnoxResponse<ArticleRoot> fr_article = await this.nox_client.Get<ArticleRoot>($"articles/{row.ArticleNumber}");

                // Make sure that article root and article not is null
                if (fr_article.model == null || fr_article.model.Article == null)
                {
                    fr_article.model = new ArticleRoot { Article = new Article() };
                }

                // Add to the total weight
                post.total_weight_kg += fr_article.model.Article.Weight != null ? (fr_article.model.Article.Weight * row.Quantity) / 1000M : 0;

                // Calculate the price
                decimal? price = root.Offer.VATIncluded == true ? row.Price / ((100 + row.VAT) / 100) : row.Price;
                if (row.Discount > 0M && row.DiscountType == "AMOUNT")
                {
                    if (root.Offer.VATIncluded == true)
                    {
                        decimal? discount = row.Discount / ((100 + row.VAT) / 100);
                        price = price - (discount / row.Quantity);
                    }
                    else
                    {
                        price = price - (row.Discount / row.Quantity);
                    }
                }
                else if (row.Discount > 0M && row.DiscountType == "PERCENT")
                {
                    price = price - (price * (row.Discount / 100));
                }

                // Add a product row
                post.product_rows.Add(new ProductRow
                {
                    product_code = fr_article.model.Article.ArticleNumber,
                    manufacturer_code = fr_article.model.Article.ManufacturerArticleNumber,
                    gtin = fr_article.model.Article.EAN,
                    product_name = row.Description,
                    vat_rate = row.VAT / 100,
                    quantity = row.Quantity,
                    unit_code = row.Unit,
                    unit_price = price,
                    subrows = null
                });
            }
            decimal? invoice_fee = AddInvoiceFee(root.Offer.VATIncluded, root.Offer.AdministrationFee, root.Offer.AdministrationFeeVAT, post.product_rows, root.Offer.Language);
            decimal? freight_fee = AddFreight(root.Offer.VATIncluded, root.Offer.Freight, root.Offer.FreightVAT, post.product_rows, root.Offer.Language);
            post.vat_specification = GetVatSpecification(post.product_rows);
            post.subtotal = root.Offer.Net + invoice_fee + freight_fee;
            post.vat_total = root.Offer.TotalVAT;
            post.rounding = root.Offer.RoundOff;
            post.total = root.Offer.Total;

            // Return a response
            return post;

        } // End of the CreateQuotation method

        /// <summary>
        /// Create an order confirmation
        /// </summary>
        private async Task<AnnytabDoxTrade> CreateOrderConfirmation(CompanySettingsRoot company, OrderRoot root, CustomerRoot customer_root)
        {
            // Create a Annytab Dox Trade document
            AnnytabDoxTrade post = new AnnytabDoxTrade();
            post.id = root.Order.DocumentNumber;
            post.document_type = "order_confirmation";
            post.issue_date = root.Order.OrderDate;
            post.delivery_date = root.Order.DeliveryDate;
            post.seller_references = new Dictionary<string, string>();
            post.seller_references.Add("quotation_id", root.Order.OfferReference);
            post.buyer_references = new Dictionary<string, string>();
            post.buyer_references.Add("customer_id", root.Order.CustomerNumber);
            post.buyer_references.Add("order_id", root.Order.YourOrderNumber);
            post.terms_of_delivery = root.Order.TermsOfDelivery;
            post.terms_of_payment = root.Order.TermsOfPayment;
            post.mode_of_delivery = root.Order.WayOfDelivery;
            post.total_weight_kg = 0M;
            post.penalty_interest = this.nox_api_values.PenaltyInterest;
            post.currency_code = root.Order.Currency;
            post.vat_country_code = company.CompanySettings.CountryCode;
            post.comment = root.Order.Remarks;
            post.seller_information = GetCompanyParty(company, root.Order.OurReference);
            post.buyer_information = new PartyInformation
            {
                person_id = root.Order.OrganisationNumber,
                person_name = root.Order.CustomerName,
                address_line_1 = root.Order.Address1,
                address_line_2 = root.Order.Address2,
                postcode = root.Order.ZipCode,
                city_name = root.Order.City,
                country_name = root.Order.Country,
                contact_name = root.Order.YourReference,
                phone_number = root.Order.Phone1,
                email = customer_root.Customer.Email
            };
            post.delivery_information = new PartyInformation
            {
                person_name = root.Order.DeliveryName,
                address_line_1 = root.Order.DeliveryAddress1,
                address_line_2 = root.Order.DeliveryAddress2,
                postcode = root.Order.DeliveryZipCode,
                city_name = root.Order.DeliveryCity,
                country_name = root.Order.DeliveryCountry
            };
            post.payment_options = GetPaymentOptions(company);
            post.product_rows = new List<ProductRow>();
            foreach (OrderRow row in root.Order.OrderRows)
            {
                // Get the article
                FortnoxResponse<ArticleRoot> fr_article = await this.nox_client.Get<ArticleRoot>($"articles/{row.ArticleNumber}");

                // Make sure that article root and article not is null
                if (fr_article.model == null || fr_article.model.Article == null)
                {
                    fr_article.model = new ArticleRoot { Article = new Article() };
                }

                // Add to the total weight
                post.total_weight_kg += fr_article.model.Article.Weight != null ? (fr_article.model.Article.Weight * row.OrderedQuantity) / 1000M : 0;

                // Calculate the price
                decimal? price = root.Order.VATIncluded == true ? row.Price / ((100 + row.VAT) / 100) : row.Price;
                if (row.Discount > 0M && row.DiscountType == "AMOUNT")
                {
                    if (root.Order.VATIncluded == true)
                    {
                        decimal? discount = row.Discount / ((100 + row.VAT) / 100);
                        price = price - (discount / row.OrderedQuantity);
                    }
                    else
                    {
                        price = price - (row.Discount / row.OrderedQuantity);
                    }
                }
                else if (row.Discount > 0M && row.DiscountType == "PERCENT")
                {
                    price = price - (price * (row.Discount / 100));
                }

                // Add a product row
                post.product_rows.Add(new ProductRow
                {
                    product_code = fr_article.model.Article.ArticleNumber,
                    manufacturer_code = fr_article.model.Article.ManufacturerArticleNumber,
                    gtin = fr_article.model.Article.EAN,
                    product_name = row.Description,
                    vat_rate = row.VAT / 100,
                    quantity = row.OrderedQuantity,
                    unit_code = row.Unit,
                    unit_price = price,
                    subrows = null
                });
            }
            decimal? invoice_fee = AddInvoiceFee(root.Order.VATIncluded, root.Order.AdministrationFee, root.Order.AdministrationFeeVAT, post.product_rows, root.Order.Language);
            decimal? freight_fee = AddFreight(root.Order.VATIncluded, root.Order.Freight, root.Order.FreightVAT, post.product_rows, root.Order.Language);
            post.vat_specification = GetVatSpecification(post.product_rows);
            post.subtotal = root.Order.Net + invoice_fee + freight_fee;
            post.vat_total = root.Order.TotalVAT;
            post.rounding = root.Order.RoundOff;
            post.total = root.Order.Total;

            // Return a response
            return post;

        } // End of the CreateOrderConfirmation method

        /// <summary>
        /// Create a purchase order
        /// </summary>
        private AnnytabDoxTrade CreatePurchaseOrder(CompanySettingsRoot company, OrderRoot root, SupplierRoot supplier_root, IList<ProductRow> product_rows, decimal? total_weight)
        {
            // Calculate totals
            decimal? net_sum = 0;
            decimal? vat_sum = 0;
            foreach (ProductRow row in product_rows)
            {
                net_sum += row.unit_price * row.quantity;
                vat_sum += row.unit_price * row.quantity * row.vat_rate;
            }

            // Create a Annytab Dox Trade document
            AnnytabDoxTrade post = new AnnytabDoxTrade();
            post.id = root.Order.DocumentNumber;
            post.document_type = "order";
            post.issue_date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            post.delivery_date = root.Order.DeliveryDate;
            post.seller_references = new Dictionary<string, string>();
            post.seller_references.Add("supplier_id", supplier_root.Supplier.SupplierNumber);
            post.buyer_references = new Dictionary<string, string>();
            post.buyer_references.Add("customer_id", supplier_root.Supplier.OurCustomerNumber);
            post.terms_of_delivery = root.Order.TermsOfDelivery;
            post.terms_of_payment = supplier_root.Supplier.TermsOfPayment;
            post.mode_of_delivery = root.Order.WayOfDelivery;
            post.total_weight_kg = total_weight;
            post.currency_code = supplier_root.Supplier.Currency;
            post.comment = root.Order.Remarks;
            post.seller_information = new PartyInformation
            {
                person_id = supplier_root.Supplier.OrganisationNumber,
                person_name = supplier_root.Supplier.Name,
                address_line_1 = supplier_root.Supplier.Address1,
                address_line_2 = supplier_root.Supplier.Address2,
                postcode = supplier_root.Supplier.ZipCode,
                city_name = supplier_root.Supplier.City,
                country_name = supplier_root.Supplier.Country,
                country_code = supplier_root.Supplier.CountryCode,
                contact_name = supplier_root.Supplier.YourReference,
                phone_number = supplier_root.Supplier.Phone1,
                email = supplier_root.Supplier.Email,
                vat_number = supplier_root.Supplier.VATNumber
            };
            post.buyer_information = GetCompanyParty(company, supplier_root.Supplier.OurReference);
            post.delivery_information = new PartyInformation
            {
                person_name = root.Order.DeliveryName,
                address_line_1 = root.Order.DeliveryAddress1,
                address_line_2 = root.Order.DeliveryAddress2,
                postcode = root.Order.DeliveryZipCode,
                city_name = root.Order.DeliveryCity,
                country_name = root.Order.DeliveryCountry
            };
            post.product_rows = product_rows;
            post.subtotal = net_sum;
            post.vat_total = vat_sum;
            post.rounding = 0M;
            post.total = net_sum + vat_sum;

            // Return a response
            return post;

        } // End of the CreatePurchaseOrder method

        /// <summary>
        /// Create a invoice
        /// </summary>
        private async Task<AnnytabDoxTrade> CreateInvoice(CompanySettingsRoot company, InvoiceRoot root, CustomerRoot customer_root)
        {
            // Create a Annytab Dox Trade document
            AnnytabDoxTrade post = new AnnytabDoxTrade();
            post.id = root.Invoice.DocumentNumber;
            post.document_type = root.Invoice.Credit == true ? "credit_invoice" : "invoice";
            post.payment_reference = string.IsNullOrEmpty(root.Invoice.OCR) == false ? root.Invoice.OCR : root.Invoice.DocumentNumber;
            post.issue_date = root.Invoice.InvoiceDate;
            post.due_date = root.Invoice.DueDate;
            post.delivery_date = root.Invoice.DeliveryDate;
            post.seller_references = new Dictionary<string, string>();
            post.seller_references.Add("quotation_id", root.Invoice.OfferReference);
            post.seller_references.Add("order_id", root.Invoice.OrderReference);
            post.seller_references.Add("invoice_id", root.Invoice.InvoiceReference);
            post.buyer_references = new Dictionary<string, string>();
            post.buyer_references.Add("customer_id", root.Invoice.CustomerNumber);
            post.buyer_references.Add("order_id", root.Invoice.YourOrderNumber);
            post.terms_of_delivery = root.Invoice.TermsOfDelivery;
            post.terms_of_payment = root.Invoice.TermsOfPayment;
            post.mode_of_delivery = root.Invoice.WayOfDelivery;
            post.total_weight_kg = 0M;
            post.penalty_interest = this.nox_api_values.PenaltyInterest;
            post.currency_code = root.Invoice.Currency;
            post.vat_country_code = company.CompanySettings.CountryCode;
            post.comment = root.Invoice.Remarks;
            post.seller_information = GetCompanyParty(company, root.Invoice.OurReference);
            post.buyer_information = new PartyInformation
            {
                person_id = root.Invoice.OrganisationNumber,
                person_name = root.Invoice.CustomerName,
                address_line_1 = root.Invoice.Address1,
                address_line_2 = root.Invoice.Address2,
                postcode = root.Invoice.ZipCode,
                city_name = root.Invoice.City,
                country_name = root.Invoice.Country,
                contact_name = root.Invoice.YourReference,
                phone_number = root.Invoice.Phone1,
                email = customer_root.Customer.Email
            };
            post.delivery_information = new PartyInformation
            {
                person_name = root.Invoice.DeliveryName,
                address_line_1 = root.Invoice.DeliveryAddress1,
                address_line_2 = root.Invoice.DeliveryAddress2,
                postcode = root.Invoice.DeliveryZipCode,
                city_name = root.Invoice.DeliveryCity,
                country_name = root.Invoice.DeliveryCountry
            };
            post.payment_options = GetPaymentOptions(company);
            post.product_rows = new List<ProductRow>();
            foreach (Annytab.Fortnox.Client.V3.InvoiceRow row in root.Invoice.InvoiceRows)
            {
                // Get the article
                FortnoxResponse<ArticleRoot> fr_article = await this.nox_client.Get<ArticleRoot>($"articles/{row.ArticleNumber}");

                // Make sure that article root and article not is null
                if (fr_article.model == null || fr_article.model.Article == null)
                {
                    fr_article.model = new ArticleRoot { Article = new Article() };
                }

                // Add to the total weight
                post.total_weight_kg += fr_article.model.Article.Weight != null ? (fr_article.model.Article.Weight * row.DeliveredQuantity) / 1000M : 0;

                // Calculate the price
                decimal? price = root.Invoice.VATIncluded == true ? row.Price / ((100 + row.VAT) / 100) : row.Price;
                if (row.Discount > 0M && row.DiscountType == "AMOUNT")
                {
                    if (root.Invoice.VATIncluded == true)
                    {
                        decimal? discount = row.Discount / ((100 + row.VAT) / 100);
                        price = price - (discount / row.DeliveredQuantity);
                    }
                    else
                    {
                        price = price - (row.Discount / row.DeliveredQuantity);
                    }
                }
                else if (row.Discount > 0M && row.DiscountType == "PERCENT")
                {
                    price = price - (price * (row.Discount / 100));
                }

                // Add a product row
                post.product_rows.Add(new ProductRow
                {
                    product_code = fr_article.model.Article.ArticleNumber,
                    manufacturer_code = fr_article.model.Article.ManufacturerArticleNumber,
                    gtin = fr_article.model.Article.EAN,
                    product_name = row.Description,
                    vat_rate = row.VAT / 100,
                    quantity = row.DeliveredQuantity,
                    unit_code = row.Unit,
                    unit_price = price,
                    subrows = null
                });
            }
            decimal? invoice_fee = AddInvoiceFee(root.Invoice.VATIncluded, root.Invoice.AdministrationFee, root.Invoice.AdministrationFeeVAT, post.product_rows, root.Invoice.Language);
            decimal? freight_fee = AddFreight(root.Invoice.VATIncluded, root.Invoice.Freight, root.Invoice.FreightVAT, post.product_rows, root.Invoice.Language);
            post.vat_specification = GetVatSpecification(post.product_rows);
            post.subtotal = root.Invoice.Net + invoice_fee + freight_fee;
            post.vat_total = root.Invoice.TotalVAT;
            post.rounding = root.Invoice.RoundOff;
            post.total = root.Invoice.Total;
            post.paid_amount = root.Invoice.TotalToPay - root.Invoice.Balance;
            post.balance_due = root.Invoice.Balance;

            // Return a response
            return post;

        } // End of the CreateInvoice method

        /// <summary>
        /// Add an invoice fee
        /// </summary>
        private decimal? AddInvoiceFee(bool? vat_included, decimal? fee, decimal? vat, IList<ProductRow> rows, string language)
        {
            // Create the decimal to return
            decimal? invoice_fee = 0M;

            // Make sure that fee not is null and different from 0
            if (fee != null && fee != 0M)
            {
                // Calculate the price
                invoice_fee = vat_included == true ? fee - vat : fee;

                // Add a product row
                rows.Add(new ProductRow
                {
                    product_code = null,
                    product_name = string.IsNullOrEmpty(language) == false && String.Equals(language, "SV", StringComparison.OrdinalIgnoreCase) ? "Fakturaavgift" : "Invoice fee",
                    vat_rate = vat / invoice_fee,
                    quantity = invoice_fee < 0M ? -1M : 1M,
                    unit_code = null,
                    unit_price = invoice_fee < 0M ? invoice_fee * -1 : invoice_fee,
                    subrows = null
                });
            }

            // Return a fee
            return invoice_fee;

        } // End of the AddInvoiceFee method

        /// <summary>
        /// Add freight
        /// </summary>
        private decimal? AddFreight(bool? vat_included, decimal? fee, decimal? vat, IList<ProductRow> rows, string language)
        {
            // Create the decimal to return
            decimal? freight = 0M;

            // Make sure that fee not is null and different from 0
            if (fee != null && fee != 0M)
            {
                // Calculate the price
                freight = vat_included == true ? fee - vat : fee;

                // Add a product row
                rows.Add(new ProductRow
                {
                    product_code = null,
                    product_name = string.IsNullOrEmpty(language) == false && String.Equals(language, "SV", StringComparison.OrdinalIgnoreCase) ? "Frakt" : "Freight",
                    vat_rate = vat / freight,
                    quantity = freight < 0M ? -1M : 1M,
                    unit_code = null,
                    unit_price = freight < 0M ? freight * -1 : freight,
                    subrows = null
                });
            }

            // Return a freight
            return freight;

        } // End of the AddFreight method

        /// <summary>
        /// Get a party with company information
        /// </summary>
        private PartyInformation GetCompanyParty(CompanySettingsRoot company, string reference)
        {
            // Return a party
            return new PartyInformation()
            {
                person_id = company.CompanySettings.OrganizationNumber,
                person_name = company.CompanySettings.Name,
                address_line_1 = company.CompanySettings.Address,
                postcode = company.CompanySettings.ZipCode,
                city_name = company.CompanySettings.City,
                country_name = company.CompanySettings.Country,
                country_code = company.CompanySettings.CountryCode,
                contact_name = reference,
                phone_number = company.CompanySettings.Phone1,
                email = company.CompanySettings.Email,
                vat_number = company.CompanySettings.VATNumber
            };

        } // End of the GetCompanyParty method

        /// <summary>
        /// Get payment options
        /// </summary>
        private IList<PaymentOption> GetPaymentOptions(CompanySettingsRoot company)
        {
            // Create the list to return
            IList<PaymentOption> payment_options = new List<PaymentOption>();

            // Add payment options
            if (string.IsNullOrEmpty(company.CompanySettings.IBAN) == false)
            {
                payment_options.Add(new PaymentOption
                {
                    name = "IBAN",
                    account_reference = company.CompanySettings.IBAN,
                    bank_identifier_code = company.CompanySettings.BIC
                });
            }
            if (string.IsNullOrEmpty(company.CompanySettings.BG) == false)
            {
                payment_options.Add(new PaymentOption
                {
                    name = "BG",
                    account_reference = company.CompanySettings.BG,
                    bank_identifier_code = "BGABSESS",
                    bank_name = "Bankgirocentralen BGC AB",
                    bank_country_code = "SE"
                });
            }
            if (string.IsNullOrEmpty(company.CompanySettings.PG) == false)
            {
                payment_options.Add(new PaymentOption
                {
                    name = "PG",
                    account_reference = company.CompanySettings.PG,
                    bank_identifier_code = "NDEASESS",
                    bank_name = "Nordea Bank AB",
                    bank_country_code = "SE"
                });
            }

            // Return a list
            return payment_options;

        } // End of the GetPaymentOptions method

        #endregion

        #region Helpers

        /// <summary>
        /// Get the sender party
        /// </summary>
        public Party GetSender(IList<Party> parties)
        {
            // Create the party to return
            Party party = null;

            // Loop parties
            foreach (Party p in parties)
            {
                // Check if the party is the sender
                if (p.is_sender == 1)
                {
                    party = p;
                    break;
                }
            }

            // Return a party
            return party;

        } // End of the GetSender method

        /// <summary>
        /// Get a party by email
        /// </summary>
        public Party GetParty(string email, IList<Party> parties)
        {
            // Create the party to return
            Party party = null;

            // Loop parties
            foreach (Party p in parties)
            {
                // Check for an email match
                if (p.email == email)
                {
                    party = p;
                    break;
                }
            }

            // Return a party
            return party;

        } // End of the GetParty method

        /// <summary>
        /// Get customer vat type
        /// </summary>
        private string GetCustomerVatType(Customer customer)
        {
            // Customer country codes
            string invoice_country_code = string.IsNullOrEmpty(customer.CountryCode) == false ? customer.CountryCode : "SE";
            string delivery_country_code = string.IsNullOrEmpty(customer.DeliveryCountryCode) == false ? customer.DeliveryCountryCode : invoice_country_code;

            // Create the vat type to return
            string vat_type = "";

            if (invoice_country_code == "SE" && delivery_country_code == "SE")
            {
                if (customer.VATType == "SEREVERSEDVAT")
                {
                    vat_type = "SEREVERSEDVAT";
                }
                else if (customer.VATType == "SEVAT")
                {
                    vat_type = "SEVAT";
                }
                else
                {
                    vat_type = this.nox_api_values.SalesVatTypeSE;
                }
            }
            else if (customer.VATNumber != null && IsCountryCodeEU(invoice_country_code) && IsCountryCodeEU(delivery_country_code))
            {
                vat_type = "EUREVERSEDVAT";
            }
            else if (customer.VATNumber == null && IsCountryCodeEU(invoice_country_code) && IsCountryCodeEU(delivery_country_code))
            {
                vat_type = "EUVAT";
            }
            else
            {
                vat_type = "EXPORT";
            }

            // Return a vat type
            return vat_type;

        } // End of the GetCustomerVatType method

        /// <summary>
        /// Get a default sales account for an article
        /// </summary>
        private string GetArticleSalesAccount(decimal? vat_rate)
        {
            // Create the account to return
            string account = this.nox_api_values.SalesAccountSE0;

            if (vat_rate == 0.25M)
            {
                account = this.nox_api_values.SalesAccountSE25;
            }
            else if (vat_rate == 0.12M)
            {
                account = this.nox_api_values.SalesAccountSE12;
            }
            else if (vat_rate == 0.06M)
            {
                account = this.nox_api_values.SalesAccountSE6;
            }

            // Return an account
            return account;

        } // End of the GetArticleSalesAccount method

        /// <summary>
        /// Check if the country code represents a EU-country
        /// </summary>
        private bool IsCountryCodeEU(string country_code)
        {
            if (country_code == "BE") { return true; }
            if (country_code == "BG") { return true; }
            if (country_code == "CZ") { return true; }
            if (country_code == "DK") { return true; }
            if (country_code == "DE") { return true; }
            if (country_code == "EE") { return true; }
            if (country_code == "IE") { return true; }
            if (country_code == "EL") { return true; }
            if (country_code == "ES") { return true; }
            if (country_code == "FR") { return true; }
            if (country_code == "HR") { return true; }
            if (country_code == "IT") { return true; }
            if (country_code == "CY") { return true; }
            if (country_code == "LV") { return true; }
            if (country_code == "LT") { return true; }
            if (country_code == "LU") { return true; }
            if (country_code == "HU") { return true; }
            if (country_code == "MT") { return true; }
            if (country_code == "NL") { return true; }
            if (country_code == "AT") { return true; }
            if (country_code == "PL") { return true; }
            if (country_code == "PT") { return true; }
            if (country_code == "RO") { return true; }
            if (country_code == "SI") { return true; }
            if (country_code == "SK") { return true; }
            if (country_code == "FI") { return true; }
            if (country_code == "SE") { return true; }
            if (country_code == "UK") { return true; }
            else { return false; }

        } // End of the IsCountryCodeEU method

        /// <summary>
        /// Convert a word to alpha numeric characters
        /// </summary>
        private string ConvertToAlphanumeric(string word)
        {
            // Turn the word into latin characters
            word = word.Unidecode();

            // Modify the word
            word = word.Replace("å", "a");
            word = word.Replace("ä", "a");
            word = word.Replace("ö", "o");
            word = word.Replace("à", "a");
            word = word.Replace("á", "a");
            word = word.Replace("é", "e");
            word = word.Replace("Å", "A");
            word = word.Replace("Ä", "A");
            word = word.Replace("Ö", "O");
            word = word.Replace("À", "A");
            word = word.Replace("Á", "A");
            word = word.Replace("É", "E");
            word = Regex.Replace(word, "[^0-9a-zA-Z-]+", "-");

            // Return a word
            return word;

        } // End of the ConvertToAlphanumeric method

        /// <summary>
        /// Get the encoding from a charset string
        /// </summary>
        private Encoding GetEncoding(string charset, Encoding fallback_encoding)
        {
            // Create the encoding to return
            Encoding encoding = fallback_encoding;

            // Convert the charset to lower case
            charset = charset.ToLower();

            if (charset == "ascii")
            {
                encoding = Encoding.ASCII;
            }
            else if (charset == "utf-8")
            {
                encoding = Encoding.UTF8;
            }
            else if (charset == "utf-16")
            {
                encoding = Encoding.Unicode;
            }
            else if (charset == "utf-32")
            {
                encoding = Encoding.UTF32;
            }

            // Return an encoding
            return encoding;

        } // End of the GetEncoding method

        /// <summary>
        /// Get a vat specification from product rows
        /// </summary>
        private IList<VatSpecification> GetVatSpecification(IList<ProductRow> rows)
        {
            // Create the list to return
            IList<VatSpecification> vat_specification = new List<VatSpecification>();

            // Create a sorted dictionary
            SortedDictionary<decimal?, VatSpecification> vat_rows = new SortedDictionary<decimal?, VatSpecification>();

            // Loop product rows
            foreach (ProductRow row in rows)
            {
                // Calculate sums
                decimal? row_sum = row.unit_price * row.quantity;
                decimal? vat_sum = row_sum * row.vat_rate;

                // Add the vat to the dictionary
                if (vat_rows.ContainsKey(row.vat_rate) == true)
                {
                    VatSpecification vs = vat_rows[row.vat_rate];
                    vs.taxable_amount += row_sum;
                    vs.tax_amount += vat_sum;
                }
                else
                {
                    vat_rows.Add(row.vat_rate, new VatSpecification
                    {
                        tax_rate = row.vat_rate,
                        taxable_amount = row.unit_price * row.quantity,
                        tax_amount = row.unit_price * row.quantity * row.vat_rate
                    });
                }
            }

            // Add vat specifications to the list
            foreach (KeyValuePair<decimal?, VatSpecification> row in vat_rows)
            {
                vat_specification.Add(row.Value);
            }

            // Return a list
            return vat_specification;

        } // End of the GetVatSpecification method

        #endregion

    } // End of the class

} // End of the namespace