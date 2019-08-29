using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Annytab.Dox.Standards.V1;
using Annytab.Fortnox.Client.V3;

namespace Annytab.Doxservr.Fortnox.App
{
    /// <summary>
    /// This class handles imports to fortnox
    /// </summary>
    public class FortnoxImporter : IFortnoxImporter
    {
        #region Variables

        private readonly ILogger logger;
        private readonly IFortnoxClient nox_client;
        private readonly DefaultValues default_values;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new fortnox importer
        /// </summary>
        public FortnoxImporter(ILogger<IFortnoxImporter> logger, IFortnoxClient nox_client, IOptions<DefaultValues> default_values)
        {
            // Set values for instance variables
            this.logger = logger;
            this.nox_client = nox_client;
            this.default_values = default_values.Value;

        } // End of the constructor

        #endregion

        #region Registers

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

            // Log errors
            if(string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

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

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }
            }

            // Return the post
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

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

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

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }
            }

            // Return the post
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

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

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

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }
            }

            // Return the post
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

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

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

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }
            }

            // Return the post
            return fr.model;

        } // End of the AddCurrency method

        /// <summary>
        /// Add a unit if it does not exists
        /// </summary>
        public async Task<UnitRoot> AddUnit(string unit_code)
        {
            // Get the root
            FortnoxResponse<UnitRoot> fr = await this.nox_client.Get<UnitRoot>($"units/{unit_code}");

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

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

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }
            }

            // Return the post
            return fr.model;

        } // End of the AddUnit method

        /// <summary>
        /// Add a price list if it does not exists
        /// </summary>
        public async Task<PriceListRoot> AddPriceList(string code)
        {
            // Get the root
            FortnoxResponse<PriceListRoot> fr = await this.nox_client.Get<PriceListRoot>($"pricelists/{code}");

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

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

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }
            }

            // Return the post
            return fr.model;

        } // End of the AddPriceList method

        /// <summary>
        /// Add an account if it does not exist
        /// </summary>
        public async Task<AccountRoot> AddAccount(string account_number)
        {
            // Get the root
            FortnoxResponse<AccountRoot> fr = await this.nox_client.Get<AccountRoot>($"accounts/{account_number}");

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

            // Add the account if it does not exist
            if (fr.model == null)
            {
                // Create a new post
                fr.model = new AccountRoot
                {
                    Account = new Account
                    {
                        Number = account_number,
                        Description = account_number
                    }
                };

                // Add an account
                fr = await this.nox_client.Add<AccountRoot>(fr.model, $"accounts");

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }
            }

            // Return the post
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
            row.product_code = string.IsNullOrEmpty(row.product_code) == false ? CommonTools.ConvertToAlphanumeric(row.product_code) : null;

            // Find the article
            if (string.IsNullOrEmpty(row.gtin) == false)
            {
                // Try to get articles on EAN
                FortnoxResponse<ArticlesRoot> fr_page = await this.nox_client.Get<ArticlesRoot>($"articles?ean={row.gtin}");

                // Log errors
                if (string.IsNullOrEmpty(fr_page.error) == false)
                {
                    this.logger.LogError(fr_page.error);
                }

                // Make sure that at least one article was found
                if (fr_page.model != null && fr_page.model.Articles != null && fr_page.model.Articles.Count > 0)
                {
                    // Get an article
                    fr = await this.nox_client.Get<ArticleRoot>($"articles/{fr_page.model.Articles[0].ArticleNumber}");

                    // Log errors
                    if (string.IsNullOrEmpty(fr.error) == false)
                    {
                        this.logger.LogError(fr.error);
                    }
                }
            }
            if(fr.model == null && string.IsNullOrEmpty(row.manufacturer_code) == false)
            {
                // Try to get articles on manufacturer code
                FortnoxResponse<ArticlesRoot> fr_page = await this.nox_client.Get<ArticlesRoot>($"articles?manufacturerarticlenumber={row.manufacturer_code}");

                // Log errors
                if (string.IsNullOrEmpty(fr_page.error) == false)
                {
                    this.logger.LogError(fr_page.error);
                }

                // Make sure that at least one article was found
                if (fr_page.model != null && fr_page.model.Articles != null && fr_page.model.Articles.Count > 0)
                {
                    // Get an article
                    fr = await this.nox_client.Get<ArticleRoot>($"articles/{fr_page.model.Articles[0].ArticleNumber}");

                    // Log errors
                    if (string.IsNullOrEmpty(fr.error) == false)
                    {
                        this.logger.LogError(fr.error);
                    }
                }
            }
            if(fr.model == null && string.IsNullOrEmpty(row.product_code) == false)
            {
                // Get an article
                fr = await this.nox_client.Get<ArticleRoot>($"articles/{row.product_code}");

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }
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
                        ConstructionAccount = this.default_values.SalesAccountSEREVERSEDVAT,
                        Description = row.product_name,
                        EAN = string.IsNullOrEmpty(row.gtin) == false ? row.gtin : null,
                        EUAccount = this.default_values.SalesAccountEUREVERSEDVAT,
                        EUVATAccount = this.default_values.SalesAccountEUVAT,
                        ExportAccount = this.default_values.SalesAccountEXPORT,
                        ManufacturerArticleNumber = string.IsNullOrEmpty(row.manufacturer_code) == false ? row.manufacturer_code : null,
                        PurchaseAccount = this.default_values.PurchaseAccount,
                        SalesAccount = CommonTools.GetArticleSalesAccount(row.vat_rate, this.default_values),
                        Unit = string.IsNullOrEmpty(row.unit_code) == false ? row.unit_code : null
                    }
                };

                // Add an article
                fr = await this.nox_client.Add<ArticleRoot>(fr.model, "articles");

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }

                // Add a default price
                if (fr.model != null)
                {
                    PriceRoot price = new PriceRoot
                    {
                        Price = new Price
                        {
                            ArticleNumber = fr.model.Article.ArticleNumber,
                            PriceList = this.default_values.PriceList,
                            FromQuantity = 0,
                            Amount = row.unit_price
                        }
                    };

                    // Add a price
                    FortnoxResponse<PriceRoot> fr_price = await this.nox_client.Add<PriceRoot>(price, "prices");

                    // Log errors
                    if (string.IsNullOrEmpty(fr_price.error) == false)
                    {
                        this.logger.LogError(fr_price.error);
                    }
                }     
            }

            // Return the post
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

            // Log errors
            if (string.IsNullOrEmpty(fr_page.error) == false)
            {
                this.logger.LogError(fr_page.error);
            }

            // Make sure that at least one customer was found
            if (fr_page.model != null && fr_page.model.Customers != null && fr_page.model.Customers.Count > 0)
            {
                // Get a customer
                fr = await this.nox_client.Get<CustomerRoot>($"customers/{fr_page.model.Customers[0].CustomerNumber}");

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }
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
            if(doc.buyer_information != null)
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
            if(doc.delivery_information != null)
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
            fr.model.Customer.VATType = CommonTools.GetCustomerVatType(fr.model.Customer, this.default_values);
            fr.model.Customer.WayOfDelivery = string.IsNullOrEmpty(doc.mode_of_delivery) == false ? doc.mode_of_delivery : fr.model.Customer.WayOfDelivery;
            fr.model.Customer.Type = string.IsNullOrEmpty(fr.model.Customer.Type) == true && string.IsNullOrEmpty(fr.model.Customer.VATNumber) == false ? "COMPANY" : fr.model.Customer.Type;
            fr.model.Customer.PriceList = string.IsNullOrEmpty(fr.model.Customer.PriceList) == true ? this.default_values.PriceList : fr.model.Customer.PriceList;

            // Add or update the customer
            if (customer_exists == true)
            {
                fr = await this.nox_client.Update<CustomerRoot>(fr.model, $"customers/{fr.model.Customer.CustomerNumber}");
            }
            else
            {
                fr = await this.nox_client.Add<CustomerRoot>(fr.model, "customers");
            }

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

            // Return the post
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

            // Log errors
            if (string.IsNullOrEmpty(fr_page.error) == false)
            {
                this.logger.LogError(fr_page.error);
            }

            // Make sure that at least one supplier was found
            if (fr_page.model != null && fr_page.model.Suppliers != null && fr_page.model.Suppliers.Count > 0)
            {
                // Get a supplier
                fr = await this.nox_client.Get<SupplierRoot>($"suppliers/{fr_page.model.Suppliers[0].SupplierNumber}");

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }
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
            if(doc.seller_information != null)
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
            if(doc.payment_options != null)
            {
                // Loop payment options
                foreach(PaymentOption po in doc.payment_options)
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
            }
            else
            {
                fr = await this.nox_client.Add<SupplierRoot>(fr.model, "suppliers");
            }

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

            // Return the post
            return fr.model;

        } // End of the UpsertSupplier method

        /// <summary>
        /// Upsert currencies
        /// </summary>
        public async Task UpsertCurrencies(FixerRates fixer_rates)
        {
            // Loop currency rates
            foreach (KeyValuePair<string, decimal> entry in fixer_rates.rates)
            {
                // A boolean that indicates if the currency exists
                bool currency_exists = false;

                // Get the currency root
                FortnoxResponse<CurrencyRoot> fr = await this.nox_client.Get<CurrencyRoot>($"currencies/{entry.Key.ToUpper()}");

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }

                // Check if the currency exists
                if (fr.model != null)
                {
                    currency_exists = true;
                }

                // Calculate the currency rate
                decimal currency_rate = Math.Round(1 / entry.Value, 6, MidpointRounding.AwayFromZero);

                // Create a new currency
                fr.model = new CurrencyRoot
                {
                    Currency = new Currency
                    {
                        Code = entry.Key.ToUpper(),
                        Description = currency_exists == false ? entry.Key.ToUpper() : null,
                        Unit = 1M,
                        BuyRate = currency_rate,
                        SellRate = currency_rate
                    }
                };

                // Update or add the currency
                if (currency_exists == true)
                {
                    // Update the post
                    fr = await this.nox_client.Update<CurrencyRoot>(fr.model, $"currencies/{entry.Key.ToUpper()}");
                }
                else
                {
                    // Add the post
                    fr = await this.nox_client.Add<CurrencyRoot>(fr.model, $"currencies");
                }

                // Log errors
                if (string.IsNullOrEmpty(fr.error) == false)
                {
                    this.logger.LogError(fr.error);
                }
            }

        } // End of the UpsertCurrencies method

        /// <summary>
        /// Get trusted email senders
        /// </summary>
        public async Task<EmailSendersRoot> GetTrustedEmailSenders()
        {
            // Get a response
            FortnoxResponse<EmailSendersRoot> fr = await this.nox_client.Get<EmailSendersRoot>("emailsenders");

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

            // Return the model
            return fr.model;

        } // End of the GetTrustedEmailSenders method

        #endregion

        #region Documents

        /// <summary>
        /// Add an offer
        /// </summary>
        public async Task<OfferRoot> AddOffer(string dox_email, AnnytabDoxTrade doc)
        {
            // Terms of delivery
            if(string.IsNullOrEmpty(doc.terms_of_delivery) == false)
            {
                doc.terms_of_delivery = CommonTools.ConvertToAlphanumeric(doc.terms_of_delivery).ToUpper();
                await AddTermsOfDelivery(doc.terms_of_delivery);
            }

            // Terms of payment
            if(string.IsNullOrEmpty(doc.terms_of_payment) == false)
            {
                doc.terms_of_payment = CommonTools.ConvertToAlphanumeric(doc.terms_of_payment).ToUpper().Replace("-", "");
                await AddTermsOfPayment(doc.terms_of_payment);
            }

            // Way of delivery
            if(string.IsNullOrEmpty(doc.mode_of_delivery) == false)
            {
                doc.mode_of_delivery = CommonTools.ConvertToAlphanumeric(doc.mode_of_delivery).ToUpper();
                await AddWayOfDelivery(doc.mode_of_delivery);
            }

            // Currency
            if(string.IsNullOrEmpty(doc.currency_code) == false)
            {
                doc.currency_code = doc.currency_code.ToUpper();
                await AddCurrency(doc.currency_code);
            }

            // Upsert the customer
            CustomerRoot customer_root = await UpsertCustomer(dox_email, doc);

            // Return if the customer is null
            if (customer_root == null || customer_root.Customer == null)
            {
                return null;
            }

            // Create a list with offer rows
            IList<OfferRow> rows = new List<OfferRow>();

            // Add offer rows
            if(doc.product_rows != null)
            {
                await AddOfferRows(doc.product_rows, rows);
            }

            // Create an offer
            OfferRoot root = new OfferRoot
            {
                Offer = new Offer
                {
                    CustomerNumber = customer_root.Customer.CustomerNumber,
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

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

            // Return the offer
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
                doc.terms_of_delivery = CommonTools.ConvertToAlphanumeric(doc.terms_of_delivery).ToUpper();
                await AddTermsOfDelivery(doc.terms_of_delivery);
            }

            // Terms of payment
            if (string.IsNullOrEmpty(doc.terms_of_payment) == false)
            {
                doc.terms_of_payment = CommonTools.ConvertToAlphanumeric(doc.terms_of_payment).ToUpper().Replace("-", "");
                await AddTermsOfPayment(doc.terms_of_payment);
            }

            // Way of delivery
            if (string.IsNullOrEmpty(doc.mode_of_delivery) == false)
            {
                doc.mode_of_delivery = CommonTools.ConvertToAlphanumeric(doc.mode_of_delivery).ToUpper();
                await AddWayOfDelivery(doc.mode_of_delivery);
            }

            // Currency
            if (string.IsNullOrEmpty(doc.currency_code) == false)
            {
                doc.currency_code = doc.currency_code.ToUpper();
                await AddCurrency(doc.currency_code);
            }

            // Upsert the customer
            CustomerRoot customer_root = await UpsertCustomer(dox_email, doc);

            // Return if the customer is null
            if (customer_root == null || customer_root.Customer == null)
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
                    CustomerNumber = customer_root.Customer.CustomerNumber,
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

            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

            // Return the order
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
                doc.terms_of_payment = CommonTools.ConvertToAlphanumeric(doc.terms_of_payment).ToUpper().Replace("-", "");
                await AddTermsOfPayment(doc.terms_of_payment);
            }

            // Currency
            if (string.IsNullOrEmpty(doc.currency_code) == false)
            {
                doc.currency_code = doc.currency_code.ToUpper();
                await AddCurrency(doc.currency_code);
            }

            // Upsert the supplier
            SupplierRoot supplier_root = await UpsertSupplier(dox_email, doc);

            // Return if the supplier_root is null
            if(supplier_root == null || supplier_root.Supplier == null)
            {
                return null;
            }

            // Create a list with supplier invoice rows
            IList<SupplierInvoiceRow> rows = new List<SupplierInvoiceRow>();

            //// Add accounts payable amount
            //if(doc.total != null && doc.total != 0M)
            //{
            //    rows.Add(new SupplierInvoiceRow
            //    {
            //        Code = "TOT",
            //        Total = doc.total * -1
            //    });
            //}
            
            //// Add value added tax
            //if (doc.vat_total != null && doc.vat_total != 0M)
            //{
            //    rows.Add(new SupplierInvoiceRow
            //    {
            //        Code = "VAT",
            //        Total = doc.vat_total
            //    });
            //}

            //// Add rounding
            //if(doc.rounding != null && doc.rounding != 0M)
            //{
            //    rows.Add(new SupplierInvoiceRow
            //    {
            //        Code = "ROV",
            //        Total = doc.rounding
            //    });
            //}
            
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
                    SupplierNumber = supplier_root.Supplier.SupplierNumber,
                    InvoiceNumber = string.IsNullOrEmpty(doc.payment_reference) == false ? doc.payment_reference : null,
                    InvoiceDate = string.IsNullOrEmpty(doc.issue_date) == false ? doc.issue_date : null,
                    DueDate = string.IsNullOrEmpty(doc.due_date) == false ? doc.due_date : null,
                    Currency = doc.currency_code,
                    Comments = doc.comment,
                    Total = doc.total != null ? doc.total : 0M,
                    VAT = doc.vat_total != null ? doc.vat_total : 0M,
                    RoundOffValue = doc.rounding != null ? doc.rounding : 0M,
                    SupplierInvoiceRows = rows
                }
            };

            // Add a supplier invoice
            FortnoxResponse<SupplierInvoiceRoot> fr = await this.nox_client.Add<SupplierInvoiceRoot>(root, "supplierinvoices");
            
            // Log errors
            if (string.IsNullOrEmpty(fr.error) == false)
            {
                this.logger.LogError(fr.error);
            }

            // Return the supplier invoice
            return fr.model;

        } // End of the AddSupplierInvoice method

        #endregion

        #region Helper methods

        /// <summary>
        /// Add offer rows recursively
        /// </summary>
        private async Task AddOfferRows(IList<ProductRow> product_rows, IList<OfferRow> offer_rows)
        {
            // Loop product rows
            foreach (ProductRow row in product_rows)
            {
                // Unit
                if(string.IsNullOrEmpty(row.unit_code) == false)
                {
                    row.unit_code = CommonTools.ConvertToAlphanumeric(row.unit_code).ToLower();
                    await AddUnit(row.unit_code);
                }

                // Article, add if there is an identifier
                ArticleRoot article_root = null;
                if (string.IsNullOrEmpty(row.product_code) == false || string.IsNullOrEmpty(row.manufacturer_code) == false || string.IsNullOrEmpty(row.gtin) == false)
                {
                    article_root = await AddArticle(row);
                }
                
                // Add a offer row
                offer_rows.Add(new OfferRow
                {
                    ArticleNumber = article_root != null ? article_root.Article.ArticleNumber : null,
                    Description = row.product_name,
                    Quantity = row.quantity,
                    Price = row.unit_price,
                    Unit = article_root != null ? article_root.Article.Unit : row.unit_code
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
                    row.unit_code = CommonTools.ConvertToAlphanumeric(row.unit_code).ToLower();
                    await AddUnit(row.unit_code);
                }

                // Article, add if there is an identifier
                ArticleRoot article_root = null;
                if (string.IsNullOrEmpty(row.product_code) == false || string.IsNullOrEmpty(row.manufacturer_code) == false || string.IsNullOrEmpty(row.gtin) == false)
                {
                    article_root = await AddArticle(row);
                }

                // Add a order row
                order_rows.Add(new OrderRow
                {
                    ArticleNumber = article_root != null ? article_root.Article.ArticleNumber : null,
                    Description = row.product_name,
                    OrderedQuantity = row.quantity,
                    DeliveredQuantity = row.quantity,
                    Price = row.unit_price,
                    Unit = article_root != null ? article_root.Article.Unit : row.unit_code
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
                    row.unit_code = CommonTools.ConvertToAlphanumeric(row.unit_code).ToLower();
                    await AddUnit(row.unit_code);
                }

                // Article, add if there is an identifier
                ArticleRoot article_root = null;
                if (string.IsNullOrEmpty(row.product_code) == false || string.IsNullOrEmpty(row.manufacturer_code) == false || string.IsNullOrEmpty(row.gtin) == false)
                {
                    article_root = await AddArticle(row);
                }

                // Add a supplier invoice row
                supplier_invoice_rows.Add(new SupplierInvoiceRow
                {
                    ArticleNumber = article_root != null ? article_root.Article.ArticleNumber : null,
                    Account = article_root == null ? this.default_values.PurchaseAccount : null,
                    ItemDescription = row.product_name,
                    Quantity = row.quantity,
                    Price = row.unit_price
                    //Unit = article_root != null ? article_root.Article.Unit : row.unit_code      
                });

                // Check if there is sub rows
                if (row.subrows != null && row.subrows.Count > 0)
                {
                    await AddSupplierInvoiceRows(row.subrows, supplier_invoice_rows);
                }
            }

        } // End of the AddSupplierInvoiceRows method

        #endregion

    } // End of the class

} // End of the namespace