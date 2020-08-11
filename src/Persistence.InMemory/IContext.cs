using System.Collections.Generic;
using Abstractions.Persistence;

namespace Persistence.InMemory
{
    public interface IContext
    {
        void StartTransaction(string auditOwner);
        IEnumerable<T> ReadAll<T>() where T : IDomainEntity;
        void Insert(IDomainEntity entity);
        void Update(IDomainEntity entity);
        void Merge(IDomainEntity entity);
        void Delete(IDomainEntity entity);
        void Commit();
    }
}