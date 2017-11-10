using System.Threading.Tasks;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This interface represent a worker repository
    /// </summary>
    public interface IWorkerRepository
    {
        Task Run();

    } // End of the interface

} // End of the namespace