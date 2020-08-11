using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Persistence;

namespace Persistence.InMemory
{
    public abstract class AbstractReadRepository<TEntity> : IReadRepository<TEntity>
        where TEntity : class, IDomainEntity
    {
        private readonly IContext _context;

        protected AbstractReadRepository(IContext context)
        {
            _context = context;
        }

        public Task<TEntity> ReadByIdAsync(long id, CancellationToken token = default)
        {
            return Task.Run(() =>
            {
                return _context.ReadAll<TEntity>().FirstOrDefault(x => x.Id == id);
            }, token);
        }

        public Task<TEntity> ReadByGlobalIdAsync(Guid globalId, CancellationToken token = default)
        {
            return Task.Run(() =>
            {
                return _context.ReadAll<TEntity>().FirstOrDefault(x => x.GlobalId == globalId);
            }, token);
        }

        public Task<IEnumerable<TEntity>> ReadAllAsync(CancellationToken token = default)
        {
            return Task.Run(() => _context.ReadAll<TEntity>(), token);
        }
    }
}