using System.Threading.Tasks;
using Annytab.Doxservr.Client.V1;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This interface represent a fixer client
    /// </summary>
    public interface IFixerClient
    {
        Task<DoxservrResponse<FixerRates>> UpdateCurrencyRates(string directory);

    } // End of the interface

} // End of the namespace