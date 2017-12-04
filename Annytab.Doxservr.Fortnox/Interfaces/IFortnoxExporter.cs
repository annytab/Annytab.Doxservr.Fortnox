using System.Net.Http;
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
        Task<OffersRoot> GetOffers(HttpClient client);
        Task<AnnytabDoxTradeRoot> GetOffer(HttpClient client, string id);
        Task<OrdersRoot> GetOrders(HttpClient client);
        Task<IList<AnnytabDoxTradeRoot>> GetOrder(HttpClient client, string id);
        Task<InvoicesRoot> GetInvoices(HttpClient client);
        Task<AnnytabDoxTradeRoot> GetInvoice(HttpClient client, string id);

    } // End of the class

} // End of the namespace