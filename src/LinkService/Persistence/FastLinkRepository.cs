using Dapper;
using LinkService.Persistence.Entities;
using MeteorCloud.Shared.ApiResults;

namespace LinkService.Persistence;

public class FastLinkRepository
{
    private readonly DapperContext _context;

    public FastLinkRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<FastLink?> GetByTokenAsync(Guid token)
    {
        const string query = @"SELECT * FROM FastLinks WHERE Token = @Token LIMIT 1;";
        await using var connection = await _context.CreateConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<FastLink>(query, new { Token = token });
    }

    public async Task<PagedResult<FastLink>> GetByUserIdAsync(int userId, int page, int pageSize)
    {
        const string dataQuery = @"
        SELECT * FROM FastLinks
        WHERE CreatedByUserId = @UserId
        ORDER BY CreatedAt DESC
        OFFSET @Offset ROWS
        LIMIT @PageSize;";

        const string countQuery = @"
        SELECT COUNT(*) FROM FastLinks
        WHERE CreatedByUserId = @UserId;";

        var offset = (page - 1) * pageSize;

        await using var connection = await _context.CreateConnectionAsync();

        var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, new { UserId = userId });

        var links = await connection.QueryAsync<FastLink>(dataQuery, new
        {
            UserId = userId,
            Offset = offset,
            PageSize = pageSize
        });

        return new PagedResult<FastLink>
        {
            Items = links.ToList(),
            TotalCount = totalCount
        };
    }

    public async Task<FastLink> CreateAsync(FastLink link)
    {
        const string query = @"
            INSERT INTO FastLinks (Token, FileId, CreatedByUserId, CreatedAt, ExpiresAt, Name, AccessCount, FileSize, FileName)
            VALUES (@Token, @FileId, @CreatedByUserId, @CreatedAt, @ExpiresAt, @Name, @AccessCount, @FileSize, @FileName)
            RETURNING *;";

        await using var connection = await _context.CreateConnectionAsync();
        return await connection.QuerySingleAsync<FastLink>(query, link);
    }

    public async Task<string?> DeleteByFileIdAsync(Guid fileId)
    {
        const string query = @"
        DELETE FROM FastLinks 
        WHERE FileId = @FileId
        RETURNING Token;";

        await using var connection = await _context.CreateConnectionAsync();
        var token = await connection.ExecuteScalarAsync<string?>(query, new { FileId = fileId });
        return token;
    }

    public async Task<bool> UpdateExpirationAsync(Guid token, DateTime newExpiration)
    {
        const string query = @"UPDATE FastLinks SET ExpiresAt = @ExpiresAt WHERE Token = @Token;";
        await using var connection = await _context.CreateConnectionAsync();
        var affected = await connection.ExecuteAsync(query, new { Token = token, ExpiresAt = newExpiration });
        return affected > 0;
    }

    public async Task<bool> IncrementAccessCountAsync(Guid token)
    {
        const string query = @"UPDATE FastLinks SET AccessCount = AccessCount + 1 WHERE Token = @Token;";
        await using var connection = await _context.CreateConnectionAsync();
        var affected = await connection.ExecuteAsync(query, new { Token = token });
        return affected > 0;
    }
    
    public async Task<List<(int UserId, Guid FileId)>> DeleteExpiredLinksAsync(DateTime now)
    {
        const string query = @"
        DELETE FROM FastLinks 
        WHERE ExpiresAt < @Now
        RETURNING CreatedByUserId AS UserId, FileId;";

        await using var connection = await _context.CreateConnectionAsync();
        var expiredLinks = await connection.QueryAsync<(int UserId, Guid FileId)>(query, new { Now = now });
        
        foreach (var (userId, fileId) in expiredLinks)
        {
            Console.WriteLine($"✔️ Expired: {userId}/{fileId}");
        }

        return expiredLinks.ToList();
    }
}
