using System.Net.Http;
using System.Threading.Tasks;
using Annytab.Dox.Standards.V1;
using Annytab.Fortnox.Client.V3;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This interface represent a fortnox importer
    /// </summary>
    public interface IFortnoxImporter
    {
        Task<TermsOfDeliveryRoot> AddTermsOfDelivery(HttpClient client, string term_of_delivery);
        Task<TermsOfPaymentRoot> AddTermsOfPayment(HttpClient client, string term_of_payment);
        Task<WayOfDeliveryRoot> AddWayOfDelivery(HttpClient client, string way_of_delivery);
        Task<CurrencyRoot> AddCurrency(HttpClient client, string currency_code);
        Task<UnitRoot> AddUnit(HttpClient client, string unit_code);
        Task<PriceListRoot> AddPriceList(HttpClient client, string code);
        Task<AccountRoot> AddAccount(HttpClient client, string account_number);
        Task<ArticleRoot> AddArticle(HttpClient client, ProductRow row);
        Task<CustomerRoot> UpsertCustomer(HttpClient client, string dox_email, AnnytabDoxTrade doc);
        Task<SupplierRoot> UpsertSupplier(HttpClient client, string dox_email, AnnytabDoxTrade doc);
        Task UpsertCurrencies(HttpClient client, FixerRates fixer_rates);
        Task<OfferRoot> AddOffer(HttpClient client, string dox_email, AnnytabDoxTrade doc);
        Task<OrderRoot> AddOrder(HttpClient client, string dox_email, AnnytabDoxTrade doc);
        Task<SupplierInvoiceRoot> AddSupplierInvoice(HttpClient client, string dox_email, AnnytabDoxTrade doc);

    } // End of the interface

} // End of the namespace