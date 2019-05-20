using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Annytab.Doxservr.Fortnox.Functions
{
    /// <summary>
    /// This interface represent a blob logger repository
    /// </summary>
    public interface IBlobLogger
    {
        Task LogDebug(string blob_name, string message);
        Task LogInformation(string blob_name, string message);
        Task LogWarning(string blob_name, string message);
        Task LogError(string blob_name, string message);
        Task GetLogAsStream(string blob_name, Stream stream);
        Task<string> GetLogAsString(string blob_name);
        IList<BlobLog> GetBlobLogs(string email);
        Task<bool> LogExists(string blob_name);
        Task Delete(string blob_name);
        Task DeleteByLastModifiedDate(Int32 days);

    } // End of the interface

} // End of the namespace