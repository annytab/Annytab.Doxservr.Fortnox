using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnidecodeSharpFork;
using Annytab.Dox.Standards.V1;
using Annytab.Fortnox.Client.V3;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This class includes handy methods
    /// </summary>
    public static class CommonTools
    {
        /// <summary>
        /// Get the extensions of a filename as a string
        /// </summary>
        /// <param name="filename">A filename with extensions</param>
        /// <returns>A string with extensions, dot (.) is included</returns>
        public static string GetExtensions(string filename)
        {
            // Create the extension to return
            string extensions = "";

            Int32 index = filename.IndexOf(".");

            if (index > -1)
            {
                extensions = filename.Substring(index);
            }

            // Return extensions
            return extensions;

        } // End of the GetExtensions method

        /// <summary>
        /// Get customer vat type
        /// </summary>
        public static string GetCustomerVatType(Customer customer, DefaultValues default_values)
        {
            // Customer country codes
            string invoice_country_code = string.IsNullOrEmpty(customer.CountryCode) == false ? customer.CountryCode : "SE";
            string delivery_country_code = string.IsNullOrEmpty(customer.DeliveryCountryCode) == false ? customer.DeliveryCountryCode : invoice_country_code;

            // Create the vat type to return
            string vat_type = "";

            if (invoice_country_code == "SE" && delivery_country_code == "SE")
            {
                if(customer.VATType != "SEREVERSEDVAT")
                {
                    vat_type = "SEREVERSEDVAT";
                }
                else if (customer.VATType == "SEVAT")
                {
                    vat_type = "SEVAT";
                }
                else
                {
                    vat_type = default_values.SalesVatTypeSE;
                }            
            }
            else if (customer.VATNumber != null && IsCountryCodeEU(invoice_country_code) && IsCountryCodeEU(delivery_country_code))
            {
                vat_type = "EUREVERSEDVAT";
            }
            else if(customer.VATNumber == null && IsCountryCodeEU(invoice_country_code) && IsCountryCodeEU(delivery_country_code))
            {
                vat_type = "EUVAT";
            }
            else
            {
                vat_type = "EXPORT";
            }

            // Return the vat type
            return vat_type;

        } // End of the GetCustomerVatType method

        /// <summary>
        /// Get a default sales account for an article
        /// </summary>
        /// <param name="vat_rate"></param>
        /// <param name="default_values"></param>
        /// <returns></returns>
        public static string GetArticleSalesAccount(decimal? vat_rate, DefaultValues default_values)
        {
            // Create the account to return
            string account = default_values.SalesAccountSE0;

            if (vat_rate == 0.25M)
            {
                account = default_values.SalesAccountSE25;
            }
            else if (vat_rate == 0.12M)
            {
                account = default_values.SalesAccountSE12;
            }
            else if (vat_rate == 0.06M)
            {
                account = default_values.SalesAccountSE6;
            }

            // Return the account
            return account;

        } // End of the GetArticleSalesAccount method

        /// <summary>
        /// Check if the country code represents a EU-country
        /// </summary>
        public static bool IsCountryCodeEU(string country_code)
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
        public static string ConvertToAlphanumeric(string word)
        {
            // Convert the word to lower case letters
            //word = word.ToLower();

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

            // Return the word
            return word;

        } // End of the ConvertToAlphanumeric method

        /// <summary>
        /// Get the encoding from a charset string
        /// </summary>
        public static Encoding GetEncoding (string charset, Encoding fallback_encoding)
        {
            // Create the encoding to return
            Encoding encoding = fallback_encoding;

            // Convert the charset to lower case
            charset = charset.ToLower();

            if(charset == "ascii")
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

            // Return the encoding
            return encoding;

        } // End of the GetEncoding method

        /// <summary>
        /// Get a vat specification from product rows
        /// </summary>
        public static IList<VatSpecification> GetVatSpecification(IList<ProductRow> rows)
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
            foreach(KeyValuePair<decimal?, VatSpecification> row in vat_rows)
            {
                vat_specification.Add(row.Value);
            }

            // Return the list
            return vat_specification;

        } // End of the GetVatSpecification method

    } // End of the class

} // End of the namespace