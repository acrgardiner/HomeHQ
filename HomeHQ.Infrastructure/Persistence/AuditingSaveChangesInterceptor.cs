using System;
using System.Collections.Generic;
using System.Text;
using HomeHQ.Contracts;
using HomeHQ.Entities;
using HomeHQ.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomeHQ.Infrastructure.Persistence;

internal class AuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditingSaveChangesInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = string.IsNullOrWhiteSpace(_currentUser.UserId) ? "system" : _currentUser.UserId;
        if (eventData.Context == null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.Entity is ISoftDelete && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                ((AuditableEntity)entry.Entity).MarkDeleted(now, user);
            }

            if (entry.Entity is AuditableEntity auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added: auditable.MarkCreated(now, user); break;
                    case EntityState.Modified: auditable.MarkModified(now, user); break;
                }
            }
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
