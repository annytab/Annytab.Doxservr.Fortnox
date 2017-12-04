using System.Threading.Tasks;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This interface represents a worker repository
    /// </summary>
    public interface IWorkerRepository
    {
        Task Run(string directory);

    } // End of the interface

} // End of the namespace