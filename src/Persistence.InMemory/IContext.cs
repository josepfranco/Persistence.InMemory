using System.Collections.Generic;
using Abstractions.Persistence;

namespace Persistence.InMemory
{
    public interface IContext
    {
        void StartTransaction(string auditOwner);
        IEnumerable<T> ReadAll<T>() where T : IDomainEntity;
        void Insert<TEntity>(TEntity entity) where TEntity : IDomainEntity;
        void Update<TEntity>(TEntity entity) where TEntity : IDomainEntity;
        void Merge<TEntity>(TEntity entity) where TEntity : IDomainEntity;
        void Delete<TEntity>(TEntity entity) where TEntity : IDomainEntity;
        void Commit();
    }
}