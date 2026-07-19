namespace TransactionService.Infrastructure.Configuration
{
    public class CosmosOptions
    {
        public const string SectionName = "Cosmos";

        public string RechargesContainerName { get; set; } = "Recharges";
        public string TransactionsContainerName { get; set; } = "Transactions";
    }
}
