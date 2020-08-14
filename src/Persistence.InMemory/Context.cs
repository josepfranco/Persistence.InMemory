using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            Database.TryGetValue(typeof(T), out var entityList);
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
        private static void GenerateStoreInternalId(IDomainEntity entity, IDictionary<Type, IList<IDomainEntity>> database)
        {
            var entityList = database[entity.GetType()];
            
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
        
        private static IEnumerable<IDomainEntity> GetAllChildReferences<TEntity>(TEntity entity) 
            where TEntity : IDomainEntity
        {
            var visited = new HashSet<IDomainEntity>();
            var frontier = new Queue<IDomainEntity>();
            frontier.Enqueue(entity);

            while (frontier.Count > 0)
            {
                var instance = frontier.Dequeue();
                var allChildReferences = GetAllChildReferencesAux(instance);
                visited.Add(instance);
                foreach (var childInstance in allChildReferences)
                {
                    frontier.Enqueue(childInstance);
                }
            }

            // remove the parent entity
            visited.Remove(entity);
            return visited;
        }

        private static IEnumerable<IDomainEntity> GetAllChildReferencesAux(IDomainEntity entity)
        {
            // get all child entities
            var typeProperties = entity.GetType().GetProperties();
            var referencePropertyNames = GetReferencePropertyNames(typeProperties);
            var collectionPropertyNames = GetCollectionPropertyNames(typeProperties);

            return collectionPropertyNames
                .SelectMany(x => entity.GetType().GetProperty(x)?.GetValue(entity) as IEnumerable<IDomainEntity>)
                .Concat(referencePropertyNames
                    .Select(x => entity.GetType().GetProperty(x)?.GetValue(entity))
                    .Cast<IDomainEntity>())
                .Distinct()
                .ToList();
        }

        private static IEnumerable<string> GetCollectionPropertyNames(IEnumerable<PropertyInfo> typeProperties)
        {
            var childEntitiesInCollectionNames = typeProperties
                .Where(x => typeof(IEnumerable).IsAssignableFrom(x.PropertyType))
                .Where(x => x.PropertyType.GenericTypeArguments
                    .Any(y => typeof(IDomainEntity).IsAssignableFrom(y)))
                .Select(x => x.Name);
            return childEntitiesInCollectionNames;
        }

        private static IEnumerable<string> GetReferencePropertyNames(IEnumerable<PropertyInfo> typeProperties)
        {
            var childEntityReferenceNames = typeProperties
                .Where(x => typeof(IDomainEntity).IsAssignableFrom(x.PropertyType))
                .Select(x => x.Name);
            return childEntityReferenceNames;
        }
        
        private void InsertEntity<TEntity>(TEntity entity, IDictionary<Type, IList<IDomainEntity>> database) where TEntity : IDomainEntity
        {
            CreateDatabaseEmptyList(entity);
            var entityList = database[entity.GetType()];
            
            // any duplicate guid found? throw exception
            if (entityList.Select(x => x.GlobalId).Contains(entity.GlobalId))
                throw new InvalidOperationException(
                    $"Cannot insert duplicate entity with global unique id {entity.GlobalId}");

            if (entity.Id == default)
            {
                GenerateStoreInternalId(entity, database);
            }

            CreateAuditData(entity);
            UpdateAuditData(entity);
            entityList.Add(entity);
        }
        
        private void UpdateEntity<TEntity>(TEntity entity, IDomainEntity foundEntity, IDictionary<Type, IList<IDomainEntity>> database)
            where TEntity : IDomainEntity
        {
            CreateDatabaseEmptyList(entity);
            UpdateAuditData(entity);
            var entityList = database[entity.GetType()];
            var indexOfEntity = entityList.IndexOf(foundEntity);
            entityList[indexOfEntity] = entity;
        }
        
        private void DeleteEntity<TEntity>(TEntity entity, IDictionary<Type, IList<IDomainEntity>> database) where TEntity : IDomainEntity
        {
            CreateDatabaseEmptyList(entity);
            if (entity.Id == default)
                throw new InvalidOperationException(
                    $"Cannot delete an entity without a defined internal id.");

            var entityList = database[entity.GetType()];
            var foundEntity = entityList.FirstOrDefault(x => x.Id == entity.Id && x.GlobalId == entity.GlobalId);
            if (foundEntity == null)
                throw new InvalidOperationException(
                    $"Cannot delete unknown entity with id {entity.Id} and global unique id {entity.GlobalId}");

            entityList.Remove(foundEntity);
        }
        
        private void InsertEntities(IEnumerable<IDomainEntity> entities, IDictionary<Type, IList<IDomainEntity>> database)
        {
            foreach (var entity in entities)
            {
                InsertEntity(entity, database);
            }
        }

        private void UpdateEntities(IEnumerable<IDomainEntity> entities, IDictionary<Type, IList<IDomainEntity>> database)
        {
            foreach (var entity in entities)
            {
                var entityList = database[entity.GetType()];
                var foundEntity = entityList.FirstOrDefault(x => x.Id == entity.Id && x.GlobalId == entity.GlobalId);
                UpdateEntity(entity, foundEntity, database);
            }
        }
        
        private void DeleteEntities(IEnumerable<IDomainEntity> entities, IDictionary<Type, IList<IDomainEntity>> database)
        {
            foreach (var entity in entities)
            {
                DeleteEntity(entity, database);
            }
        }

        private void DoInsertOperation<TEntity>(TEntity entity, IDictionary<Type, IList<IDomainEntity>> database)
            where TEntity : IDomainEntity
        {
            ValidateEntityKeys(entity);
            CreateDatabaseEmptyList(entity);
            var entityList = database[entity.GetType()];
            
            // any duplicate internal id found? throw exception
            if (entityList.Select(x => x.Id).Contains(entity.Id))
                throw new InvalidOperationException(
                    $"Cannot insert duplicate entity with internal id {entity.Id}");

            GenerateStoreInternalId(entity, database);
            InsertEntity(entity, database);
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
            UpdateEntity(entity, foundEntity, database);
        }

        private void DoMergeOperation<TEntity>(TEntity entity, IDictionary<Type, IList<IDomainEntity>> database)
            where TEntity : IDomainEntity
        {
            ValidateEntityKeys(entity);
            CreateDatabaseEmptyList(entity);
            var entityList = database[entity.GetType()];
            var foundEntity = entityList.FirstOrDefault(x => x.Id == entity.Id && x.GlobalId == entity.GlobalId);
            if (foundEntity == null)
            {
                var allChildEntities = GetAllChildReferences(entity).ToList();
                var toInsertEntities = allChildEntities.Except(ReadAll<IDomainEntity>()).ToList();
                var toUpdateEntities = ReadAll<IDomainEntity>().Intersect(allChildEntities).ToList();
                InsertEntities(toInsertEntities, database);
                UpdateEntities(toUpdateEntities, database);
                InsertEntity(entity, database);
            }
            else
            {
                var allDatabaseChildEntities = GetAllChildReferences(foundEntity).ToList();
                var allChildEntities = GetAllChildReferences(entity).ToList();
                var toInsertEntities = allChildEntities.Except(allDatabaseChildEntities).ToList();
                var toUpdateEntities = allDatabaseChildEntities.Intersect(allChildEntities).ToList();
                var toRemoveEntities = allDatabaseChildEntities.Except(allChildEntities).ToList();
                InsertEntities(toInsertEntities, database);
                UpdateEntities(toUpdateEntities, database);
                DeleteEntities(toRemoveEntities, database);
                UpdateEntity(entity, foundEntity, database);
            }
        }
        private void DoDeleteOperation<TEntity>(TEntity entity, IDictionary<Type, IList<IDomainEntity>> database)
            where TEntity : IDomainEntity
        {
            ValidateEntityKeys(entity);
            CreateDatabaseEmptyList(entity);
            DeleteEntity(entity, database);
        }
        #endregion
    }
}