using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Annytab.Dox.Standards.V1;
using Annytab.Fortnox.Client.V3;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This class handles imports to fortnox
    /// </summary>
    public class FortnoxImporter : IFortnoxImporter
    {
        #region Variables

        private readonly ILogger logger;
        private readonly IFortnoxRepository fortnox_repository;
        private readonly DefaultValues default_values;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new fortnox importer
        /// </summary>
        public FortnoxImporter(ILogger<FortnoxImporter> logger, IFortnoxRepository fortnox_repository, IOptions<DefaultValues> default_values)
        {
            // Set values for instance variables
            this.logger = logger;
            this.fortnox_repository = fortnox_repository;
            this.default_values = default_values.Value;

        } // End of the constructor

        #endregion

        #region Registers

        /// <summary>
        /// Add a term of delivery if it does not exists
        /// </summary>
        public async Task<TermsOfDeliveryRoot> AddTermsOfDelivery(HttpClient client, string term_of_delivery)
        {
            // Get the root
            TermsOfDeliveryRoot root = await this.fortnox_repository.Get<TermsOfDeliveryRoot>(client, $"termsofdeliveries/{term_of_delivery}");

            // Add the post if it does not exist
            if (root == null)
            {
                // Create a new post
                root = new TermsOfDeliveryRoot
                {
                    TermsOfDelivery = new TermsOfDelivery
                    {
                        Code = term_of_delivery,
                        Description = term_of_delivery
                    }
                };
                
                // Add the post
                root = await this.fortnox_repository.Add<TermsOfDeliveryRoot>(client, root, $"termsofdeliveries");
            }

            // Return the post
            return root;

        } // End of the AddTermsOfDelivery method

        /// <summary>
        /// Add a term of payment if it does not exists
        /// </summary>
        public async Task<TermsOfPaymentRoot> AddTermsOfPayment(HttpClient client, string term_of_payment)
        {
            // Get the root
            TermsOfPaymentRoot root = await this.fortnox_repository.Get<TermsOfPaymentRoot>(client, $"termsofpayments/{term_of_payment}");

            // Add the post if it does not exist
            if (root == null)
            {
                // Create a new post
                root = new TermsOfPaymentRoot
                {
                    TermsOfPayment = new TermsOfPayment
                    {
                        Code = term_of_payment,
                        Description = term_of_payment
                    }
                };

                // Add the post
                root = await this.fortnox_repository.Add<TermsOfPaymentRoot>(client, root, $"termsofpayments");
            }

            // Return the post
            return root;

        } // End of the AddTermsOfPayment method

        /// <summary>
        /// Add a way of delivery if it does not exists
        /// </summary>
        public async Task<WayOfDeliveryRoot> AddWayOfDelivery(HttpClient client, string way_of_delivery)
        {
            // Get the root
            WayOfDeliveryRoot root = await this.fortnox_repository.Get<WayOfDeliveryRoot>(client, $"wayofdeliveries/{way_of_delivery}");

            // Add the post if it does not exist
            if (root == null)
            {
                // Create a new post
                root = new WayOfDeliveryRoot
                {
                    WayOfDelivery = new WayOfDelivery
                    {
                        Code = way_of_delivery,
                        Description = way_of_delivery
                    }
                };

                // Add the post
                root = await this.fortnox_repository.Add<WayOfDeliveryRoot>(client, root, $"wayofdeliveries");
            }

            // Return the post
            return root;

        } // End of the AddWayOfDelivery method

        /// <summary>
        /// Add a currency if it does not exists
        /// </summary>
        public async Task<CurrencyRoot> AddCurrency(HttpClient client, string currency_code)
        {
            // Get the root
            CurrencyRoot root = await this.fortnox_repository.Get<CurrencyRoot>(client, $"currencies/{currency_code}");

            // Add the currency if it does not exist
            if (root == null)
            {
                // Create a new post
                root = new CurrencyRoot
                {
                    Currency = new Currency
                    {
                        Code = currency_code,
                        Description = currency_code
                    }
                };

                // Add the post
                root = await this.fortnox_repository.Add<CurrencyRoot>(client, root, $"currencies");
            }

            // Return the post
            return root;

        } // End of the AddCurrency method

        /// <summary>
        /// Add a unit if it does not exists
        /// </summary>
        public async Task<UnitRoot> AddUnit(HttpClient client, string unit_code)
        {
            // Get the root
            UnitRoot root = await this.fortnox_repository.Get<UnitRoot>(client, $"units/{unit_code}");

            // Add the unit if it does not exist
            if (root == null)
            {
                // Create a new post
                root = new UnitRoot
                {
                    Unit = new Unit
                    {
                        Code = unit_code,
                        Description = unit_code
                    }
                };

                // Add a unit
                root = await this.fortnox_repository.Add<UnitRoot>(client, root, $"units");
            }

            // Return the post
            return root;

        } // End of the AddUnit method

        /// <summary>
        /// Add a price list if it does not exists
        /// </summary>
        public async Task<PriceListRoot> AddPriceList(HttpClient client, string code)
        {
            // Get the root
            PriceListRoot root = await this.fortnox_repository.Get<PriceListRoot>(client, $"pricelists/{code}");

            // Add the price list if it does not exist
            if (root == null)
            {
                // Create a new post
                root = new PriceListRoot
                {
                    PriceList = new PriceList
                    {
                        Code = code,
                        Description = code
                    }
                };

                // Add a price list
                root = await this.fortnox_repository.Add<PriceListRoot>(client, root, $"pricelists");
            }

            // Return the post
            return root;

        } // End of the AddPriceList method

        /// <summary>
        /// Add an account if it does not exist
        /// </summary>
        public async Task<AccountRoot> AddAccount(HttpClient client, string account_number)
        {
            // Get the root
            AccountRoot root = await this.fortnox_repository.Get<AccountRoot>(client, $"accounts/{account_number}");

            // Add the account if it does not exist
            if (root == null)
            {
                // Create a new post
                root = new AccountRoot
                {
                    Account = new Account
                    {
                        Number = account_number,
                        Description = account_number
                    }
                };

                // Add an account
                root = await this.fortnox_repository.Add<AccountRoot>(client, root, $"accounts");
            }

            // Return the post
            return root;

        } // End of the AddAccount method

        /// <summary>
        /// Add an article if it does not exist
        /// </summary>
        public async Task<ArticleRoot> AddArticle(HttpClient client, ProductRow row)
        {
            // Create a reference to an article root
            ArticleRoot root = null;

            // Make sure that the product code only consists of alphanumeric characters
            row.product_code = string.IsNullOrEmpty(row.product_code) == false ? CommonTools.ConvertToAlphanumeric(row.product_code) : null;

            // Find the article
            if (string.IsNullOrEmpty(row.gtin) == false)
            {
                // Try to get an article on EAN
                ArticlesRoot articles_root = await this.fortnox_repository.Get<ArticlesRoot>(client, $"articles?ean={row.gtin}");

                if (articles_root != null && articles_root.Articles != null && articles_root.Articles.Count > 0)
                {
                    root = await this.fortnox_repository.Get<ArticleRoot>(client, $"articles/{articles_root.Articles[0].ArticleNumber}");
                }
            }
            if(root == null && string.IsNullOrEmpty(row.manufacturer_code) == false)
            {
                // Try to get an article on manufacturer code
                ArticlesRoot articles_root = await this.fortnox_repository.Get<ArticlesRoot>(client, $"articles?manufacturerarticlenumber={row.manufacturer_code}");

                if (articles_root != null && articles_root.Articles != null && articles_root.Articles.Count > 0)
                {
                    root = await this.fortnox_repository.Get<ArticleRoot>(client, $"articles/{articles_root.Articles[0].ArticleNumber}");
                }
            }
            if(root == null && string.IsNullOrEmpty(row.product_code) == false)
            {
                // Try to get an article on article number
                root = await this.fortnox_repository.Get<ArticleRoot>(client, $"articles/{row.product_code}");
            }

            // Add the article if it does not exist
            if (root == null)
            {
                // Create a new article
                root = new ArticleRoot
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

                // Add the post
                root = await this.fortnox_repository.Add<ArticleRoot>(client, root, "articles");

                // Add a default price
                if(root != null)
                {
                    PriceRoot price = new PriceRoot
                    {
                        Price = new Price
                        {
                            ArticleNumber = root.Article.ArticleNumber,
                            PriceList = this.default_values.PriceList,
                            FromQuantity = 0,
                            Amount = row.unit_price
                        }
                    };

                    price = await this.fortnox_repository.Add<PriceRoot>(client, price, "prices");
                }     
            }

            // Return the post
            return root;

        } // End of the AddArticle method

        /// <summary>
        /// Add or update a customer
        /// </summary>
        public async Task<CustomerRoot> UpsertCustomer(HttpClient client, string dox_email, AnnytabDoxTrade doc)
        {
            // Create variables
            CustomerRoot root = null;
            bool customer_exists = false;
            string customer_email = doc.buyer_information != null && string.IsNullOrEmpty(doc.buyer_information.email) == false ? doc.buyer_information.email : dox_email;

            // Find the customer on email
            CustomersRoot customers_root = await this.fortnox_repository.Get<CustomersRoot>(client, $"customers?email={customer_email}");
            if (customers_root != null && customers_root.Customers != null && customers_root.Customers.Count > 0)
            {
                root = await this.fortnox_repository.Get<CustomerRoot>(client, $"customers/{customers_root.Customers[0].CustomerNumber}");
            }

            // Check if the customer exists
            if (root != null)
            {
                customer_exists = true;
            }
            else
            {
                root = new CustomerRoot { Customer = new Customer() };
            }

            // Update the customer: ONLY SET VAT TYPE, ACCOUNT IS SET IN ARTICLE
            root.Customer.Email = customer_email;
            if (doc.seller_information != null)
            {
                root.Customer.OurReference = string.IsNullOrEmpty(root.Customer.OurReference) == true ? doc.seller_information.contact_name : root.Customer.OurReference;
            }
            if(doc.buyer_information != null)
            {
                root.Customer.Name = string.IsNullOrEmpty(doc.buyer_information.person_name) == false ? doc.buyer_information.person_name : root.Customer.Name;
                root.Customer.OrganisationNumber = string.IsNullOrEmpty(doc.buyer_information.person_id) == false ? doc.buyer_information.person_id : root.Customer.OrganisationNumber;
                root.Customer.VATNumber = string.IsNullOrEmpty(doc.buyer_information.vat_number) == false ? doc.buyer_information.vat_number : root.Customer.VATNumber;
                root.Customer.YourReference = string.IsNullOrEmpty(doc.buyer_information.contact_name) == false ? doc.buyer_information.contact_name : root.Customer.YourReference;
                root.Customer.Phone1 = string.IsNullOrEmpty(doc.buyer_information.phone_number) == false ? doc.buyer_information.phone_number : root.Customer.Phone1;
                root.Customer.Address1 = string.IsNullOrEmpty(doc.buyer_information.address_line_1) == false ? doc.buyer_information.address_line_1 : root.Customer.Address1;
                root.Customer.Address2 = string.IsNullOrEmpty(doc.buyer_information.address_line_2) == false ? doc.buyer_information.address_line_2 : root.Customer.Address2;
                root.Customer.ZipCode = string.IsNullOrEmpty(doc.buyer_information.postcode) == false ? doc.buyer_information.postcode : root.Customer.ZipCode;
                root.Customer.City = string.IsNullOrEmpty(doc.buyer_information.city_name) == false ? doc.buyer_information.city_name : root.Customer.City;
                root.Customer.CountryCode = string.IsNullOrEmpty(doc.buyer_information.country_code) == false ? doc.buyer_information.country_code : root.Customer.CountryCode;
                root.Customer.EmailOffer = string.IsNullOrEmpty(root.Customer.EmailOffer) == true ? customer_email : root.Customer.EmailOffer;
                root.Customer.EmailOrder = string.IsNullOrEmpty(root.Customer.EmailOrder) == true ? customer_email : root.Customer.EmailOrder;
                root.Customer.EmailInvoice = string.IsNullOrEmpty(root.Customer.EmailInvoice) == true ? customer_email : root.Customer.EmailInvoice;
            }
            if(doc.delivery_information != null)
            {
                root.Customer.DeliveryName = string.IsNullOrEmpty(doc.delivery_information.person_name) == false ? doc.delivery_information.person_name : root.Customer.DeliveryName;
                root.Customer.DeliveryPhone1 = string.IsNullOrEmpty(doc.delivery_information.phone_number) == false ? doc.delivery_information.phone_number : root.Customer.DeliveryPhone1;
                root.Customer.DeliveryAddress1 = string.IsNullOrEmpty(doc.delivery_information.address_line_1) == false ? doc.delivery_information.address_line_1 : root.Customer.DeliveryAddress1;
                root.Customer.DeliveryAddress2 = string.IsNullOrEmpty(doc.delivery_information.address_line_2) == false ? doc.delivery_information.address_line_2 : root.Customer.DeliveryAddress2;
                root.Customer.DeliveryCity = string.IsNullOrEmpty(doc.delivery_information.city_name) == false ? doc.delivery_information.city_name : root.Customer.DeliveryCity;
                root.Customer.DeliveryCountryCode = string.IsNullOrEmpty(doc.delivery_information.country_code) == false ? doc.delivery_information.country_code : root.Customer.DeliveryCountryCode;
                root.Customer.DeliveryZipCode = string.IsNullOrEmpty(doc.delivery_information.postcode) == false ? doc.delivery_information.postcode : root.Customer.DeliveryZipCode;
            }
            root.Customer.Currency = string.IsNullOrEmpty(doc.currency_code) == false ? doc.currency_code : root.Customer.Currency;
            root.Customer.TermsOfDelivery = string.IsNullOrEmpty(doc.terms_of_delivery) == false ? doc.terms_of_delivery : root.Customer.TermsOfDelivery;
            root.Customer.TermsOfPayment = string.IsNullOrEmpty(doc.terms_of_payment) == false ? doc.terms_of_payment : root.Customer.TermsOfPayment;
            root.Customer.VATType = CommonTools.GetCustomerVatType(root.Customer, this.default_values);
            root.Customer.WayOfDelivery = string.IsNullOrEmpty(doc.mode_of_delivery) == false ? doc.mode_of_delivery : root.Customer.WayOfDelivery;
            root.Customer.Type = string.IsNullOrEmpty(root.Customer.Type) == true && string.IsNullOrEmpty(root.Customer.VATNumber) == false ? "COMPANY" : root.Customer.Type;
            root.Customer.PriceList = string.IsNullOrEmpty(root.Customer.PriceList) == true ? this.default_values.PriceList : root.Customer.PriceList;

            // Add or update the customer
            if (customer_exists == true)
            {
                root = await this.fortnox_repository.Update<CustomerRoot>(client, root, $"customers/{root.Customer.CustomerNumber}");
            }
            else
            {
                root = await this.fortnox_repository.Add<CustomerRoot>(client, root, "customers");
            }

            // Return the post
            return root;

        } // End of the UpsertCustomer method

        /// <summary>
        /// Add or update a supplier
        /// </summary>
        public async Task<SupplierRoot> UpsertSupplier(HttpClient client, string dox_email, AnnytabDoxTrade doc)
        {
            // Create variables
            SupplierRoot root = null;
            bool supplier_exists = false;
            string supplier_email = doc.seller_information != null && string.IsNullOrEmpty(doc.seller_information.email) == false ? doc.seller_information.email : dox_email;

            // Find the supplier on email
            SuppliersRoot suppliers_root = await this.fortnox_repository.Get<SuppliersRoot>(client, $"suppliers?email={supplier_email}");
            if (suppliers_root != null && suppliers_root.Suppliers != null && suppliers_root.Suppliers.Count > 0)
            {
                root = await this.fortnox_repository.Get<SupplierRoot>(client, $"suppliers/{suppliers_root.Suppliers[0].SupplierNumber}");
            }

            // Check if the supplier exists
            if (root != null)
            {
                supplier_exists = true;
            }
            else
            {
                root = new SupplierRoot { Supplier = new Supplier() };
            }

            // Update the supplier
            root.Supplier.Email = supplier_email;
            if (doc.buyer_information != null)
            {
                root.Supplier.OurReference = string.IsNullOrEmpty(root.Supplier.OurReference) == true ? doc.buyer_information.contact_name : root.Supplier.OurReference;
            }
            if(doc.seller_information != null)
            {
                root.Supplier.Name = string.IsNullOrEmpty(doc.seller_information.person_name) == false ? doc.seller_information.person_name : root.Supplier.Name;
                root.Supplier.OrganisationNumber = string.IsNullOrEmpty(doc.seller_information.person_id) == false ? doc.seller_information.person_id : root.Supplier.OrganisationNumber;
                root.Supplier.VATNumber = string.IsNullOrEmpty(doc.seller_information.vat_number) == false ? doc.seller_information.vat_number : root.Supplier.VATNumber;
                root.Supplier.YourReference = string.IsNullOrEmpty(doc.seller_information.contact_name) == false ? doc.seller_information.contact_name : root.Supplier.YourReference;
                root.Supplier.Phone1 = string.IsNullOrEmpty(doc.seller_information.phone_number) == false ? doc.seller_information.phone_number : root.Supplier.Phone1;
                root.Supplier.Address1 = string.IsNullOrEmpty(doc.seller_information.address_line_1) == false ? doc.seller_information.address_line_1 : root.Supplier.Address1;
                root.Supplier.Address2 = string.IsNullOrEmpty(doc.seller_information.address_line_2) == false ? doc.seller_information.address_line_2 : root.Supplier.Address2;
                root.Supplier.ZipCode = string.IsNullOrEmpty(doc.seller_information.postcode) == false ? doc.seller_information.postcode : root.Supplier.ZipCode;
                root.Supplier.City = string.IsNullOrEmpty(doc.seller_information.city_name) == false ? doc.seller_information.city_name : root.Supplier.City;
                root.Supplier.CountryCode = string.IsNullOrEmpty(doc.seller_information.country_code) == false ? doc.seller_information.country_code : root.Supplier.CountryCode;
            }
            root.Supplier.Currency = string.IsNullOrEmpty(doc.currency_code) == false ? doc.currency_code : root.Supplier.Currency;
            root.Supplier.TermsOfPayment = string.IsNullOrEmpty(doc.terms_of_payment) == false ? doc.terms_of_payment : root.Supplier.TermsOfPayment;
            root.Supplier.VATType = string.IsNullOrEmpty(root.Supplier.VATType) == true ? "NORMAL" : root.Supplier.VATType;
            root.Supplier.OurCustomerNumber = doc.buyer_references != null && doc.buyer_references.ContainsKey("customer_id") ? doc.buyer_references["customer_id"] : null;
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
                        root.Supplier.BIC = string.IsNullOrEmpty(po.bank_identifier_code) == false ? po.bank_identifier_code : root.Supplier.BIC;
                        root.Supplier.IBAN = string.IsNullOrEmpty(po.account_reference) == false ? po.account_reference : root.Supplier.IBAN;
                    }
                    else if (name == "BG")
                    {
                        root.Supplier.BG = string.IsNullOrEmpty(po.account_reference) == false ? po.account_reference : root.Supplier.BG;
                    }
                    else if (name == "PG")
                    {
                        root.Supplier.PG = string.IsNullOrEmpty(po.account_reference) == false ? po.account_reference : root.Supplier.PG;
                    }
                    else if (name == "BANK")
                    {
                        root.Supplier.BankAccountNumber = string.IsNullOrEmpty(po.account_reference) == false ? po.account_reference.Replace(" ", "").Replace("-", "") : root.Supplier.BankAccountNumber;
                        root.Supplier.Bank = string.IsNullOrEmpty(po.bank_name) == false ? po.bank_name : root.Supplier.Bank;
                    }
                }
            }

            // Add or update the supplier
            if (supplier_exists == true)
            {
                root = await this.fortnox_repository.Update<SupplierRoot>(client, root, $"suppliers/{root.Supplier.SupplierNumber}");
            }
            else
            {
                root = await this.fortnox_repository.Add<SupplierRoot>(client, root, "suppliers");
            }

            // Return the post
            return root;

        } // End of the UpsertSupplier method

        /// <summary>
        /// Upsert currencies
        /// </summary>
        public async Task UpsertCurrencies(HttpClient client, FixerRates fixer_rates)
        {
            // Loop currency rates
            foreach (KeyValuePair<string, decimal> entry in fixer_rates.rates)
            {
                // A boolean that indicates if the currency exists
                bool currency_exists = false;

                // Get the currency root
                CurrencyRoot root = await this.fortnox_repository.Get<CurrencyRoot>(client, $"currencies/{entry.Key.ToUpper()}");

                // Check if the currency exists
                if (root != null) { currency_exists = true; }

                // Calculate the currency rate
                decimal currency_rate = Math.Round(1 / entry.Value, 6, MidpointRounding.AwayFromZero);

                // Create a new currency
                root = new CurrencyRoot
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
                    root = await this.fortnox_repository.Update<CurrencyRoot>(client, root, $"currencies/{entry.Key.ToUpper()}");
                }
                else
                {
                    // Add the post
                    root = await this.fortnox_repository.Add<CurrencyRoot>(client, root, $"currencies");
                }
            }

        } // End of the UpsertCurrencies method

        #endregion

        #region Documents

        /// <summary>
        /// Add an offer
        /// </summary>
        public async Task<OfferRoot> AddOffer(HttpClient client, string dox_email, AnnytabDoxTrade doc)
        {
            // Terms of delivery
            if(string.IsNullOrEmpty(doc.terms_of_delivery) == false)
            {
                doc.terms_of_delivery = CommonTools.ConvertToAlphanumeric(doc.terms_of_delivery).ToUpper();
                await AddTermsOfDelivery(client, doc.terms_of_delivery);
            }

            // Terms of payment
            if(string.IsNullOrEmpty(doc.terms_of_payment) == false)
            {
                doc.terms_of_payment = CommonTools.ConvertToAlphanumeric(doc.terms_of_payment).ToUpper();
                await AddTermsOfPayment(client, doc.terms_of_payment);
            }

            // Way of delivery
            if(string.IsNullOrEmpty(doc.mode_of_delivery) == false)
            {
                doc.mode_of_delivery = CommonTools.ConvertToAlphanumeric(doc.mode_of_delivery).ToUpper();
                await AddWayOfDelivery(client, doc.mode_of_delivery);
            }

            // Currency
            if(string.IsNullOrEmpty(doc.currency_code) == false)
            {
                doc.currency_code = doc.currency_code.ToUpper();
                await AddCurrency(client, doc.currency_code);
            }

            // Upsert the customer
            CustomerRoot customer_root = await UpsertCustomer(client, dox_email, doc);

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
                await AddOfferRows(client, doc.product_rows, rows);
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
            return await this.fortnox_repository.Add<OfferRoot>(client, root, "offers");

        } // End of the AddOffer method

        /// <summary>
        /// Add an order
        /// </summary>
        public async Task<OrderRoot> AddOrder(HttpClient client, string dox_email, AnnytabDoxTrade doc)
        {
            // Terms of delivery
            if (string.IsNullOrEmpty(doc.terms_of_delivery) == false)
            {
                doc.terms_of_delivery = CommonTools.ConvertToAlphanumeric(doc.terms_of_delivery).ToUpper();
                await AddTermsOfDelivery(client, doc.terms_of_delivery);
            }

            // Terms of payment
            if (string.IsNullOrEmpty(doc.terms_of_payment) == false)
            {
                doc.terms_of_payment = CommonTools.ConvertToAlphanumeric(doc.terms_of_payment).ToUpper();
                await AddTermsOfPayment(client, doc.terms_of_payment);
            }

            // Way of delivery
            if (string.IsNullOrEmpty(doc.mode_of_delivery) == false)
            {
                doc.mode_of_delivery = CommonTools.ConvertToAlphanumeric(doc.mode_of_delivery).ToUpper();
                await AddWayOfDelivery(client, doc.mode_of_delivery);
            }

            // Currency
            if (string.IsNullOrEmpty(doc.currency_code) == false)
            {
                doc.currency_code = doc.currency_code.ToUpper();
                await AddCurrency(client, doc.currency_code);
            }

            // Upsert the customer
            CustomerRoot customer_root = await UpsertCustomer(client, dox_email, doc);

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
                await AddOrderRows(client, doc.product_rows, rows);
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
            return await this.fortnox_repository.Add<OrderRoot>(client, root, "orders");

        } // End of the AddOrder method

        /// <summary>
        /// Add an supplier invoice
        /// </summary>
        public async Task<SupplierInvoiceRoot> AddSupplierInvoice(HttpClient client, string dox_email, AnnytabDoxTrade doc)
        {
            // Terms of payment
            if (string.IsNullOrEmpty(doc.terms_of_payment) == false)
            {
                doc.terms_of_payment = CommonTools.ConvertToAlphanumeric(doc.terms_of_payment).ToUpper();
                await AddTermsOfPayment(client, doc.terms_of_payment);
            }

            // Currency
            if (string.IsNullOrEmpty(doc.currency_code) == false)
            {
                doc.currency_code = doc.currency_code.ToUpper();
                await AddCurrency(client, doc.currency_code);
            }

            // Upsert the supplier
            SupplierRoot supplier_root = await UpsertSupplier(client, dox_email, doc);

            // Return if the supplier_root is null
            if(supplier_root == null || supplier_root.Supplier == null)
            {
                return null;
            }

            // Create a list with supplier invoice rows
            IList<SupplierInvoiceRow> rows = new List<SupplierInvoiceRow>();

            // Add accounts payable amount
            if(doc.total != null && doc.total != 0M)
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
            if(doc.rounding != null && doc.rounding != 0M)
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
                await AddSupplierInvoiceRows(client, doc.product_rows, rows);
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
                    SupplierInvoiceRows = rows
                }
            };

            // Add a supplier invoice
            return await this.fortnox_repository.Add<SupplierInvoiceRoot>(client, root, "supplierinvoices");

        } // End of the AddSupplierInvoice method

        #endregion

        #region Helper methods

        /// <summary>
        /// Add offer rows recursively
        /// </summary>
        private async Task AddOfferRows(HttpClient client, IList<ProductRow> product_rows, IList<OfferRow> offer_rows)
        {
            // Loop product rows
            foreach (ProductRow row in product_rows)
            {
                // Unit
                if(string.IsNullOrEmpty(row.unit_code) == false)
                {
                    row.unit_code = CommonTools.ConvertToAlphanumeric(row.unit_code).ToLower();
                    await AddUnit(client, row.unit_code);
                }

                // Article, add if there is an identifier
                ArticleRoot article_root = null;
                if (string.IsNullOrEmpty(row.product_code) == false || string.IsNullOrEmpty(row.manufacturer_code) == false || string.IsNullOrEmpty(row.gtin) == false)
                {
                    article_root = await AddArticle(client, row);
                }
                
                // Add a offer row
                offer_rows.Add(new OfferRow
                {
                    ArticleNumber = article_root != null ? article_root.Article.ArticleNumber : null,
                    Description = row.product_name,
                    Quantity = row.quantity,
                    Price = row.unit_price,
                    Unit = article_root != null ? article_root.Article.Unit : row.unit_code,
                    VAT = article_root == null && row.vat_rate != null ? row.vat_rate * 100 : null
                });

                // Check if there is sub rows
                if (row.subrows != null && row.subrows.Count > 0)
                {
                    await AddOfferRows(client, row.subrows, offer_rows);
                }
            }

        } // End of the AddOfferRows method

        /// <summary>
        /// Add order rows recursively
        /// </summary>
        private async Task AddOrderRows(HttpClient client, IList<ProductRow> product_rows, IList<OrderRow> order_rows)
        {
            // Loop product rows
            foreach (ProductRow row in product_rows)
            {
                // Unit
                if (string.IsNullOrEmpty(row.unit_code) == false)
                {
                    row.unit_code = CommonTools.ConvertToAlphanumeric(row.unit_code).ToLower();
                    await AddUnit(client, row.unit_code);
                }

                // Article, add if there is an identifier
                ArticleRoot article_root = null;
                if (string.IsNullOrEmpty(row.product_code) == false || string.IsNullOrEmpty(row.manufacturer_code) == false || string.IsNullOrEmpty(row.gtin) == false)
                {
                    article_root = await AddArticle(client, row);
                }

                // Add a order row
                order_rows.Add(new OrderRow
                {
                    ArticleNumber = article_root != null ? article_root.Article.ArticleNumber : null,
                    Description = row.product_name,
                    OrderedQuantity = row.quantity,
                    DeliveredQuantity = row.quantity,
                    Price = row.unit_price,
                    Unit = article_root != null ? article_root.Article.Unit : row.unit_code,
                    VAT = article_root == null && row.vat_rate != null ? row.vat_rate * 100 : null
                });

                // Check if there is sub rows
                if (row.subrows != null && row.subrows.Count > 0)
                {
                    await AddOrderRows(client, row.subrows, order_rows);
                }
            }

        } // End of the AddOrderRows method

        /// <summary>
        /// Add supplier invoice rows recursively
        /// </summary>
        private async Task AddSupplierInvoiceRows(HttpClient client, IList<ProductRow> product_rows, IList<SupplierInvoiceRow> supplier_invoice_rows)
        {
            // Loop product rows
            foreach (ProductRow row in product_rows)
            {
                // Unit
                if (string.IsNullOrEmpty(row.unit_code) == false)
                {
                    row.unit_code = CommonTools.ConvertToAlphanumeric(row.unit_code).ToLower();
                    await AddUnit(client, row.unit_code);
                }

                // Article, add if there is an identifier
                ArticleRoot article_root = null;
                if (string.IsNullOrEmpty(row.product_code) == false || string.IsNullOrEmpty(row.manufacturer_code) == false || string.IsNullOrEmpty(row.gtin) == false)
                {
                    article_root = await AddArticle(client, row);
                }

                // Add a supplier invoice row
                supplier_invoice_rows.Add(new SupplierInvoiceRow
                {
                    ArticleNumber = article_root != null ? article_root.Article.ArticleNumber : null,
                    Account = article_root == null ? this.default_values.PurchaseAccount : null,
                    ItemDescription = row.product_name,
                    Quantity = row.quantity,
                    Price = row.unit_price,
                    Unit = article_root != null ? article_root.Article.Unit : row.unit_code      
                });

                // Check if there is sub rows
                if (row.subrows != null && row.subrows.Count > 0)
                {
                    await AddSupplierInvoiceRows(client, row.subrows, supplier_invoice_rows);
                }
            }

        } // End of the AddSupplierInvoiceRows method

        #endregion

    } // End of the class

} // End of the namespace