# a-doxservr-fortnox
This is a console application that synchronizes electronic documents between Doxservr 
([www.doxservr.com](https://www.doxservr.com)) and Fortnox ([www.fortnox.se](https://www.fortnox.se)). 
This application imports offers, orders, invoices and credit invoices to Fortnox from Doxservr and sends offers, orders, purchase orders, 
invoices and credit invoices from Fortnox through Doxservr to receivers. This application only handles electronic documents 
that has been createad with the Annytab Dox Trade standard.

## Getting started
Decide on a directory that should be used to contain the appsettings.json file and to which files and logs should be 
saved. The default directory is "D:\\home\\AnnytabDoxservrFortnox" and this directory will work for a web job on 
Azure. You can choose any directory to contain files for the application but you have to add the directory path 
as an argument when you start the program if it is different from the default directory. 

Copy/Paste the appsettings.template.json file and rename the new file to appsettings.json. Save the created appsettings.json file in the 
last folder of your selected directory. You need to edit the settings in the appsettings.json file before you run the program. 
You can optionally add a appsettings.development.json file to the directory folder if you are testing the program 
locally, you need to manually add the AccessToken (FortnoxOptions) to the appsettings.development.json, the AccessToken is 
always written to the appsettings.json file only.

### DoxservrOptions
- **ApiHost:** The doxservr base url, https://www.doxservr.com or https://www.doxservr.se.
- **ApiEmail:** The email address for your doxservr account.
- **ApiPassword:** The api password for your doxervr account, you find this in your member details.

### FortnoxOptions
- **ClientId:** This is the id for a Fortnox integration, it can be used to search for integrations in Fortnox.
- **ClientSecret:** The client secret is used together with the access token to authenticate each request to Fortnox.
- **AuthorizationCode:** You get this code when you connect this integration to your Fortnox account. The authorization code i
s used once to get the access token.
- **AccessToken:** You should leave this setting empty (""). The access token is set by the program if you have specified a ClientSecret and a 
AuthorizationCode (API-kod).

### DefaultValues
- **PriceList:** The default Fortnox price list you use for new articles and new customers that are created from this program.
- **PenaltyInterest:** The penalty interest expressed on exported offers, orders and invoices. 10 % is expressed as 0.1 (decimal).
- **SalesVatTypeSE:** The default vat type for customers can be SEVAT or SEREVERSEDVAT, used when a new customer is created.
- **SalesAccountSE25:** Account for sales to Swedish customers at a vat rate of 25 %. Must exist and be active in Fortnox, is added to new articles.
- **SalesAccountSE12:** Account for sales to Swedish customers at a vat rate of 12 %. Must exist and be active in Fortnox, is added to new articles.
- **SalesAccountSE6:** Account for sales to Swedish customers at a vat rate of 6 %. Must exist and be active in Fortnox, is added to new articles.
- **SalesAccountSE0:** Account for sales to Swedish customers at a vat rate of 0 %. Must exist and be active in Fortnox, is added to new articles.
- **SalesAccountSEREVERSEDVAT:** Account for sales to Swedish customers when the customer should report value added tax (reversed vat). Must exist and be active in Fortnox, is added to new articles.
- **SalesAccountEUVAT:** Account for sales to customers in other EU-countries than Sweden with value added tax. Must exist and be active in Fortnox, is added to new articles.
- **SalesAccountEUREVERSEDVAT:** Account for sales to customers in other EU-countries than Sweden without VAT (reversed vat). Must exist and be active in Fortnox, is added to new articles.
- **SalesAccountEXPORT:** Account for sales to customers outside of EU, no value added tax. Must exist and be active in Fortnox, is added to new articles.
- **PurchaseAccount:** Account for purchases (expenses). Must exist and be active in Fortnox, is added to new articles.
- **OnlyAllowTrustedSenders:** Set this value to true if you only want to allow documents from trusted senders to be imported to Fortnox. You can add email addresses to a list of trusted email addresses in Fortnox (Inställningar/Arkivplats).
- **DoxservrGibPerInvoice:** If you want to refill your doxservr account balance automatically, enter the number of gibibytes that you want to add with each invoice.
- **DoxservrMinimumBytes:** If you want to refill your doxservr account balance automatically, enter the minimum number of bytes that you allow your account balance to have before refilling your account balance.

## Run the program
The purpose of this program is that it should run on a schedule, triggered from a task scheduler program or from Azure. This program 
should run on only one instance, a triggered Azure web job will run on just one instance selected at random. The 
run.cmd file and the settings.job file are used if this program is running as a web job on Azure. The settings.job file includes 
a cron expression that tells Azure when the web job should be triggered.

You can optionally pass a directory path as an argument when you run the program, the default directory is "D:\\home\\AnnytabDoxservrFortnox" and 
this directory will be used if no directory path is specified.

> Command: dotnet Annytab.Doxservr.Fortnox.dll "D:\\home\\AnnytabDoxservrFortnox"

This program saves files to the choosen directory folder and folders under this folder. Files that are imported and exported are saved 
to the Files folder and loggfiles are saved to the Logs folder. Logs are important to get information about program 
excecution and errors. Saved files are important to recover from errors or to correct for errors.

You need to publish the project to a folder in order to get all the files that is needed for the program to run, files in the 
bin/Debug or bin/Release is just a subset of the files needed. In Visual Studio click on Publish Annytab.Doxservr.Fortnox 
from the Build menu to publish the project to a folder.

## Import files to Fortnox
This program gets files that not have been downloaded (Status: 0) from your doxservr account and saves files 
that have a standard name of "Annytab Dox Trade v1" to the Files folder, the meta information for these files are 
saved to the Files/Meta folder.

The application adds the price list and accounts specified as default values if they does not exist in Fortnox and 
updates currencies and currency rates. Currencies and currency rates are downloaded from [Fixer.io](http://fixer.io), 
currency rates are not updated every day.

This program loops all meta files and import files to Fortnox according to the document type for each file:

- request_for_quotation: Imported as an Offer.
- quotation: Not imported.
- order: Imported as an Order.
- order_confirmation: Not imported.
- invoice: Imported as SupplierInvoice.
- credit_invoice: Imported as SupplierInvoice (Negative quantites makes it a credit invoice).

A customer will be added or updated and a supplier will be added or updated. Article, TermsOfDelivery, TermsOfPayment, WayOfDelivery, 
Currency and Unit will be added if they don't exist. TermsOfDelivery, TermsOfPayment, WayOfDelivery, Currency, PriceList should have 
an identifier (Code) in uppercase and this application converts these identifiers to uppercase. Unit should have an identifier (Code) in 
lowercase and this application convert this identifier to lowercase. ArticleNumber can have a mix of uppercase and lowercase letters and 
the ArticleNumber is case sensitive. Identifiers can only be in alphanumeric characters and this application handles this by converting 
not supported characters to alphanumeric characters.

Default values for accounts that are set when an article is added can be changed in Fortnox. 
The account numbers that are set for an article is added to rows in offers, orders and supplier invoices. If you add a 
default account to a customer or a supplier, this account will override the account set for an article.

To handle accounting of reversed VAT in supplier invoices my recommendation is to add accounting templates for each case 
that removes the total amount with an adjustment account.

**Example: Purchase services from another EU-country than Sweden, 25 %**
- 4599 Justeringskonto, omvänd moms: -{$total}
- 4535 Inköp av tjänster från annat EU-land, 25 %: {$total}
- 2614 Utgående moms omvänd skattskyldighet, 25 %: {$total}*-0.25
- 2645 Beräknad ingående moms på förvärv från utlandet: {$total}*0.25

This method means that you don't need to adjust the account for each article and also that each article can have 
different accounts for expenses, the VAT-report will also be correct.

Each file is moved to the Imported folder if everything goes well and no errors are encountered. Meta files are moved to 
the Files/Meta/Imported folder and files are moved to the Files/Imported folder. If you only allow trusted senders, no 
documents from untrusted senders will be imported to Fortnox.

## Export files from Fortnox
You can export documents from Fortnox to customers and suppliers through Doxservr. Exported documents are sent to the email that 
is set for the customer or supplier in Fortnox. Only documents that not has been marked as Sent can be exported from Fortnox, a document 
will be marked as Sent if it is printed or sent to an email. You can export documents by adding a label to the document that you want 
to export. 

Add a "a-dox-trade-v1" label to a offer, order, invoice or credit invoice if you want to export it. You can also 
add a "a-dox-trade-v1-po" label to an order if you want to create purchase orders to suppliers from the order. Purchase orders 
are created by looping through the rows in the order, get the article and find the supplier for that article. A purchase order 
is created to each identified supplier in the order.

A file that you want to export are created as an "Annytab Dox Trade" document and sent through Doxserver to the reciever. 
Exported files are saved to the Files/Exported folder with a name according to the document type and the id. Each purchase 
order is named as purchase_order_{SupplierNumber}_{OrderNumber}. Exported documents will be marked as Sent in Fortnox to not be 
exported multiple times.




