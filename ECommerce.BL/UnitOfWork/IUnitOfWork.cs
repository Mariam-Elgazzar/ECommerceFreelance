using ECommerce.BL.Repository;
using ECommerce.BL.Services;
using ECommerce.BL.Services.AuthenticationService;
using ECommerce.BL.Services.CategoryServices;
using ECommerce.BL.Services.EmailServices;
using ECommerce.BL.Services.ProductServices;
using ECommerce.BL.Services.UserServices;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.BL.UnitOfWork
{
    public interface IUnitOfWork
    {
        IOrderService OrderServices { get; }
        IProductServices ProductServices { get; }
        ICategoryServices CategoryServices { get; }
        IEmailServices EmailServices { get; }
        IUserServices UserServices { get; }
        IAuthenticationServices AuthenticationServices { get; }

        /// <summary>
        /// Retrieves or creates a generic repository for the specified type T.
        /// </summary>
        /// <typeparam name="T">The entity type, constrained to reference types.</typeparam>
        /// <returns>An instance of IGenaricRepo<T> for the specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the context is null.</exception>
        IGenericRepository<T> Repository<T>() where T : class;
        /// <summary>
        /// Begins a new database transaction or returns an existing active transaction.
        /// </summary>
        /// <returns>An IDbContextTransaction for the current operation.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the database context is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the database context or connection is not initialized.</exception>
        Task<IDbContextTransaction> BeginTransactionAsync();
        /// <summary>
        /// Commits the current database transaction asynchronously.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no active transaction exists or the database context is not initialized.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the database context is disposed.</exception>
        Task CommitAsync();
        /// <summary>
        /// Rolls back the current database transaction asynchronously.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no active transaction exists or the database context is not initialized.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the database context is disposed.</exception>
        Task RollbackAsync();
        /// <summary>
        /// Saves all pending changes to the database asynchronously and returns the number of affected rows.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation, returning the number of affected rows.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the database context is not initialized or no changes are pending.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the database context is disposed.</exception>
        /// <exception cref="DbUpdateConcurrencyException">Thrown if a concurrency conflict occurs.</exception>
        /// <exception cref="DbUpdateException">Thrown if a database update error occurs.</exception>
        Task<int> Complete();
    }
}
