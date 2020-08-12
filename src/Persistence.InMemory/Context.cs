using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Abstractions.Persistence;

namespace Persistence.InMemory
{
    public class Context : IContext
    {
        /// <summary>
        /// In-Memory dictionary that holds all the entities
        /// </summary>
        private IDictionary<Type, IList<IDomainEntity>> Database { get; set; }

        /// <summary>
        /// Whether the context is under a transaction operation or not
        /// </summary>
        private bool InTransactionOperation { get; set; }

        /// <summary>
        /// The current audit owner name/identifier
        /// </summary>
        private string TransactionAuditOwner { get; set; }
        
        /// <summary>
        /// The snapshot of the database during a transaction
        /// </summary>
        private IDictionary<Type, IList<IDomainEntity>> TransactionDatabaseSnapshot { get; set; }

        public Context()
        {
            Database = new Dictionary<Type, IList<IDomainEntity>>();
            InTransactionOperation = false;
            TransactionDatabaseSnapshot = new Dictionary<Type, IList<IDomainEntity>>();
            TransactionAuditOwner = string.Empty;
        }
        
        /// <summary>
        /// Begins a new transaction
        /// </summary>
        public void StartTransaction(string auditOwner)
        {
            // already under a transaction? discard previous operations
            if (InTransactionOperation)
            {
                TransactionDatabaseSnapshot.Clear();
            }
            
            // this is most likely a expensive operation
            TransactionDatabaseSnapshot = new Dictionary<Type, IList<IDomainEntity>>(Database);
            // -----------------------------------------

            TransactionAuditOwner = auditOwner;
            InTransactionOperation = true;
        }

        /// <summary>
        /// Reads all the entities with a specific type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> ReadAll<T>() where T : IDomainEntity
        {
            var entityList = Database[typeof(T)];
            return entityList != null
                ? entityList.Cast<T>()
                : new List<T>();
        }

        /// <summary>
        /// Persists a value into the in-memory database
        /// </summary>
        /// <param name="entity"></param>
        public void Insert<TEntity>(TEntity entity) where TEntity : IDomainEntity
        {
            DoInsertOperation(entity, InTransactionOperation 
                ? TransactionDatabaseSnapshot 
                : Database);
        }

        /// <summary>
        /// Updates a value existing in the in-memory database
        /// </summary>
        /// <param name="entity"></param>
        public void Update<TEntity>(TEntity entity) where TEntity : IDomainEntity
        {
            DoUpdateOperation(entity, InTransactionOperation 
                ? TransactionDatabaseSnapshot 
                : Database);
        }

        /// <summary>
        /// Merges (inserts or updates) a value into the in-memory database
        /// </summary>
        /// <param name="entity"></param>
        public void Merge<TEntity>(TEntity entity) where TEntity : IDomainEntity
        {
            DoMergeOperation(entity, InTransactionOperation 
                ? TransactionDatabaseSnapshot 
                : Database);
        }

        /// <summary>
        /// Deletes a value from the in-memory database
        /// </summary>
        /// <param name="entity"></param>
        public void Delete<TEntity>(TEntity entity) where TEntity : IDomainEntity
        {
            DoDeleteOperation(entity, InTransactionOperation 
                ? TransactionDatabaseSnapshot 
                : Database);
        }
        
        /// <summary>
        /// Commits a pending transaction unto the in-memory database
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Commit()
        {
            if (!InTransactionOperation)
                throw new InvalidOperationException("Cannot commit a non initialized transaction.");
            
            // clear current database
            Database.Clear();
            
            // use the new transaction snapshot as the most recent, updated one
            Database = TransactionDatabaseSnapshot;

            // no longer in a transaction
            InTransactionOperation = false;
        }

        #region PRIVATE METHODS
        private static void GenerateStoreInternalId(IDomainEntity entity, IList<IDomainEntity> entityList)
        {
            // is this a new entity with default id set?
            if (entity.Id == default)
            {
                entity.Id = entityList.Count > 0 // do we have any other elements present?
                    ? entityList[^1].Id + 1      // yes, then calculate last element's id + 1
                    : 1;                         // no, default id to 1
            }
        }

        private void CreateDatabaseEmptyList(IDomainEntity entity)
        {
            // type not yet registered, create empty list
            if (!Database.ContainsKey(entity.GetType()))
            {
                Database[entity.GetType()] = new List<IDomainEntity>();
            }
        }

        private static void ValidateEntityKeys(IDomainEntity entity)
        {
            // negative id? throw exception
            if (entity.Id < 0)
                throw new InvalidOperationException(
                    "Entity internal id cannot be negative.");

            // negative id? throw exception
            if (entity.GlobalId == default || entity.GlobalId == Guid.Empty)
                throw new InvalidOperationException(
                    "Entity global unique id cannot have the default value or be empty.");
        }
        
        private void CreateAuditData(IAuditable entity)
        {
            entity.CreatedBy = TransactionAuditOwner;
            entity.CreatedAt = DateTime.UtcNow;
        }
        
        private void UpdateAuditData(IAuditable entity)
        {
            entity.ModifiedBy = TransactionAuditOwner;
            entity.ModifiedAt = DateTime.UtcNow;
        }

        private void DoInsertOperation<TEntity>(TEntity entity, IDictionary<Type, IList<IDomainEntity>> database)
            where TEntity : IDomainEntity
        {
            ValidateEntityKeys(entity);
            CreateDatabaseEmptyList(entity);
            
            // get all child entities
            var typeProperties = entity.GetType().GetProperties();
            var childEntityReferences = typeProperties
                .Where(x => typeof(IDomainEntity).IsAssignableFrom(x.PropertyType));
            var childEntitiesNestedInCollections = typeProperties
                .Where(x => typeof(IEnumerable).IsAssignableFrom(x.PropertyType))
                .Where(x => x.PropertyType.GenericTypeArguments
                    .Any(y => typeof(IDomainEntity).IsAssignableFrom(y)));

            var entityList = database[entity.GetType()];

            GenerateStoreInternalId(entity, entityList);

            // any duplicate internal id found? throw exception
            if (entityList.Select(x => x.Id).Contains(entity.Id))
                throw new InvalidOperationException(
                    $"Cannot insert duplicate entity with internal id {entity.Id}");
            
            // any duplicate guid found? throw exception
            if (entityList.Select(x => x.GlobalId).Contains(entity.GlobalId))
                throw new InvalidOperationException(
                    $"Cannot insert duplicate entity with global unique id {entity.GlobalId}");
            
            CreateAuditData(entity);
            UpdateAuditData(entity);
            
            entityList.Add(entity);
        }

        private void DoUpdateOperation<TEntity>(TEntity entity, IDictionary<Type, IList<IDomainEntity>> database)
            where TEntity : IDomainEntity
        {
            ValidateEntityKeys(entity);
            CreateDatabaseEmptyList(entity);
            
            var entityList = database[entity.GetType()];
            var foundEntity = entityList.FirstOrDefault(x => x.Id == entity.Id && x.GlobalId == entity.GlobalId);
            if (entity.Id == default || entity.GlobalId == default || entity.GlobalId == Guid.Empty || foundEntity == null)
                throw new InvalidOperationException("Cannot update a not yet inserted entity.");
            
            UpdateAuditData(entity);
            
            var indexOfEntity = entityList.IndexOf(foundEntity);
            entityList[indexOfEntity] = entity;
        }

        private void DoMergeOperation<TEntity>(TEntity entity, IDictionary<Type, IList<IDomainEntity>> database)
            where TEntity : IDomainEntity
        {
            ValidateEntityKeys(entity);
            CreateDatabaseEmptyList(entity);
            var entityList = database[entity.GetType()];
            
            // default internal id? insert
            if (entity.Id == default)
            {
                GenerateStoreInternalId(entity, entityList);
                
                // any duplicate guid found? throw exception
                if (entityList.Select(x => x.GlobalId).Contains(entity.GlobalId))
                    throw new InvalidOperationException(
                        $"Cannot merge (insert in this context) duplicate entity with global unique id {entity.GlobalId}");
                
                CreateAuditData(entity);
                UpdateAuditData(entity);
                
                entityList.Add(entity);
            }
            
            var foundEntity = entityList.FirstOrDefault(x => x.Id == entity.Id && x.GlobalId == entity.GlobalId);

            // not found? insert with predefined internal id
            if (foundEntity == null)
            {
                CreateAuditData(entity);
                UpdateAuditData(entity);
                
                entityList.Add(entity);
            }
            // found? then update
            else
            {
                UpdateAuditData(entity);
                
                var indexOfEntity = entityList.IndexOf(foundEntity);
                entityList[indexOfEntity] = entity;
            }
        }

        private void DoDeleteOperation<TEntity>(TEntity entity, IDictionary<Type, IList<IDomainEntity>> database)
            where TEntity : IDomainEntity
        {
            ValidateEntityKeys(entity);
            CreateDatabaseEmptyList(entity);
            var entityList = database[entity.GetType()];
            
            if (entity.Id == default)
                throw new InvalidOperationException(
                    $"Cannot delete an entity without a defined internal id.");

            var foundEntity = entityList.FirstOrDefault(x => x.Id == entity.Id && x.GlobalId == entity.GlobalId);
            if (foundEntity == null)
                throw new InvalidOperationException(
                    $"Cannot delete unknown entity with id {entity.Id} and global unique id {entity.GlobalId}");

            entityList.Remove(foundEntity);
        }
        #endregion
    }
}