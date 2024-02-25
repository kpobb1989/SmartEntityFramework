using Microsoft.EntityFrameworkCore;

using Sample.Abstractions.DB;
using Sample.Abstractions.DB.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.Function.Extensions
{
    //public static class RepositoryExtensions
    //{
    //    public static async Task RefreshAsync<TEntity, TKey>(
    //        this IRepository<TEntity> repository,
    //        IEnumerable<TEntity> newData,
    //        Func<TEntity, TKey> keySelector,
    //        string includeProperties = "",
    //        CancellationToken cancellationToken = default) where TEntity : DbEntity
    //    {
    //        var dbEntities = await repository
    //                                .ToListAsync(includeProperties: includeProperties)
    //                                .ToListAsync(cancellationToken: cancellationToken);

    //        var dataToAdd = newData.GroupJoin(dbEntities, keySelector, keySelector, (entity, dbEntities) => (NewEntity: entity, DbEntities: dbEntities))
    //            .Where(s => !s.DbEntities.Any())
    //            .Select(s => s.NewEntity)
    //            .ToList();

    //        var lastUpdate = DateTime.UtcNow.ToYearMonthDayFormat();

    //        foreach (var item in dataToAdd)
    //        {
    //            item.LastUpdate = lastUpdate;

    //            repository.Add(item);
    //        }

    //        var dataToUpdate = dbEntities.GroupJoin(newData, keySelector, keySelector, (dbEntity, entities) => (DbEntity: dbEntity, NewEntities: entities))
    //            .Where(s => s.NewEntities.Any())
    //            .Select(s => (s.DbEntity, NewEntity: s.NewEntities.First()))
    //            .ToList();

    //        foreach (var item in dataToUpdate)
    //        {
    //            if (!item.DbEntity.Equals(item.NewEntity))
    //            {
    //                item.DbEntity.Update(item.NewEntity);
    //            }

    //            item.DbEntity.LastUpdate = lastUpdate;

    //            repository.Update(item.DbEntity);
    //        }

    //        var dataToDelete = dbEntities
    //            .Where(s => s.LastUpdate != lastUpdate)
    //            .ToList();

    //        repository.Remove(dataToDelete);

    //        await repository.CommitAsync();
    //    }
    //}
}
