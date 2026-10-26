using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApexPost.Application;
using ApexPost.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace ApexPost.Infrastructure;

public sealed class JsonRepository<T>(IConfiguration configuration, IHostEnvironment environment, ILogger<JsonRepository<T>> logger) : IRepository<T> where T : class, IEntity
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    private readonly string _path = Path.Combine(ResolveDataDirectory(configuration, environment), $"{typeof(T).Name}.json");
    private readonly SemaphoreSlim _gate = Gates.GetOrAdd(Path.GetFullPath(Path.Combine(ResolveDataDirectory(configuration, environment), $"{typeof(T).Name}.json")), _ => new SemaphoreSlim(1, 1));

    private static string ResolveDataDirectory(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Data:Directory"] ?? "App_Data";
        return Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_path)) return Array.Empty<T>();
            await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            return await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, cancellationToken) ?? [];
        }
        catch (JsonException ex) { logger.LogError(ex, "Invalid JSON data in {DataFile}", Path.GetFileName(_path)); throw new InvalidDataException($"The data file {Path.GetFileName(_path)} is corrupt. It was not overwritten.", ex); }
        finally { _gate.Release(); }
    }

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => (await GetAllAsync(cancellationToken)).FirstOrDefault(x => x.Id == id);
    public Task AddAsync(T entity, CancellationToken cancellationToken = default) => MutateAsync(items => { if (items.Any(x => x.Id == entity.Id)) throw new InvalidOperationException($"Duplicate ID for {typeof(T).Name}."); items.Add(entity); }, cancellationToken);
    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default) => MutateAsync(items => { var index = items.FindIndex(x => x.Id == entity.Id); if (index < 0) throw new KeyNotFoundException($"{typeof(T).Name} was not found."); items[index] = entity; }, cancellationToken);
    public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => MutateAsync(items => items.RemoveAll(x => x.Id == id), cancellationToken);

    private async Task MutateAsync(Action<List<T>> mutation, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        string? temporary = null;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            List<T> items;
            if (File.Exists(_path))
            {
                await using var input = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
                items = await JsonSerializer.DeserializeAsync<List<T>>(input, JsonOptions, token) ?? [];
            }
            else items = [];
            mutation(items);
            temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(output, items, JsonOptions, token);
                await output.FlushAsync(token);
            }
            File.Move(temporary, _path, true);
            temporary = null;
        }
        catch (JsonException ex) { logger.LogError(ex, "Refusing to overwrite corrupt JSON data file {DataFile}", Path.GetFileName(_path)); throw new InvalidDataException($"The data file {Path.GetFileName(_path)} is corrupt. It was not overwritten.", ex); }
        finally
        {
            if (temporary is not null && File.Exists(temporary)) File.Delete(temporary);
            _gate.Release();
        }
    }
}
