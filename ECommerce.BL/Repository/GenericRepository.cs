using ECommerce.BL.Specification.BaseSpacification;
using ECommerce.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ECommerce.BL.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all entities, optionally filtered by a predicate.
        /// </summary>
        /// <param name="predicate">Optional condition to filter entities.</param>
        /// <returns>A read-only list of entities.</returns>
        public async Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null)
        {
            var query = _context.Set<T>().AsNoTracking();
            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            return await query.ToListAsync();
        }

        /// <summary>
        /// Retrieves an entity by its ID.
        /// </summary>
        /// <param name="id">The ID of the entity.</param>
        /// <returns>The entity, or null if not found.</returns>
        public async Task<T?> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        /// <summary>
        /// Adds a new entity to the database.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        public async Task AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
        }

        /// <summary>
        /// Adds a collection of entities to the database.
        /// </summary>
        /// <param name="entities">The entities to add.</param>
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities);
        }

        /// <summary>
        /// Deletes an entity by its ID.
        /// </summary>
        /// <param name="id">The ID of the entity to delete.</param>
        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                throw new ArgumentException($"Entity with ID {id} not found.");
            }
            _context.Set<T>().Remove(entity);
        }

        /// <summary>
        /// Deletes a collection of entities.
        /// </summary>
        /// <param name="entities">The entities to delete.</param>
        public async Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
        }

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        public async Task UpdateAsync(T entity)
        {
            _context.Set<T>().Update(entity);
        }

        /// <summary>
        /// Finds a single entity matching the specified predicate.
        /// </summary>
        /// <param name="predicate">The condition to filter entities.</param>
        /// <returns>The matching entity, or null if not found.</returns>
        //public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate)
        //{
        //    return await _context.Set<T>().AsNoTracking().FirstOrDefaultAsync(predicate);
        //}
        public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate, string? includeProperties = null)
        {
            IQueryable<T> query = _context.Set<T>().AsNoTracking();

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }

            return await query.FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// Checks if any entities match the specified predicate.
        /// </summary>
        /// <param name="predicate">The condition to check.</param>
        /// <returns>True if any entities match, otherwise false.</returns>
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().AnyAsync(predicate);
        }

        /// <summary>
        /// Retrieves a single entity matching the specified specification.
        /// </summary>
        /// <param name="spec">The specification to apply.</param>
        /// <returns>The matching entity, or null if not found.</returns>
        public async Task<T?> GetBySpecAsync(ISpecification<T> spec)
        {
            ArgumentNullException.ThrowIfNull(spec);
            return await ApplySpecification(spec).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Retrieves all entities matching the specified specification.
        /// </summary>
        /// <param name="spec">The specification to apply.</param>
        /// <returns>A read-only list of matching entities.</returns>
        public async Task<IReadOnlyList<T>> GetAllBySpecAsync(ISpecification<T> spec)
        {
            ArgumentNullException.ThrowIfNull(spec);
            return await ApplySpecification(spec).ToListAsync();
        }


        /// <summary>
        /// Counts entities matching the specified specification.
        /// </summary>
        /// <param name="spec">The specification to apply.</param>
        /// <returns>The number of matching entities.</returns>
        public async Task<int> CountAsync(ISpecification<T> spec)
        {
            ArgumentNullException.ThrowIfNull(spec);
            var query = _context.Set<T>().AsQueryable();

            return await query.CountAsync(spec?.Criteria);
        }

        /// <summary>
        /// Applies the specified specification to the query.
        /// </summary>
        /// <param name="spec">The specification to apply.</param>
        /// <returns>The configured queryable.</returns>
        private IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            var query = _context.Set<T>().AsQueryable();

            // Apply includes
            foreach (var include in spec.Includes)
            {
                query = query.Include(include);
            }

            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            if (spec.OrderBy != null)
            {
                query = query.OrderBy(spec.OrderBy);
            }
            else if (spec.OrderByDescending != null)
            {
                query = query.OrderByDescending(spec.OrderByDescending);
            }

            if (spec.IsPaginationEnabled)
            {
                query = query.Skip(spec.Skip).Take(spec.Take);
            }

            return query;
        }
    }
}
