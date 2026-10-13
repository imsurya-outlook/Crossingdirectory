using System.Data;
using System.Data.Common;
using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AdvertisingPortal.Infrastructure.Services;

/// <summary>
/// Reads public.shops (owned by the WhatsApp app, not by this DbContext) with plain SQL. Only PostgreSQL has that
/// table; on SQLite, or when the query fails, shops simply have no picture and the card shows its placeholder.
/// </summary>
public class ShopImageService : IShopImageService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ShopImageService> _logger;

    public ShopImageService(ApplicationDbContext db, ILogger<ShopImageService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<int, string>> GetPictureUrlsAsync(IEnumerable<int> shopIds, CancellationToken ct = default)
    {
        var ids = shopIds.Distinct().ToArray();
        var urls = new Dictionary<int, string>();
        if (ids.Length == 0 || !_db.Database.IsNpgsql())
            return urls;

        try
        {
            // The version (last update time) is part of the URL, so a new picture is fetched even though browsers
            // cache each URL for a long time.
            await QueryAsync(
                """
                SELECT id, floor(extract(epoch FROM updated_at))::bigint
                FROM public.shops
                WHERE id = ANY(@ids) AND picture IS NOT NULL
                """,
                command => AddParameter(command, "ids", ids),
                async reader =>
                {
                    while (await reader.ReadAsync(ct))
                        urls[reader.GetInt32(0)] = $"/shop-image/{reader.GetInt32(0)}?v={reader.GetInt64(1)}";
                },
                ct);
        }
        catch (DbException ex)
        {
            _logger.LogWarning(ex, "Could not read shop pictures; showing placeholders.");
        }

        return urls;
    }

    public async Task<ShopPicture?> GetPictureAsync(int shopId, CancellationToken ct = default)
    {
        if (!_db.Database.IsNpgsql())
            return null;

        ShopPicture? picture = null;
        await QueryAsync(
            "SELECT picture, picture_content_type FROM public.shops WHERE id = @id AND picture IS NOT NULL",
            command => AddParameter(command, "id", shopId),
            async reader =>
            {
                if (await reader.ReadAsync(ct))
                    picture = new ShopPicture(reader.GetFieldValue<byte[]>(0), reader.IsDBNull(1) ? "image/webp" : reader.GetString(1));
            },
            ct);

        return picture;
    }

    private async Task QueryAsync(string sql, Action<DbCommand> addParameters, Func<DbDataReader, Task> read, CancellationToken ct)
    {
        var connection = _db.Database.GetDbConnection();
        var opened = connection.State != ConnectionState.Open;
        if (opened)
            await connection.OpenAsync(ct);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            addParameters(command);
            await using var reader = await command.ExecuteReaderAsync(ct);
            await read(reader);
        }
        finally
        {
            if (opened)
                await connection.CloseAsync();
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
