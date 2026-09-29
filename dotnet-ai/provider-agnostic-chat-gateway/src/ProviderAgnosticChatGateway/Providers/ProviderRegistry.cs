namespace ProviderAgnosticChatGateway.Providers;

/// <summary>
/// Stores the AI providers that the gateway is explicitly configured to use.
/// Callers select a registered name only; they never supply provider endpoints,
/// models or credentials in an HTTP request.
/// </summary>
public sealed class ProviderRegistry : IDisposable
{
    private readonly Dictionary<string, ProviderDescriptor> _providers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly bool _allowProviderSelection;

    public ProviderRegistry(
        string defaultProvider,
        bool allowProviderSelection = true)
    {
        if (string.IsNullOrWhiteSpace(defaultProvider))
        {
            throw new ArgumentException(
                "A default provider name is required.",
                nameof(defaultProvider));
        }

        DefaultProvider = defaultProvider.Trim();
        _allowProviderSelection = allowProviderSelection;
    }

    public string DefaultProvider { get; }

    public bool AllowProviderSelection => _allowProviderSelection;

    public IReadOnlyCollection<string> ProviderNames =>
        _providers.Keys.ToArray();

    public IReadOnlyCollection<ProviderDescriptor> Providers =>
        _providers.Values.ToArray();

    public void Add(ProviderDescriptor provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (string.IsNullOrWhiteSpace(provider.Name))
        {
            throw new ArgumentException(
                "Provider name is required.",
                nameof(provider));
        }

        string normalizedName = provider.Name.Trim();
        ProviderDescriptor normalizedProvider =
            provider with { Name = normalizedName };

        if (!_providers.TryAdd(normalizedName, normalizedProvider))
        {
            throw new InvalidOperationException(
                $"Provider '{normalizedName}' is already registered.");
        }
    }

    public ProviderDescriptor Resolve(string? requestedProvider = null)
    {
        string selectedProvider =
            string.IsNullOrWhiteSpace(requestedProvider)
                ? DefaultProvider
                : requestedProvider.Trim();

        if (!_allowProviderSelection &&
            !selectedProvider.Equals(
                DefaultProvider,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Provider selection is disabled. " +
                $"The gateway is configured to use '{DefaultProvider}'.");
        }

        if (!_providers.TryGetValue(
                selectedProvider,
                out ProviderDescriptor? provider))
        {
            throw new KeyNotFoundException(
                $"Provider '{selectedProvider}' is not registered.");
        }

        return provider;
    }

    public void Dispose()
    {
        foreach (ProviderDescriptor provider in _providers.Values)
        {
            provider.Client.Dispose();
        }

        _providers.Clear();
    }
}
