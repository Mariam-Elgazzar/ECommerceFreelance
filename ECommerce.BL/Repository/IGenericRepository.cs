using ECommerce.BL.Specification.BaseSpacification;
using ECommerce.DAL.Models;
using System.Linq.Expressions;

namespace ECommerce.BL.Repository
{
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>
        /// Retrieves all entities, optionally filtered by a predicate.
        /// </summary>
        /// <param name="predicate">Optional condition to filter entities.</param>
        /// <returns>A read-only list of entities.</returns>
        Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null);

        /// <summary>
        /// Retrieves an entity by its ID.
        /// </summary>
        /// <param name="id">The ID of the entity.</param>
        /// <returns>The entity, or null if not found.</returns>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// Adds a new entity to the database.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        Task AddAsync(T entity);

        /// <summary>
        /// Adds a collection of entities to the database.
        /// </summary>
        /// <param name="entities">The entities to add.</param>
        Task AddRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// Deletes an entity by its ID.
        /// </summary>
        /// <param name="id">The ID of the entity to delete.</param>
        Task DeleteAsync(int id);

        /// <summary>
        /// Deletes a collection of entities.
        /// </summary>
        /// <param name="entities">The entities to delete.</param>
        Task DeleteRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        Task UpdateAsync(T entity);

        /// <summary>
        /// Finds a single entity matching the specified predicate.
        /// </summary>
        /// <param name="predicate">The condition to filter entities.</param>
        /// <returns>The matching entity, or null if not found.</returns>
        Task<T?> FindAsync(Expression<Func<T, bool>> predicate, string? includeProperties = null);
        /// <summary>
        /// Checks if any entities match the specified predicate.
        /// </summary>
        /// <param name="predicate">The condition to check.</param>
        /// <returns>True if any entities match, otherwise false.</returns>
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Retrieves a single entity matching the specified specification.
        /// </summary>
        /// <param name="spec">The specification to apply.</param>
        /// <returns>The matching entity, or null if not found.</returns>
        Task<T?> GetBySpecAsync(ISpecification<T> spec);

        /// <summary>
        /// Retrieves all entities matching the specified specification.
        /// </summary>
        /// <param name="spec">The specification to apply.</param>
        /// <returns>A read-only list of matching entities.</returns>
        Task<IReadOnlyList<T>> GetAllBySpecAsync(ISpecification<T> spec);

        /// <summary>
        /// Counts entities matching the specified specification.
        /// </summary>
        /// <param name="spec">The specification to apply.</param>
        /// <returns>The number of matching entities.</returns>
        Task<int> CountAsync(ISpecification<T> spec);
    }
}
