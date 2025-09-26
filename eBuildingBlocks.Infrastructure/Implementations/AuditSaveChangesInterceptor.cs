using eBuildingBlocks.Common.utils;
using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
// Namespace updated to eBuildingBlocks
namespace eBuildingBlocks.Infrastructure.Implementations
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
       
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ICurrentUser _currentUser;

        public AuditSaveChangesInterceptor(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ApplyAudit(eventData.Context);
            PrepareAuditLogs(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }
        private void ApplyAudit(DbContext? context)
        {
            if (context == null) return;

            var entries = context.ChangeTracker.Entries<AuditableEntity<Guid>>();

            var now = DateTime.UtcNow;
            var user = GetCurrentUsername();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.SetCreated(user);
                        break;
                    case EntityState.Modified:
                        entry.Entity.SetModified(user);
                        break;
                }
            }
        }
        private void PrepareAuditLogs(DbContext? context)
        {
            if (context == null) return;
            context.ChangeTracker.DetectChanges();
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                            !(e.Entity is AuditLog));
            var _auditLogs = new List<AuditLog>();
            foreach (var entry in entries)
            {
                var audit = new AuditLog
                {
                    TableName = entry.Metadata.GetTableName()!,
                    Action = entry.State.ToString(),
                    KeyValues = Serialize(entry.Properties.Where(p => p.Metadata.IsPrimaryKey())),
                    OldValues = entry.State == EntityState.Modified
                                ? SerializeModifiedOnly(entry, useOriginal: true)
                                : entry.State == EntityState.Deleted
                                    ? SerializeAll(entry, useOriginal: true)
                                    : null,

                    NewValues = entry.State == EntityState.Modified
                            ? SerializeModifiedOnly(entry, useOriginal: false)
                            : entry.State == EntityState.Added
                                ? SerializeAll(entry, useOriginal: false)
                                : null,
                    PerformedAt = DateTime.UtcNow,
                    PerformedBy = GetCurrentUsername(),
                    IPAddress = GetIPAdress()
                };

                _auditLogs.Add(audit);
            }
            if (_auditLogs.Any())
            {
                context!.Set<AuditLog>().AddRange(_auditLogs);
            }
        }

        private string GetCurrentUsername()
        {
            return _currentUser?.UserName ?? "System";
        }
        private string GetIPAdress()
        {
            return _currentUser?.IPAddress ?? "0.0.0.0";
        }
        private string SerializeModifiedOnly(EntityEntry entry, bool useOriginal)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var prop in entry.Properties.Where(p => p.IsModified))
            {
                var name = prop.Metadata.Name;
                var value = useOriginal ? entry.OriginalValues[name] : entry.CurrentValues[name];
                dict[name] = value;
            }

            return JsonSerializer.Serialize(dict, _jsonOptions);
        }

        private string SerializeAll(EntityEntry entry, bool useOriginal)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var prop in entry.Properties)
            {
                var name = prop.Metadata.Name;
                var value = useOriginal ? entry.OriginalValues[name] : entry.CurrentValues[name];
                dict[name] = value;
            }

            return JsonSerializer.Serialize(dict, _jsonOptions);
        }


        private string Serialize(IEnumerable<PropertyEntry> properties, bool old = false)
        {
            var dict = properties.ToDictionary(
                p => p.Metadata.Name,
                p => old ? p.OriginalValue : p.CurrentValue
            );
            return JsonSerializer.Serialize(dict, _jsonOptions);
        }
    }

}
