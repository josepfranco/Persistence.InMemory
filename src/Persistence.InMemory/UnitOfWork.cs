using System;
using Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence.InMemory
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IContext _context;
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWork(IContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        public IWriteRepository<TEntity> GetRepository<TEntity>() where TEntity : IDomainEntity
        {
            return _serviceProvider.GetRequiredService<IWriteRepository<TEntity>>();
        }

        public void Begin(string auditOwner)
        {
            _context.StartTransaction(auditOwner);
        }

        public void Commit()
        {
            _context.Commit();
        }
    }
}