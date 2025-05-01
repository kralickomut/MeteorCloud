using System.Text.Json;
using AuditService.Persistence.Entities;
using Dapper;
using MeteorCloud.Shared.ApiResults;
using Npgsql;

namespace AuditService.Persistence;

public class AuditRepository
{
    private readonly DapperContext _context;

    public AuditRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task InsertAsync(AuditEvent auditEvent)
    {
        const string query = @"
            INSERT INTO AuditEvents 
            (EntityType, EntityId, Action, PerformedByUserId, Timestamp, WorkspaceId, Metadata)
            VALUES 
            (@EntityType, @EntityId, @Action, @PerformedByUserId, @Timestamp, @WorkspaceId, @Metadata::jsonb);";

        var parameters = new
        {
            auditEvent.EntityType,
            auditEvent.EntityId,
            auditEvent.Action,
            auditEvent.PerformedByUserId,
            auditEvent.Timestamp,
            auditEvent.WorkspaceId,
            Metadata = auditEvent.Metadata != null ? JsonSerializer.Serialize(auditEvent.Metadata) : null
        };

        await using var connection = await _context.CreateConnectionAsync();
        await connection.ExecuteAsync(query, parameters);
    }

    public async Task<IEnumerable<AuditEvent>> GetByEntityAsync(string entityType, string entityId)
    {
        const string query = @"
            SELECT * FROM AuditEvents
            WHERE EntityType = @EntityType AND EntityId = @EntityId
            ORDER BY Timestamp DESC;";

        await using var connection = await _context.CreateConnectionAsync();

        var results = await connection.QueryAsync<AuditEventDto>(query, new { entityType, entityId });

        return results.Select(ToAuditEvent);
    }

    public async Task<PagedResult<AuditEvent>> GetFileHistoryByWorkspaceIdAsync(int workspaceId, int page = 1, int pageSize = 10)
    {
        await using var connection = await _context.CreateConnectionAsync();

        const string dataQuery = @"
            SELECT * FROM AuditEvents
            WHERE EntityType = 'File'
              AND WorkspaceId = @WorkspaceId
            ORDER BY Timestamp DESC
            OFFSET @Offset
            LIMIT @PageSize;";

        const string countQuery = @"
            SELECT COUNT(*) FROM AuditEvents
            WHERE EntityType = 'File'
              AND WorkspaceId = @WorkspaceId;";

        var offset = (page - 1) * pageSize;

        var count = await connection.ExecuteScalarAsync<int>(countQuery, new { WorkspaceId = workspaceId });

        var results = await connection.QueryAsync<AuditEventDto>(dataQuery, new
        {
            WorkspaceId = workspaceId,
            Offset = offset,
            PageSize = pageSize
        });

        var items = results.Select(ToAuditEvent);

        return new PagedResult<AuditEvent>
        {
            Items = items.ToList(),
            TotalCount = count
        };
    }

    public async Task DeleteAllByWorkspaceIdAsync(int workspaceId)
    {
        const string query = "DELETE FROM AuditEvents WHERE WorkspaceId = @WorkspaceId;";
        await using var connection = await _context.CreateConnectionAsync();
        await connection.ExecuteAsync(query, new { WorkspaceId = workspaceId });
    }

    private static AuditEvent ToAuditEvent(AuditEventDto dto)
    {
        return new AuditEvent
        {
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            Action = dto.Action,
            PerformedByUserId = dto.PerformedByUserId,
            Timestamp = dto.Timestamp,
            WorkspaceId = dto.WorkspaceId,
            Metadata = dto.Metadata != null
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(dto.Metadata)
                : null
        };
    }

    public async Task<List<int>> GetRecentWorkspaceIdsByUserAsync(int userId, int limit = 3)
    {
        const string query = @"
            SELECT DISTINCT ON (WorkspaceId) WorkspaceId, Timestamp
            FROM AuditEvents
            WHERE PerformedByUserId = @UserId
              AND WorkspaceId IS NOT NULL
            ORDER BY WorkspaceId, Timestamp DESC;";

        await using var connection = await _context.CreateConnectionAsync();
        var allEvents = await connection.QueryAsync<(int WorkspaceId, DateTime Timestamp)>(query, new { UserId = userId });

        var recentWorkspaceIds = allEvents
            .GroupBy(e => e.WorkspaceId)
            .Select(g => g.OrderByDescending(e => e.Timestamp).First())
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .Select(e => e.WorkspaceId)
            .ToList();

        return recentWorkspaceIds;
    }

    private record AuditEventDto
    {
        public string EntityType { get; init; }
        public string EntityId { get; init; }
        public string Action { get; init; }
        public int PerformedByUserId { get; init; }
        public DateTime Timestamp { get; init; }
        public int WorkspaceId { get; init; }
        public string? Metadata { get; init; }
    }
}
