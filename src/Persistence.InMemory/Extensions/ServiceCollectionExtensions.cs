using Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence.InMemory.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the in memory persistence context into the DI container
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddInMemoryPersistence(this IServiceCollection services)
        {
            services.AddSingleton<IContext, Context>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            return services;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TRepository"></typeparam>
        /// <returns></returns>
        public static IServiceCollection AddInMemoryReadRepository<TEntity, TRepository>(this IServiceCollection services)
            where TEntity : class, IDomainEntity
            where TRepository : AbstractReadRepository<TEntity>
        {
            services.AddTransient<IReadRepository<TEntity>, TRepository>();
            return services;
        }
    }
}