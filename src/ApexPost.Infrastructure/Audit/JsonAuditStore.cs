using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApexPost.Application;
using ApexPost.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace ApexPost.Infrastructure;

public sealed class JsonAuditStore(IConfiguration configuration, IHostEnvironment environment, ILogger<JsonAuditStore> logger) : IAuditStore
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private string PathName { get { var configured = configuration["Data:Directory"] ?? "App_Data"; var directory = Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured); return Path.Combine(directory, "AuditRecord.jsonl"); } }

    public async Task AppendAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
            await using var stream = new FileStream(PathName, FileMode.Append, FileAccess.Write, FileShare.Read, 8192, FileOptions.Asynchronous | FileOptions.WriteThrough);
            await JsonSerializer.SerializeAsync(stream, record, Options, cancellationToken);
            await stream.WriteAsync("\n"u8.ToArray(), cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger.LogError(ex, "Could not append audit record. Business action audit durability may be affected."); throw;
        }
        finally { Gate.Release(); }
    }

    public async Task<IReadOnlyList<AuditRecord>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(PathName)) return Array.Empty<AuditRecord>();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var lines = await File.ReadAllLinesAsync(PathName, cancellationToken);
            return lines.Where(line => !string.IsNullOrWhiteSpace(line)).Reverse().Take(Math.Clamp(count, 1, 1000)).Select(line => JsonSerializer.Deserialize<AuditRecord>(line, Options)).Where(record => record is not null).Cast<AuditRecord>().ToArray();
        }
        finally { Gate.Release(); }
    }
}
