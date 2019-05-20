using System.Threading.Tasks;
using Annytab.Dox.Standards.V1;
using Annytab.Fortnox.Client.V3;

namespace Annytab.Doxservr.Fortnox.App
{
    /// <summary>
    /// This interface represent a fortnox importer
    /// </summary>
    public interface IFortnoxImporter
    {
        Task<TermsOfDeliveryRoot> AddTermsOfDelivery(string term_of_delivery);
        Task<TermsOfPaymentRoot> AddTermsOfPayment(string term_of_payment);
        Task<WayOfDeliveryRoot> AddWayOfDelivery(string way_of_delivery);
        Task<CurrencyRoot> AddCurrency(string currency_code);
        Task<UnitRoot> AddUnit(string unit_code);
        Task<PriceListRoot> AddPriceList(string code);
        Task<AccountRoot> AddAccount(string account_number);
        Task<ArticleRoot> AddArticle(ProductRow row);
        Task<CustomerRoot> UpsertCustomer(string dox_email, AnnytabDoxTrade doc);
        Task<SupplierRoot> UpsertSupplier(string dox_email, AnnytabDoxTrade doc);
        Task UpsertCurrencies(FixerRates fixer_rates);
        Task<EmailSendersRoot> GetTrustedEmailSenders();
        Task<OfferRoot> AddOffer(string dox_email, AnnytabDoxTrade doc);
        Task<OrderRoot> AddOrder(string dox_email, AnnytabDoxTrade doc);
        Task<SupplierInvoiceRoot> AddSupplierInvoice(string dox_email, AnnytabDoxTrade doc);

    } // End of the interface

} // End of the namespace