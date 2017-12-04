using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Annytab.Dox.Standards.V1;
using System.Globalization;

namespace TestProgram
{
    [TestClass]
    public class CreateDummyFiles
    {
        /// <summary>
        /// Create a request for quotation
        /// </summary>
        [TestMethod]
        public void CreateRequestForQuotation()
        {
            // Make sure that the directory exists
            string directory = Directory.GetCurrentDirectory() + "\\Offers";
            if(Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            // Create a request for quotation
            AnnytabDoxTrade post = new AnnytabDoxTrade();
            post.id = Guid.NewGuid().ToString();
            post.document_type = "request_for_quotation";
            post.payment_reference = "T2";
            post.issue_date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            //post.due_date = invoice.invoice_date.AddDays(15).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            post.delivery_date = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            post.offer_expires_date = DateTime.Now.AddDays(90).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            //post.seller_references = new Dictionary<string, string>();
            //post.seller_references.Add("supplier_id", "");
            //post.seller_references.Add("quotation_id", "");
            //post.seller_references.Add("order_id", "");
            //post.seller_references.Add("invoice_id", "");
            //post.buyer_references = new Dictionary<string, string>();
            //post.buyer_references.Add("customer_id", "1");
            //post.buyer_references.Add("request_for_quotation_id", post.id);
            //post.buyer_references.Add("order_id", "");
            post.terms_of_delivery = "EXW";
            post.terms_of_payment = "Net 30";
            post.mode_of_delivery = "RRR";
            post.total_weight_kg = 20M;
            //post.penalty_interest = 0.10M;
            //post.currency_code = invoice.currency_code;
            //post.vat_country_code = invoice.vat_country_code;
            //post.vat_state_code = invoice.vat_state_code;
            post.comment = "Jag önskar att få priser för dessa produkter.";
            post.seller_information = new PartyInformation()
            {
                person_id = "556864-2747",
                person_name = "A Name Not Yet Taken AB",
                address_line_1 = "Skonertgatan 12",
                address_line_2 = "Kronobränneriet",
                address_line_3 = "",
                postcode = "30238",
                city_name = "Halmstad",
                country_name = "Sweden",
                country_code = "SE",
                state_code = "",
                contact_name = "Fredrik Stigsson",
                phone_number = "",
                email = "dox@annytab.se",
                vat_number = "SE556864274701"
            };
            post.buyer_information = new PartyInformation
            {
                person_id = "445566-8877",
                person_name = "Kundnamn",
                address_line_1 = "Address rad 1",
                address_line_2 = "Address rad 2",
                //address_line_3 = "",
                postcode = "Postkod",
                city_name = "Stad",
                country_name = "Land",
                country_code = "SE",
                //state_code = "",
                contact_name = "Kontaktnamn",
                phone_number = "Telefonnummer",
                email = "sven@annytab.se",
                vat_number = "VAT-nummer"
            };
            //post.payment_options = new List<PaymentOption>
            //{
            //    new PaymentOption
            //    {
            //        name = "IBAN",
            //        account_reference = "SE4680000816959239073274",
            //        bank_identifier_code = "SWEDSESS",
            //        bank_name = "Swedbank AB",
            //        bank_country_code = "SE"
            //    },
            //    new PaymentOption
            //    {
            //        name = "BG",
            //        account_reference = "7893514",
            //        bank_identifier_code = "BGABSESS",
            //        bank_name = "Bankgirocentralen BGC AB",
            //        bank_country_code = "SE"
            //    },
            //    new PaymentOption
            //    {
            //        name = "SWISH",
            //        account_reference = "1235370366",
            //        bank_identifier_code = "SWEDSESS",
            //        bank_name = "Swedbank AB",
            //        bank_country_code = "SE"
            //    },
            //    new PaymentOption
            //    {
            //        name = "PAYPAL.ME",
            //        account_reference = "https://www.paypal.me/annytab",
            //        bank_identifier_code = "",
            //        bank_name = "PayPal",
            //        bank_country_code = "US"
            //    }
            //};
            post.product_rows = new List<ProductRow>
            {
                //new ProductRow
                //{
                //    //product_code = "P-87945656",
                //    //manufacturer_code = "T2223445566",
                //    //gtin = "",
                //    product_name = "SUPER",
                //    vat_rate = 0.25M,
                //    quantity = 50M,
                //    unit_code = "st",
                //    unit_price = 100.45M,
                //    subrows = new List<ProductRow>
                //    {
                //        new ProductRow
                //        {
                //            //product_code = "P-447",
                //            manufacturer_code = "TG00025",
                //            //gtin = "",
                //            product_name = "Ett",
                //            vat_rate = 0.25M,
                //            quantity = 200M,
                //            unit_code = "st",
                //            unit_price = 0M,
                //            subrows = null
                //        },
                //        new ProductRow
                //        {
                //            //product_code = "P-448",
                //            //manufacturer_code = "T2223445568",
                //            gtin = "00002",
                //            product_name = "Två",
                //            vat_rate = 0.25M,
                //            quantity = 50M,
                //            unit_code = "st",
                //            unit_price = 0M,
                //            subrows = new List<ProductRow>
                //            {
                //                new ProductRow
                //                {
                //                    product_code = "TTT-%%//34",
                //                    //manufacturer_code = "T2223445568",
                //                    //gtin = "00002",
                //                    product_name = "2:1",
                //                    vat_rate = 0.12M,
                //                    quantity = 10M,
                //                    unit_code = "st",
                //                    unit_price = 0M
                //                }
                //            }
                //        }
                //    }
                //},
                new ProductRow
                {
                    product_code = "NEW-00001",
                    manufacturer_code = "GiB",
                    gtin = "",
                    product_name = "Gibibytes",
                    vat_rate = 0.25M,
                    quantity = 5M,
                    unit_code = "PIEC",
                    unit_price = 0M,
                    subrows = new List<ProductRow>()
                },
                new ProductRow
                {
                    product_code = "NEW-00002",
                    manufacturer_code = "",
                    gtin = "",
                    product_name = "Sockar",
                    vat_rate = 0.25M,
                    quantity = 80M,
                    unit_code = "st",
                    unit_price = 10.89M,
                    subrows = new List<ProductRow>()
                }
            };
            //post.vat_specification = new List<VatSpecification>
            //{
            //    new VatSpecification
            //    {
            //        tax_rate = 0.25M,
            //        taxable_amount = 0M,
            //        tax_amount = 0M,
            //    }
            //};
            post.subtotal = 0M;
            post.vat_total = 0M;
            post.rounding = 0M;
            post.total = 0M;
            post.paid_amount = 0M;
            post.balance_due = 0M;

            // Write the  to a file
            File.WriteAllText(directory + "\\" + post.id + ".json", JsonConvert.SerializeObject(post));

        } // End of the CreateRequestForQuotation method

        /// <summary>
        /// Create an order
        /// </summary>
        [TestMethod]
        public void CreateOrder()
        {
            // Make sure that the directory exists
            string directory = Directory.GetCurrentDirectory() + "\\Orders";
            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            // Create a request for quotation
            AnnytabDoxTrade post = new AnnytabDoxTrade();
            post.id = Guid.NewGuid().ToString();
            post.document_type = "order";
            post.payment_reference = "D-5001";
            post.issue_date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            //post.due_date = invoice.invoice_date.AddDays(15).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            post.delivery_date = DateTime.Now.AddDays(60).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            post.seller_references = new Dictionary<string, string>();
            //post.seller_references.Add("supplier_id", "");
            post.seller_references.Add("quotation_id", "500");
            //post.seller_references.Add("order_id", "");
            //post.seller_references.Add("invoice_id", "");
            post.buyer_references = new Dictionary<string, string>();
            //post.buyer_references.Add("customer_id", "1");
            post.buyer_references.Add("request_for_quotation_id", "784da920-a5e2-4ad0-afea-6961b9885554");
            post.buyer_references.Add("order_id", "");
            //post.terms_of_delivery = "EXW";
            post.terms_of_payment = "Net 20";
            //post.mode_of_delivery = "WEB";
            post.total_weight_kg = 10M;
            //post.penalty_interest = 0.10M;
            post.currency_code = "SEK";
            //post.vat_country_code = invoice.vat_country_code;
            //post.vat_state_code = invoice.vat_state_code;
            post.comment = "Jag skickar en beställning enligt Offert: 5000. Hoppas att priserna är samma";
            post.seller_information = new PartyInformation()
            {
                person_id = "556864-2747",
                person_name = "A Name Not Yet Taken AB",
                address_line_1 = "Skonertgatan 12",
                address_line_2 = "Kronobränneriet",
                address_line_3 = "",
                postcode = "30238",
                city_name = "Halmstad",
                country_name = "Sweden",
                country_code = "SE",
                state_code = "",
                contact_name = "Fredrik Stigsson",
                phone_number = "",
                email = "dox@annytab.se",
                vat_number = "SE556864274701"
            };
            post.buyer_information = new PartyInformation
            {
                person_id = "445566-8877",
                person_name = "Kundnamn",
                address_line_1 = "Address rad 1",
                address_line_2 = "Address rad 2",
                //address_line_3 = "",
                postcode = "Postkod",
                city_name = "Stad",
                country_name = "Land",
                country_code = "SE",
                //state_code = "",
                contact_name = "Kontaktnamn",
                phone_number = "Telefonnummer",
                email = "nisse@annytab.se",
                vat_number = "VAT-nummer"
            };
            //post.payment_options = new List<PaymentOption>
            //{
            //    new PaymentOption
            //    {
            //        name = "IBAN",
            //        account_reference = "SE4680000816959239073274",
            //        bank_identifier_code = "SWEDSESS",
            //        bank_name = "Swedbank AB",
            //        bank_country_code = "SE"
            //    },
            //    new PaymentOption
            //    {
            //        name = "BG",
            //        account_reference = "7893514",
            //        bank_identifier_code = "BGABSESS",
            //        bank_name = "Bankgirocentralen BGC AB",
            //        bank_country_code = "SE"
            //    },
            //    new PaymentOption
            //    {
            //        name = "SWISH",
            //        account_reference = "1235370366",
            //        bank_identifier_code = "SWEDSESS",
            //        bank_name = "Swedbank AB",
            //        bank_country_code = "SE"
            //    },
            //    new PaymentOption
            //    {
            //        name = "PAYPAL.ME",
            //        account_reference = "https://www.paypal.me/annytab",
            //        bank_identifier_code = "",
            //        bank_name = "PayPal",
            //        bank_country_code = "US"
            //    }
            //};
            post.product_rows = new List<ProductRow>
            {
                new ProductRow
                {
                    //product_code = "GiB",
                    //manufacturer_code = "GiB",
                    //gtin = "",
                    product_name = "Gibibytes",
                    vat_rate = 0.25M,
                    quantity = 5M,
                    unit_code = "PIEC",
                    unit_price = 100.45M,
                    subrows = new List<ProductRow>()
                },
                new ProductRow
                {
                    //product_code = "XX-1450",
                    //manufacturer_code = "T2223445566",
                    gtin = "00000047",
                    product_name = "Verktygslåda",
                    vat_rate = 0.25M,
                    quantity = 3.55M,
                    unit_code = "st",
                    unit_price = 5999.77M,
                    subrows = null         
                }
            };
            //post.vat_specification = new List<VatSpecification>
            //{
            //    new VatSpecification
            //    {
            //        tax_rate = 0.25M,
            //        taxable_amount = 0M,
            //        tax_amount = 0M,
            //    }
            //};
            post.subtotal = 10000M;
            post.vat_total = 2500M;
            post.rounding = 0M;
            post.total = 12500M;
            post.paid_amount = 0M;
            post.balance_due = 12500M;

            // Write the  to a file
            File.WriteAllText(directory + "\\" + post.id + ".json", JsonConvert.SerializeObject(post));

        } // End of the CreateOrder method

        /// <summary>
        /// Create an invoice
        /// </summary>
        [TestMethod]
        public void CreateInvoice()
        {
            // Make sure that the directory exists
            string directory = Directory.GetCurrentDirectory() + "\\Invoices";
            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            // Create a request for quotation
            AnnytabDoxTrade post = new AnnytabDoxTrade();
            post.id = Guid.NewGuid().ToString();
            post.document_type = "invoice";
            post.payment_reference = "D-2000";
            post.issue_date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            post.due_date = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            post.delivery_date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            post.seller_references = new Dictionary<string, string>();
            post.seller_references.Add("supplier_id", "test");
            //post.seller_references.Add("quotation_id", "500");
            //post.seller_references.Add("order_id", "");
            //post.seller_references.Add("invoice_id", "");
            post.buyer_references = new Dictionary<string, string>();
            post.buyer_references.Add("customer_id", "F557882");
            //post.buyer_references.Add("request_for_quotation_id", "784da920-a5e2-4ad0-afea-6961b9885554");
            //post.buyer_references.Add("order_id", "");
            post.terms_of_delivery = "CIF";
            post.terms_of_payment = "Net 30";
            post.mode_of_delivery = "UPS";
            post.total_weight_kg = 10M;
            post.penalty_interest = 0.20M;
            post.currency_code = "USD";
            //post.vat_country_code = invoice.vat_country_code;
            //post.vat_state_code = invoice.vat_state_code;
            post.comment = "Vänligen ange fakturanummer vid betalning.";
            post.seller_information = new PartyInformation()
            {
                person_id = "778899-7447",
                person_name = "Supplier Inc",
                address_line_1 = "Abroad way 55",
                //address_line_2 = "",
                //address_line_3 = "",
                postcode = "CA90009",
                city_name = "San Francisco",
                country_name = "USA",
                country_code = "US",
                state_code = "CA",
                contact_name = "Brenda Meatloaf",
                phone_number = "+8800004545",
                email = "olle@annytab.se",
                vat_number = "778899-7447"
            };
            post.buyer_information = new PartyInformation
            {
                person_id = "556864-2747",
                person_name = "A Name Not Yet Taken AB",
                address_line_1 = "Skonertgatan 12",
                address_line_2 = "Kronobränneriet",
                address_line_3 = "",
                postcode = "30238",
                city_name = "Halmstad",
                country_name = "Sweden",
                country_code = "SE",
                state_code = "",
                contact_name = "Fredrik Stigsson",
                phone_number = "",
                email = "dox@annytab.se",
                vat_number = "SE556864274701"
            };
            post.payment_options = new List<PaymentOption>
            {
                new PaymentOption
                {
                    name = "IBAN",
                    account_reference = "SE4680000816959239073274",
                    bank_identifier_code = "SWEDSESS",
                    bank_name = "Swedbank AB",
                    bank_country_code = "SE"
                },
                new PaymentOption
                {
                    name = "BG",
                    account_reference = "7893514",
                    bank_identifier_code = "BGABSESS",
                    bank_name = "Bankgirocentralen BGC AB",
                    bank_country_code = "SE"
                },
                new PaymentOption
                {
                    name = "BANK",
                    account_reference = "816959239073274",
                    bank_identifier_code = "SWEDSESS",
                    bank_name = "Swedbank AB",
                    bank_country_code = "SE"
                },
                new PaymentOption
                {
                    name = "SWISH",
                    account_reference = "1235370366",
                    bank_identifier_code = "SWEDSESS",
                    bank_name = "Swedbank AB",
                    bank_country_code = "SE"
                },
                new PaymentOption
                {
                    name = "PAYPAL.ME",
                    account_reference = "https://www.paypal.me/annytab",
                    bank_identifier_code = "",
                    bank_name = "PayPal",
                    bank_country_code = "US"
                }
            };
            post.product_rows = new List<ProductRow>
            {
                new ProductRow
                {
                    product_code = "",
                    //manufacturer_code = "T2223445566",
                    //gtin = "888090909",
                    product_name = "Telefoni",
                    vat_rate = 0M,
                    quantity = 10.46M,
                    unit_code = "h",
                    unit_price = 11.44M,
                    subrows = null
                },
                new ProductRow
                {
                    product_code = null,
                    manufacturer_code = "TT99878",
                    //gtin = "888090909",
                    product_name = "Dator",
                    vat_rate = 0M,
                    quantity = 1M,
                    unit_code = "st",
                    unit_price = 499.99M,
                    subrows = null
                },
                new ProductRow
                {
                    product_code = null,
                    //manufacturer_code = "TT99878",
                    gtin = "0000001",
                    product_name = "Musslor",
                    vat_rate = 0M,
                    quantity = 2M,
                    unit_code = "kg",
                    unit_price = 2M,
                    subrows = null
                }
            };
            post.vat_specification = new List<VatSpecification>
            {
                new VatSpecification
                {
                    tax_rate = 0.25M,
                    taxable_amount = 0M,
                    tax_amount = 0M,
                }
            };
            post.subtotal = 624M;
            post.vat_total = 0M;
            post.rounding = 0.3476M;
            post.total = 624M;
            post.paid_amount = 0M;
            post.balance_due = 624M;

            // Write the  to a file
            File.WriteAllText(directory + "\\" + post.id + ".json", JsonConvert.SerializeObject(post));

        } // End of the CreateInvoice method

        /// <summary>
        /// Create a credit invoice
        /// </summary>
        [TestMethod]
        public void CreateCreditInvoice()
        {
            // Make sure that the directory exists
            string directory = Directory.GetCurrentDirectory() + "\\CreditInvoices";
            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            // Create a request for quotation
            AnnytabDoxTrade post = new AnnytabDoxTrade();
            post.id = Guid.NewGuid().ToString();
            post.document_type = "credit_invoice";
            post.payment_reference = "D-1514";
            post.issue_date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            post.due_date = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            post.delivery_date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            post.seller_references = new Dictionary<string, string>();
            post.seller_references.Add("supplier_id", "test");
            //post.seller_references.Add("quotation_id", "500");
            //post.seller_references.Add("order_id", "");
            //post.seller_references.Add("invoice_id", "");
            post.buyer_references = new Dictionary<string, string>();
            post.buyer_references.Add("customer_id", "F557882");
            //post.buyer_references.Add("request_for_quotation_id", "784da920-a5e2-4ad0-afea-6961b9885554");
            //post.buyer_references.Add("order_id", "");
            post.terms_of_delivery = "CIF";
            post.terms_of_payment = "Net 30";
            post.mode_of_delivery = "UPS";
            post.total_weight_kg = 10M;
            post.penalty_interest = 0.20M;
            post.currency_code = "USD";
            //post.vat_country_code = invoice.vat_country_code;
            //post.vat_state_code = invoice.vat_state_code;
            post.comment = "Vänligen ange fakturanummer vid betalning.";
            post.seller_information = new PartyInformation()
            {
                person_id = "778899-7447",
                person_name = "Supplier Inc",
                address_line_1 = "Abroad way 55",
                //address_line_2 = "",
                //address_line_3 = "",
                postcode = "CA90009",
                city_name = "San Francisco",
                country_name = "USA",
                country_code = "US",
                state_code = "CA",
                contact_name = "Brenda Meatloaf",
                phone_number = "+8800004545",
                email = "invoice@annytab.se",
                vat_number = "778899-7447"
            };
            post.buyer_information = new PartyInformation
            {
                person_id = "556864-2747",
                person_name = "A Name Not Yet Taken AB",
                address_line_1 = "Skonertgatan 12",
                address_line_2 = "Kronobränneriet",
                address_line_3 = "",
                postcode = "30238",
                city_name = "Halmstad",
                country_name = "Sweden",
                country_code = "SE",
                state_code = "",
                contact_name = "Fredrik Stigsson",
                phone_number = "",
                email = "dox@annytab.se",
                vat_number = "SE556864274701"
            };
            post.payment_options = new List<PaymentOption>
            {
                new PaymentOption
                {
                    name = "IBAN",
                    account_reference = "SE4680000816959239073274",
                    bank_identifier_code = "SWEDSESS",
                    bank_name = "Swedbank AB",
                    bank_country_code = "SE"
                },
                new PaymentOption
                {
                    name = "BG",
                    account_reference = "7893514",
                    bank_identifier_code = "BGABSESS",
                    bank_name = "Bankgirocentralen BGC AB",
                    bank_country_code = "SE"
                },
                new PaymentOption
                {
                    name = "SWISH",
                    account_reference = "1235370366",
                    bank_identifier_code = "SWEDSESS",
                    bank_name = "Swedbank AB",
                    bank_country_code = "SE"
                },
                new PaymentOption
                {
                    name = "PAYPAL.ME",
                    account_reference = "https://www.paypal.me/annytab",
                    bank_identifier_code = "",
                    bank_name = "PayPal",
                    bank_country_code = "US"
                }
            };
            post.product_rows = new List<ProductRow>
            {
                new ProductRow
                {
                    product_code = "US-law",
                    //manufacturer_code = "T2223445566",
                    //gtin = "888090909",
                    product_name = "Law consulting package",
                    vat_rate = 0M,
                    quantity = -60M,
                    unit_code = "h",
                    unit_price = 200M,
                    subrows = new List<ProductRow>
                    {
                        new ProductRow
                        {
                            product_code = "USLAW-1",
                            //manufacturer_code = "T2223445567",
                            //gtin = "",
                            product_name = "Trade",
                            vat_rate = 0M,
                            quantity = -20M,
                            unit_code = "h",
                            unit_price = 0M,
                            subrows = null
                        },
                        new ProductRow
                        {
                            product_code = "USLAW-1",
                            //manufacturer_code = "T2223445568",
                            //gtin = "",
                            product_name = "Real estate",
                            vat_rate = 0M,
                            quantity = -4M,
                            unit_code = "h",
                            unit_price = 0M,
                            subrows = null
                        }
                    }
                }
            };
            post.vat_specification = new List<VatSpecification>
            {
                new VatSpecification
                {
                    tax_rate = 0.25M,
                    taxable_amount = 0M,
                    tax_amount = 0M,
                }
            };
            post.subtotal = -12000M;
            post.vat_total = 0M;
            post.rounding = 0M;
            post.total = -12000M;
            post.paid_amount = 0M;
            post.balance_due = -12000M;

            // Write the  to a file
            File.WriteAllText(directory + "\\" + post.id + ".json", JsonConvert.SerializeObject(post));

        } // End of the CreateCreditInvoice method

    } // End of the class

} // End of the namespace