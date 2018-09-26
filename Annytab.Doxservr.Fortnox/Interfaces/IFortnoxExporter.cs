using System.Collections.Generic;
using System.Threading.Tasks;
using Annytab.Fortnox.Client.V3;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This interface represent a fortnox exporter
    /// </summary>
    public interface IFortnoxExporter
    {
        Task<Dictionary<string, string>> GetLabels();
        Task<CompanySettingsRoot> GetCompanySettings();
        Task<OffersRoot> GetOffers();
        Task<AnnytabDoxTradeRoot> GetOffer(string id);
        Task<OrdersRoot> GetOrders();
        Task<IList<AnnytabDoxTradeRoot>> GetOrder(string id);
        Task<InvoicesRoot> GetInvoices();
        Task<AnnytabDoxTradeRoot> GetInvoice(string id);

    } // End of the class

} // End of the namespace