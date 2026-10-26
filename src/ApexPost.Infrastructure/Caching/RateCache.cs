using ApexPost.Application;
using ApexPost.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApexPost.Infrastructure;

public sealed class RateCache(IRepository<ShipmentRate> repository) : IRateCache
{
    private IReadOnlyList<ShipmentRate>? _cache;
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async ValueTask<IReadOnlyList<ShipmentRate>> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        if (_cache is not null) return _cache;
        await _gate.WaitAsync(cancellationToken);
        try { _cache ??= await repository.GetAllAsync(cancellationToken); return _cache; }
        finally { _gate.Release(); }
    }
}
