using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Annytab.Fortnox.Client.V3;

namespace Annytab.Doxservr.Fortnox
{
    /// <summary>
    /// This class represent a worker repository
    /// </summary>
    public class WorkerRepository : IWorkerRepository
    {
        #region Variables

        private readonly ILogger logger;
        private readonly IFortnoxRepository fortnox_repository;
        
        #endregion

        #region Constructors

        /// <summary>
        /// Create a new repository
        /// </summary>
        public WorkerRepository(ILogger<IWorkerRepository> logger, IFortnoxRepository fortnox_repository)
        {
            // Set values for instance variables
            this.logger = logger;
            this.fortnox_repository = fortnox_repository;

        } // End of the constructor

        #endregion

        #region Run methods

        /// <summary>
        /// Do the work
        /// </summary>
        public async Task Run()
        {
            // Log the start
            this.logger.LogInformation("Application is starting!");

            // Get a fortnox client
            HttpClient client = this.fortnox_repository.GetClient();

            // Get a list with articles
            //ArticlesRoot post = await this.fortnox_repository.Get<ArticlesRoot>(client, "articles?sortby=articlenumber&sortorder=ascending&limit=1&page=2");

            // Get an article
            //ArticleRoot post = await this.fortnox_repository.Get<ArticleRoot>(client, "articles/5");

            // Create an article
            //ArticleRoot post = new ArticleRoot
            //{
            //    Article = new Article
            //    {
            //        ArticleNumber = "TEST2",
            //        Description = "Testar en artikel"
            //    }
            //};
            //post = await this.fortnox_repository.Add<ArticleRoot>(client, post, "articles");

            // Get a customer
            //CustomerRoot post = await GetCustomer(client, "5"); Guid.NewGuid().ToString()

            // Create a customer
            //CustomerRoot post = new CustomerRoot
            //{
            //    Customer = new Customer
            //    {
            //        CustomerNumber = "3e30efc4-8437-4786-921f-8de7541baab9",
            //        Name = "TESTAR UPPDATERAD",
            //        SalesAccount = "3000",
            //        Project = ""
            //    }
            //};

            //post = await this.fortnox_repository.Add<CustomerRoot>(client, post, "customers");
            //post = await this.fortnox_repository.Update<CustomerRoot>(client, post, $"customers/{post.Customer.CustomerNumber}");
            //bool success = await this.fortnox_repository.Delete(client, $"customers/3e30efc4-8437-4786-921f-8de7541baab9");
            //CustomerRoot post = await this.fortnox_repository.Get<CustomerRoot>(client, $"customers/d0aea590-d2b8-4f70-a684-96ae1818ab02");
            //Customer post = new Customer
            //{
            //    CustomerNumber = Guid.NewGuid().ToString(),
            //    Name = "TESTAR",
            //    //Active = true,
            //    //Address1 = "Skonertgatan 12",
            //    //Address2 = "Kronobränneriet",
            //    SalesAccount = "3000",
            //    Project = ""
            //};

            //post = await CreateCustomer(client, post);

            // Get an account
            //Account post = await GetAccount(client, "1030");

            // Create an account
            //Account post = new Account
            //{
            //    Number = "9003",
            //    Description = "Test konto",
            //    Active = true,
            //    BalanceBroughtForward = null,
            //    CostCenter = "",
            //    CostCenterSettings = "ALLOWED",
            //    Project = "",
            //    ProjectSettings = "ALLOWED",
            //    SRU = "8001",
            //    TransactionInformation = "HEj",
            //    TransactionInformationSettings = "ALLOWED",
            //    VATCode = ""
            //};

            //post = await CreateAccount(client, post);

            // Log the customer
            //this.logger.LogInformation(success.ToString());
            //this.logger.LogInformation(JsonConvert.SerializeObject(post));

            // Dispose of the client
            client.Dispose();

            // Log that the application is shutting down
            this.logger.LogInformation("Application is shutting down!");

        } // End of the Run method

        #endregion

    } // End of the class

} // End of the namespace