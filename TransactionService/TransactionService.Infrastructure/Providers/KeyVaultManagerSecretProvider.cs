using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using TransactionService.Application.Abstractions.Secrets;
using TransactionService.Infrastructure.Caching;
using TransactionService.Infrastructure.Configuration;

namespace TransactionService.Infrastructure.Providers
{
    public sealed class KeyVaultManagerSecretProvider : ISecretProvider
    {
        private readonly SecretClient _client;
        private readonly InMemorySecretCache _cache;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public KeyVaultManagerSecretProvider(KeyVaultOptions options, InMemorySecretCache cache)
        {
            _client = new SecretClient(new Uri(options.VaultUrl), new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeAzureCliCredential = false,
                ExcludeAzurePowerShellCredential = true
            }));
            _cache = cache;
        }

        public async Task<IReadOnlyDictionary<string, string>> GetAllSecretsAsync(CancellationToken cancellationToken = default)
        {
            var cached = _cache.Get();
            if (cached is not null)
                return cached;

            await _lock.WaitAsync(cancellationToken);

            try
            {
                cached = _cache.Get();
                if (cached is not null)
                    return cached;

                var secrets = new Dictionary<string, string>();

                await foreach (var secretProperties in _client.GetPropertiesOfSecretsAsync(cancellationToken))
                {
                    var secret = await _client.GetSecretAsync(secretProperties.Name, cancellationToken: cancellationToken);
                    secrets[secret.Value.Name] = secret.Value.Value;
                }

                _cache.Set(secrets);

                return secrets;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<string?> GetSecretAsync(string secretKey, CancellationToken cancellationToken = default)
        {
            var all = await GetAllSecretsAsync(cancellationToken);
            all.TryGetValue(secretKey, out var value);
            return value;
        }
    }
}