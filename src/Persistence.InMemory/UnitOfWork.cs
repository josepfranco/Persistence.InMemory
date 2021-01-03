using System;
using Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence.InMemory
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IContext _context;

        public UnitOfWork(IContext context)
        {
            _context = context;
        }

        public IWriteRepository<TEntity> GetRepository<TEntity>() where TEntity : class, IDomainEntity
        {
            return new WriteRepository<TEntity>(_context);
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