using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Annytab.Fortnox.Client.V3;
using Annytab.Dox.Standards.V1;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This class handles exports from fortnox
    /// </summary>
    public class FortnoxExporter : IFortnoxExporter
    {
        #region Variables

        private readonly ILogger logger;
        private readonly IFortnoxClient nox_client;
        private readonly DefaultValues default_values;
        private CompanySettingsRoot _company_settings;
        private Dictionary<string, string> _labels;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new fortnox exporter
        /// </summary>
        public FortnoxExporter(ILogger<IFortnoxExporter> logger, IFortnoxClient nox_client, IOptions<DefaultValues> default_values)
        {
            // Set values for instance variables
            this.logger = logger;
            this.nox_client = nox_client;
            this.default_values = default_values.Value;
            this._company_settings = null;
            this._labels = null;

        } // End of the constructor

        #endregion

        #region Get methods

        /// <summary>
        /// Get labels
        /// </summary>
        public async Task<Dictionary<string, string>> GetLabels()
        {
            // Return labels if they already exists
            if (this._labels != null)
            {
                return this._labels;
            }

            // Get labels
            FortnoxResponse<LabelsRoot> fr = await this.nox_client.Get<LabelsRoot>("labels");

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

            // Make sure that root and root.Labels not is null
            if (fr.model == null || fr.model.Labels == null)
            {
                return new Dictionary<string, string>();
            }

            // Create a dictionary
            this._labels = new Dictionary<string, string>();

            // Loop the list
            foreach(Label label in fr.model.Labels)
            {
                this._labels.Add(label.Id, label.Description);
            }

            // Return a reference to labels
            return this._labels;

        } // End of the GetLabels method

        /// <summary>
        /// Get company settings
        /// </summary>
        public async Task<CompanySettingsRoot> GetCompanySettings()
        {
            // Return company settings if they already exists
            if(this._company_settings != null)
            {
                return this._company_settings;
            }

            // Get company settings
            FortnoxResponse<CompanySettingsRoot> fr = await this.nox_client.Get<CompanySettingsRoot>("settings/company");

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

            // Create a reference to company settings
            this._company_settings = fr.model;

            // Return company settings
            return this._company_settings;

        } // End of the GetCompanySettings method

        #endregion

        #region Offer methods

        /// <summary>
        /// Get offers
        /// </summary>
        public async Task<OffersRoot> GetOffers()
        {
            // Variables
            Int32 page = 1;

            // Get a page with offers
            FortnoxResponse<OffersRoot> fr = await this.nox_client.Get<OffersRoot>($"offers?sent=false&limit=10&page={page}");

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

            // Calculate the total number of pages
            Int32? total_pages = fr.model != null && fr.model.MetaInformation != null ? fr.model.MetaInformation.TotalPages : 1;

            // Loop while there is more pages to get
            while (page < total_pages)
            {
                // Increase the page number
                page += 1;

                // Get the next page
                FortnoxResponse<OffersRoot> fr_next_page = await this.nox_client.Get<OffersRoot>($"offers?sent=false&limit=10&page={page}");

                // Log errors
                if (string.IsNullOrEmpty(fr_next_page.error) == false)
                {
                    this.logger.LogError(fr_next_page.error);
                }

                // Add posts
                if (fr_next_page.model != null && fr_next_page.model.Offers != null)
                {
                    foreach (Offer offer in fr_next_page.model.Offers)
                    {
                        fr.model.Offers.Add(offer);
                    }
                }
            }

            // Return the model
            return fr.model;

        } // End of the GetOffers method

        /// <summary>
        /// Get an offer to export
        /// </summary>
        public async Task<AnnytabDoxTradeRoot> GetOffer(string id)
        {
            // Get data
            Dictionary<string, string> labels = await GetLabels();
            CompanySettingsRoot company = await GetCompanySettings();

            // Make sure that company and company.Settings not is null
            if(company == null || company.CompanySettings == null)
            {
                this.logger.LogError($"GetOffer: {id}, Could not find any company settings.");
                return null;
            }

            // Get the offer
            FortnoxResponse<OfferRoot> fr = await this.nox_client.Get<OfferRoot>($"offers/{id}");

            // Return if model or model.Offer is null
            if (fr.model == null || fr.model.Offer == null)
            {
                // Log the error and return null
                this.logger.LogError(fr.error);
                return null;
            }

            // Check if the offer should be exported
            bool export_offer = false;
            foreach(Label label in fr.model.Offer.Labels)
            {
                if(labels.ContainsKey(label.Id) && labels[label.Id] == "a-dox-trade-v1")
                {
                    // Export the offer
                    export_offer = true;
                    break;
                }
            }

            // Return null if the offer not should be exported
            if(export_offer == false)
            {
                return null;
            }

            // Get the customer
            FortnoxResponse<CustomerRoot> fr_customer = await this.nox_client.Get<CustomerRoot>($"customers/{fr.model.Offer.CustomerNumber}");

            // Return if modle or model.Customer is null
            if(fr_customer.model == null || fr_customer.model.Customer == null)
            {
                // Log the error and return null
                this.logger.LogError(fr_customer.error);
                return null;
            }

            // Create a quotation
            AnnytabDoxTrade post = await CreateQuotation(company, fr.model, fr_customer.model);
 
            // Return the post
            return new AnnytabDoxTradeRoot { document_type = post.document_type, document = post, email = fr_customer.model.Customer.Email, language_code = fr.model.Offer.Language };

        } // End of the GetOffer method

        #endregion

        #region Orders

        /// <summary>
        /// Get orders
        /// </summary>
        public async Task<OrdersRoot> GetOrders()
        {
            // Variables
            Int32 page = 1;

            // Get a page with orders
            FortnoxResponse<OrdersRoot> fr = await this.nox_client.Get<OrdersRoot>($"orders?sent=false&limit=10&page={page}");

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

            // Calculate the total number of pages
            Int32? total_pages = fr.model != null && fr.model.MetaInformation != null ? fr.model.MetaInformation.TotalPages : 1;

            // Loop while there is more pages to get
            while (page < total_pages)
            {
                // Increase the page number
                page += 1;

                // Get the next page
                FortnoxResponse<OrdersRoot> fr_next_page = await this.nox_client.Get<OrdersRoot>($"orders?sent=false&limit=10&page={page}");

                // Log errors
                if (string.IsNullOrEmpty(fr_next_page.error) == false)
                {
                    this.logger.LogError(fr_next_page.error);
                }

                // Add posts
                if (fr_next_page.model != null && fr_next_page.model.Orders != null)
                {
                    foreach (Order order in fr_next_page.model.Orders)
                    {
                        fr.model.Orders.Add(order);
                    }
                }
            }

            // Return the model
            return fr.model;

        } // End of the GetOrders method

        /// <summary>
        /// Get an order to export
        /// </summary>
        public async Task<IList<AnnytabDoxTradeRoot>> GetOrder(string id)
        {
            // Get data
            Dictionary<string, string> labels = await GetLabels();
            CompanySettingsRoot company = await GetCompanySettings();

            // Make sure that company and company.Settings not is null
            if (company == null || company.CompanySettings == null)
            {
                this.logger.LogError($"GetOrder: {id}, Could not find any company settings.");
                return null;
            }

            // Get the order
            FortnoxResponse<OrderRoot> fr = await this.nox_client.Get<OrderRoot>($"orders/{id}");

            // Return if model or model.Order is null
            if (fr.model == null || fr.model.Order == null)
            {
                // Log the error and return null
                this.logger.LogError(fr.error);
                return null;
            }

            // Check if the order should be exported
            bool export_order = false;
            bool export_purchase_orders = false;
            foreach (Label label in fr.model.Order.Labels)
            {
                if (labels.ContainsKey(label.Id) && labels[label.Id] == "a-dox-trade-v1")
                {
                    // Export the order
                    export_order = true;
                }
                else if (labels.ContainsKey(label.Id) && labels[label.Id] == "a-dox-trade-v1-po")
                {
                    // Export the purchase order
                    export_purchase_orders = true;
                }
            }

            // Return null if nothing should be exported
            if (export_order == false && export_purchase_orders == false)
            {
                return null;
            }

            // Get the customer
            FortnoxResponse<CustomerRoot> fr_customer = await this.nox_client.Get<CustomerRoot>($"customers/{fr.model.Order.CustomerNumber}");

            // Return if model or model.Customer is null
            if (fr_customer.model == null || fr_customer.model.Customer == null)
            {
                // Log the error and return null
                this.logger.LogError(fr_customer.error);
                return null;
            }

            // Create the list to return
            IList<AnnytabDoxTradeRoot> posts = new List<AnnytabDoxTradeRoot>();

            // Create an order confirmation
            if(export_order == true)
            {
                AnnytabDoxTrade post = await CreateOrderConfirmation(company, fr.model, fr_customer.model);
                posts.Add(new AnnytabDoxTradeRoot { document_type = post.document_type, document = post, email = fr_customer.model.Customer.Email, language_code = fr.model.Order.Language });
            }

            // Create purchase orders
            if(export_purchase_orders == true)
            {
                // Create variables
                IDictionary<string, SupplierRoot> suppliers = new Dictionary<string, SupplierRoot>();
                IDictionary<string, IList<ProductRow>> supplier_rows = new Dictionary<string, IList<ProductRow>>();
                decimal? total_weight = 0M;

                // Get suppliers
                foreach (OrderRow row in fr.model.Order.OrderRows)
                {
                    // Get the article
                    FortnoxResponse<ArticleRoot> fr_article = await this.nox_client.Get<ArticleRoot>($"articles/{row.ArticleNumber}");

                    // Make sure that the article was found
                    if (fr_article.model != null && fr_article.model.Article != null)
                    {
                        // Get the supplier
                        if(string.IsNullOrEmpty(fr_article.model.Article.SupplierNumber) == false)
                        {
                            // Check if the supplier exists
                            if (suppliers.ContainsKey(fr_article.model.Article.SupplierNumber) == false)
                            {
                                // Get the supplier
                                FortnoxResponse<SupplierRoot> fr_supplier = await this.nox_client.Get<SupplierRoot>($"suppliers/{fr_article.model.Article.SupplierNumber}");

                                // Add the supplier
                                if(fr_supplier != null && fr_supplier.model != null)
                                {
                                    // Add the supplier
                                    suppliers.Add(fr_article.model.Article.SupplierNumber, fr_supplier.model);
                                }
                                else
                                {
                                    this.logger.LogError(fr_supplier.error);
                                }
                            }

                            // Check if the supplier has order rows
                            if(supplier_rows.ContainsKey(fr_article.model.Article.SupplierNumber) == false && suppliers.ContainsKey(fr_article.model.Article.SupplierNumber) == true)
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
                        // Log the error
                        this.logger.LogError(fr_article.error);
                    }
                }

                // Create a purchase order to each supplier
                foreach(KeyValuePair<string, SupplierRoot> entry in suppliers)
                {
                    // Create a purchase order
                    AnnytabDoxTrade post = CreatePurchaseOrder(company, fr.model, entry.Value, supplier_rows[entry.Key], total_weight);

                    // Add the document
                    posts.Add(new AnnytabDoxTradeRoot { document_type = "purchase_order_" + entry.Value.Supplier.SupplierNumber, document = post,
                        email = entry.Value.Supplier.Email, language_code = "en" });
                }
            }

            // Return the list with posts
            return posts;

        } // End of the GetOrder method

        #endregion

        #region Invoices

        /// <summary>
        /// Get invoices
        /// </summary>
        public async Task<InvoicesRoot> GetInvoices()
        {
            // Variables
            Int32 page = 1;

            // Get a page with invoices
            FortnoxResponse<InvoicesRoot> fr = await this.nox_client.Get<InvoicesRoot>($"invoices?sent=false&limit=10&page={page}");

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

            // Calculate the total number of pages
            Int32? total_pages = fr.model != null && fr.model.MetaInformation != null ? fr.model.MetaInformation.TotalPages : 1;

            // Loop while there is more pages to get
            while (page < total_pages)
            {
                // Increase the page number
                page += 1;

                // Get the next page
                FortnoxResponse<InvoicesRoot> fr_next_page = await this.nox_client.Get<InvoicesRoot>($"invoices?sent=false&limit=10&page={page}");

                // Log errors
                if (string.IsNullOrEmpty(fr_next_page.error) == false)
                {
                    this.logger.LogError(fr_next_page.error);
                }

                // Add posts
                if (fr_next_page.model != null && fr_next_page.model.Invoices != null)
                {
                    foreach (Invoice invoice in fr_next_page.model.Invoices)
                    {
                        fr.model.Invoices.Add(invoice);
                    }
                }
            }

            // Return the root post
            return fr.model;

        } // End of the GetInvoices method

        /// <summary>
        /// Get an invoice to export
        /// </summary>
        public async Task<AnnytabDoxTradeRoot> GetInvoice(string id)
        {
            // Get data
            Dictionary<string, string> labels = await GetLabels();
            CompanySettingsRoot company = await GetCompanySettings();

            // Make sure that company and company.Settings not is null
            if (company == null || company.CompanySettings == null)
            {
                this.logger.LogError($"GetInvoice: {id}, Could not find any company settings.");
                return null;
            }

            // Get the invoice
            FortnoxResponse<InvoiceRoot> fr = await this.nox_client.Get<InvoiceRoot>($"invoices/{id}");

            // Return if model or model.Invoice is null
            if (fr.model == null || fr.model.Invoice == null)
            {
                // Log the error and return null
                this.logger.LogError(fr.error);
                return null;
            }

            // Check if the invoice should be exported
            bool export_invoice = false;
            foreach (Label label in fr.model.Invoice.Labels)
            {
                if (labels.ContainsKey(label.Id) && labels[label.Id] == "a-dox-trade-v1")
                {
                    // Export the invoice
                    export_invoice = true;
                    break;
                }
            }

            // Return null if the invoice not should be exported
            if (export_invoice == false)
            {
                return null;
            }

            // Get the customer
            FortnoxResponse<CustomerRoot> fr_customer = await this.nox_client.Get<CustomerRoot>($"customers/{fr.model.Invoice.CustomerNumber}");

            // Return if model or model.Customer is null
            if (fr_customer.model == null || fr_customer.model.Customer == null)
            {
                // Log the error and return null
                this.logger.LogError(fr_customer.error);
                return null;
            }

            // Create an invoice
            AnnytabDoxTrade post = await CreateInvoice(company, fr.model, fr_customer.model);

            // Return the post
            return new AnnytabDoxTradeRoot { document_type = post.document_type, document = post, email = fr_customer.model.Customer.Email, language_code = fr.model.Invoice.Language };

        } // End of the GetInvoice method

        #endregion

        #region Helper methods

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
            post.penalty_interest = this.default_values.PenaltyInterest;
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
                post.total_weight_kg += fr_article.model.Article.Weight != null ? (fr_article.model.Article.Weight * row.Quantity)/ 1000M : 0;

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
            post.vat_specification = CommonTools.GetVatSpecification(post.product_rows);
            post.subtotal = root.Offer.Net + invoice_fee + freight_fee;
            post.vat_total = root.Offer.TotalVAT;
            post.rounding = root.Offer.RoundOff;
            post.total = root.Offer.Total;

            // Return the post
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
            post.penalty_interest = this.default_values.PenaltyInterest;
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
            post.vat_specification = CommonTools.GetVatSpecification(post.product_rows);
            post.subtotal = root.Order.Net + invoice_fee + freight_fee;
            post.vat_total = root.Order.TotalVAT;
            post.rounding = root.Order.RoundOff;
            post.total = root.Order.Total;

            // Return the post
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

            // Return the post
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
            post.penalty_interest = this.default_values.PenaltyInterest;
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
            foreach (InvoiceRow row in root.Invoice.InvoiceRows)
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
            post.vat_specification = CommonTools.GetVatSpecification(post.product_rows);
            post.subtotal = root.Invoice.Net + invoice_fee + freight_fee;
            post.vat_total = root.Invoice.TotalVAT;
            post.rounding = root.Invoice.RoundOff;
            post.total = root.Invoice.Total;
            post.paid_amount = root.Invoice.TotalToPay - root.Invoice.Balance;
            post.balance_due = root.Invoice.Balance;

            // Return the post
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

            // Return the fee
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

            // Return the fee
            return freight;

        } // End of the AddFreight method

        /// <summary>
        /// Get a party with company information
        /// </summary>
        private PartyInformation GetCompanyParty(CompanySettingsRoot company, string reference)
        {
            // Return the party
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

            // Return the list
            return payment_options;

        } // End of the GetPaymentOptions method

        #endregion

    } // End of the class

} // End of the namespace