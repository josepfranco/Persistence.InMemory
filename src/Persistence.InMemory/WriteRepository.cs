using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Persistence;

namespace Persistence.InMemory
{
    public sealed class WriteRepository<TEntity> : IWriteRepository<TEntity>
        where TEntity : IDomainEntity
    {
        private readonly IContext _context;

        public WriteRepository(IContext context)
        {
            _context = context;
        }

        public Task InsertAsync(TEntity entity, CancellationToken token = default)
        {
            return Task.Run(() => _context.Insert(entity), token);
        }

        public Task InsertRangeAsync(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            return Task.Run(() =>
            {
                foreach (var entity in entities)
                {
                    _context.Insert(entity);
                }
            }, token);
        }

        public Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            return Task.Run(() => _context.Update(entity), token);
        }

        public Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            return Task.Run(() =>
            {
                foreach (var entity in entities)
                {
                    _context.Update(entity);
                }
            }, token);
        }

        public Task MergeAsync(TEntity entity, CancellationToken token = default)
        {
            return Task.Run(() => _context.Merge(entity), token);
        }

        public Task MergeRangeAsync(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            return Task.Run(() =>
            {
                foreach (var entity in entities)
                {
                    _context.Merge(entity);
                }
            }, token);
        }

        public Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            return Task.Run(() => _context.Delete(entity), token);
        }

        public Task DeleteAsyncAsync(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            return Task.Run(() =>
            {
                foreach (var entity in entities)
                {
                    _context.Delete(entity);
                }
            }, token);
        }
    }
}