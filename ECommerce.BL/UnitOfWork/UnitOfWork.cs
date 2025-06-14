#region Usings

using ECommerce.BL.Repository;
using ECommerce.BL.Services.AuthenticationService;
using ECommerce.BL.Services.CategoryServices;
using ECommerce.BL.Services.EmailServices;
using ECommerce.BL.Services.ProductServices;
using ECommerce.BL.Services.UserServices;
using ECommerce.BL.Settings;
using ECommerce.DAL.Data;
using ECommerce.DAL.Extend;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

#endregion

namespace ECommerce.BL.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {

        #region Private Properties

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ConcurrentDictionary<Type, object> _repos;
        private readonly ILogger<UnitOfWork> _logger;
        private IDbContextTransaction _transaction;
        private bool _disposed;
        private readonly IOptions<AdminLogin> adminLogin;
        private readonly IOptions<JWT> jwt;
        private readonly IOptions<EmailConfiguration> _emailConfiguration;
        private readonly IOptions<CloudinarySettings> _cloudinarySettings;
        private readonly IOptions<OrderSettings> _orderSettings;
        private readonly ILogger<UnitOfWork> _unitOfWorkLogger;
        private readonly IOptions<TwilioSettings> _twilioSettings;

        #region Services
        private IAuthenticationServices _authenticationService;
        private IEmailServices _emailService;
        private ICategoryServices _categoryService;
        private IProductServices _productService;
        private IUserServices _userService;

        #endregion

        #endregion


        #region Public Properties

        #region AuthenticationService
        public IAuthenticationServices AuthenticationServices
        {
            get
            {
                if (_authenticationService == null)
                {
                    _authenticationService = new AuthenticationServices(
                        _userManager,
                        _signInManager,
                        adminLogin,
                        jwt,
                        new Logger<AuthenticationServices>(new LoggerFactory()),
                        this,
                        _emailConfiguration
                    );
                    return _authenticationService;
                }
                return _authenticationService;
            }
        }

        #endregion

        #region EmailServices
        public IEmailServices EmailServices
        {
            get
            {
                if (_emailService == null)
                {
                    _emailService = new EmailServices(_emailConfiguration);
                    return _emailService;
                }
                return _emailService;
            }
        }
        #endregion

        #region CategoryServices

        public ICategoryServices CategoryServices
        {
            get
            {
                if (_categoryService == null)
                {
                    _categoryService = new CategoryServices(_cloudinarySettings, this);
                    return _categoryService;
                }
                return _categoryService;
            }
        }
        #endregion

        #region ProductServices

        public IProductServices ProductServices
        {
            get
            {
                if (_productService == null)
                {
                    _productService = new ProductServices(this, _cloudinarySettings);
                    return _productService;
                }
                return _productService;
            }
        }
        #endregion

        #region UserServices
        public IUserServices UserServices
        {
            get
            {
                if (_userService == null)
                {
                    _userService = new UserServices(_userManager,this);
                    return _userService;
                }
                return _userService;
            }
        }
        #endregion

        #endregion


        #region Constructor

        public UnitOfWork(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UnitOfWork> logger,
            IOptions<AdminLogin> adminLogin,
            IOptions<JWT> jwt,
            IOptions<EmailConfiguration> emailConfiguration,
            IOptions<CloudinarySettings> cloudinarySettings,
            IOptions<OrderSettings> orderSettings,
            IOptions<TwilioSettings> twilioSettings)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _repos = new();
            _logger = logger;
            _disposed = false;
            this.adminLogin = adminLogin;
            this.jwt = jwt;
            this._emailConfiguration = emailConfiguration;
            _cloudinarySettings = cloudinarySettings;
            _orderSettings = orderSettings;
            _twilioSettings = twilioSettings;
        }


        #endregion


        #region Repository

        /// <summary>
        /// Retrieves or creates a generic repository for the specified type T.
        /// </summary>
        /// <typeparam name="T">The entity type, constrained to reference types.</typeparam>
        /// <returns>An instance of IGenaricRepo<T> for the specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the context is null.</exception>
        public IGenericRepository<T> Repository<T>() where T : class
        {
            try
            {
                // Retrieve or create a repository using the Type as the key in the ConcurrentDictionary
                var repository = (IGenericRepository<T>)_repos.GetOrAdd(typeof(T), _ => new GenericRepository<T>(_context));
                // Log successful retrieval or creation of the repository
                _logger.LogDebug("Successfully retrieved or created repository for entity type: {EntityType}", typeof(T).Name);
                // Return the repository instance
                return repository;
            }
            // Catch any exceptions during repository operations
            catch (Exception ex)
            {
                // Log the error with the type name and exception message
                _logger.LogError(ex, "Failed to retrieve or create repository for entity type: {EntityType}. Error: {Message}", typeof(T).Name, ex.Message);
                // Rethrow the exception to propagate it up the call stack
                throw;
            }
        }

        #endregion


        #region Begin Transaction

        /// <summary>
        /// Begins a new database transaction or returns an existing active transaction.
        /// </summary>
        /// <returns>An IDbContextTransaction for the current operation.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the database context is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the database context or connection is not initialized.</exception>
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            try
            {
                // Check if the DbContext is null
                if (_context == null)
                {
                    // Log an error if the DbContext is null
                    _logger.LogError("Database context is null.");
                    // Throw an exception indicating the DbContext is not initialized
                    throw new InvalidOperationException("Database context is not initialized.");
                }
                // Check if the DbContext has been disposed
                if (_context.GetType().GetProperty("IsDisposed")?.GetValue(_context) as bool? == true)
                {
                    // Log an error if the DbContext is disposed
                    _logger.LogError("Database context is disposed.");
                    // Throw an exception indicating the DbContext is disposed
                    throw new ObjectDisposedException(nameof(_context), "Database context is disposed.");
                }
                // Check if the Database property of the DbContext is null
                if (_context.Database == null)
                {
                    // Log an error if the Database connection is null
                    _logger.LogError("Database connection is null.");
                    // Throw an exception indicating the Database connection is not initialized
                    throw new InvalidOperationException("Database connection is not initialized.");
                }
                // Check if there is an existing active transaction in the DbContext
                if (_context.Database.CurrentTransaction != null)
                {
                    // Log a warning if a transaction is already active
                    _logger.LogWarning("An active transaction already exists. Returning the existing transaction.");
                    // Return the existing transaction from the DbContext
                    return _context.Database.CurrentTransaction;
                }
                // Check if the local transaction field is not null (additional safety for class-level tracking)
                if (_transaction != null)
                {
                    // Log a warning if a transaction is already tracked in the class
                    _logger.LogWarning("A transaction is already tracked in the factory. Returning the existing transaction.");
                    // Return the tracked transaction
                    return _transaction;
                }
                // Start a new transaction asynchronously using the DbContext
                _transaction = await _context.Database.BeginTransactionAsync();
                // Log successful creation of the transaction
                _logger.LogInformation("Database transaction started successfully.");
                // Return the newly created transaction
                return _transaction;
            }
            // Catch any exceptions during transaction creation
            catch (Exception ex)
            {
                // Log the error with the exception details
                _logger.LogError(ex, "Failed to start a database transaction: {Message}", ex.Message);
                // Rethrow the original exception to preserve the stack trace
                throw;
            }
        }



        #endregion


        #region Commit Transaction

        /// <summary>
        /// Commits the current database transaction asynchronously.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no active transaction exists or the database context is not initialized.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the database context is disposed.</exception>
        public async Task CommitAsync()
        {
            // Log the attempt to commit the current transaction
            _logger.LogInformation("Attempting to commit the database transaction.");
            // Start a try block to handle exceptions during transaction commit
            try
            {
                // Check if the DbContext is null
                if (_context == null)
                {
                    // Log an error if the DbContext is null
                    _logger.LogError("Database context is null.");
                    // Throw an exception indicating the DbContext is not initialized
                    throw new InvalidOperationException("Database context is not initialized.");
                }
                // Check if the DbContext has been disposed
                if (_context.GetType().GetProperty("IsDisposed")?.GetValue(_context) as bool? == true)
                {
                    // Log an error if the DbContext is disposed
                    _logger.LogError("Database context is disposed.");
                    // Throw an exception indicating the DbContext is disposed
                    throw new ObjectDisposedException(nameof(_context), "Database context is disposed.");
                }
                // Check if the Database property of the DbContext is null
                if (_context.Database == null)
                {
                    // Log an error if the Database connection is null
                    _logger.LogError("Database connection is null.");
                    // Throw an exception indicating the Database connection is not initialized
                    throw new InvalidOperationException("Database connection is not initialized.");
                }
                // Check if there is no active transaction in the DbContext
                if (_context.Database.CurrentTransaction == null)
                {
                    // Log an error if no transaction is active
                    _logger.LogError("No active transaction exists to commit.");
                    // Throw an exception indicating no transaction is active
                    throw new InvalidOperationException("No active transaction exists to commit.");
                }
                // Commit the transaction asynchronously
                await _context.Database.CommitTransactionAsync();
                // Clear the transaction field to avoid stale references
                _transaction = null;
                // Log successful commit of the transaction
                _logger.LogInformation("Database transaction committed successfully.");
            }
            // Catch any exceptions during transaction commit
            catch (Exception ex)
            {
                // Log the error with the exception details
                _logger.LogError(ex, "Failed to commit the database transaction: {Message}", ex.Message);
                // Rethrow the original exception to preserve the stack trace
                throw;
            }
        }



        #endregion


        #region Rollback Transaction

        /// <summary>
        /// Rolls back the current database transaction asynchronously.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no active transaction exists or the database context is not initialized.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the database context is disposed.</exception>
        public async Task RollbackAsync()
        {
            try
            {
                // Check if the DbContext is null
                if (_context == null)
                {
                    // Log an error if the DbContext is null
                    _logger.LogError("Database context is null.");
                    // Throw an exception indicating the DbContext is not initialized
                    throw new InvalidOperationException("Database context is not initialized.");
                }
                // Check if the DbContext has been disposed
                if (_context.GetType().GetProperty("IsDisposed")?.GetValue(_context) as bool? == true)
                {
                    // Log an error if the DbContext is disposed
                    _logger.LogError("Database context is disposed.");
                    // Throw an exception indicating the DbContext is disposed
                    throw new ObjectDisposedException(nameof(_context), "Database context is disposed.");
                }
                // Check if the Database property of the DbContext is null
                if (_context.Database == null)
                {
                    // Log an error if the Database connection is null
                    _logger.LogError("Database connection is null.");
                    // Throw an exception indicating the Database connection is not initialized
                    throw new InvalidOperationException("Database connection is not initialized.");
                }
                // Check if there is no active transaction in the DbContext
                if (_context.Database.CurrentTransaction == null)
                {
                    // Log a warning if no transaction is active
                    _logger.LogWarning("No active transaction exists to roll back.");
                    // Throw an exception indicating no transaction is active
                    throw new InvalidOperationException("No active transaction exists to roll back.");
                }
                // Roll back the transaction asynchronously
                await _context.Database.RollbackTransactionAsync();
                // Clear the transaction field to avoid stale references
                _transaction = null;
                // Log successful rollback of the transaction
                _logger.LogInformation("Database transaction rolled back successfully.");
            }
            // Catch any exceptions during transaction rollback
            catch (Exception ex)
            {
                // Log the error with the exception details
                _logger.LogError(ex, "Failed to roll back the database transaction: {Message}", ex.Message);
                // Rethrow the original exception to preserve the stack trace
                throw;
            }
        }



        #endregion


        #region Save Changes

        /// <summary>
        /// Saves all pending changes to the database asynchronously and returns the number of affected rows.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation, returning the number of affected rows.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the database context is not initialized or no changes are pending.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the database context is disposed.</exception>
        /// <exception cref="DbUpdateConcurrencyException">Thrown if a concurrency conflict occurs.</exception>
        /// <exception cref="DbUpdateException">Thrown if a database update error occurs.</exception>
        public async Task<int> Complete()
        {
            try
            {
                // Check if the DbContext is null
                if (_context == null)
                {
                    // Log an error if the DbContext is null
                    _logger.LogError("Database context is null.");
                    // Throw an exception indicating the DbContext is not initialized
                    throw new InvalidOperationException("Database context is not initialized.");
                }
                // Check if the DbContext has been disposed
                if (_context.GetType().GetProperty("IsDisposed")?.GetValue(_context) as bool? == true)
                {
                    // Log an error if the DbContext is disposed
                    _logger.LogError("Database context is disposed.");
                    // Throw an exception indicating the DbContext is disposed
                    throw new ObjectDisposedException(nameof(_context), "Database context is disposed.");
                }
                // Check if there are any pending changes to save
                if (!_context.ChangeTracker.HasChanges())
                {
                    // Log a warning if no changes are pending
                    _logger.LogWarning("No pending changes to save in the database context.");
                    // Return 0 to indicate no rows were affected
                    return 0;
                }
                // Log the number of pending changes for context
                _logger.LogDebug("Pending changes detected: {EntryCount} entries.", _context.ChangeTracker.Entries().Count());
                // Save changes to the database asynchronously and capture the number of affected rows
                int result = await _context.SaveChangesAsync();
                // Log successful save operation with the number of affected rows
                _logger.LogInformation("Changes saved successfully. Rows affected: {Rows}", result);
                // Return the number of affected rows
                return result;
            }
            // Catch concurrency exceptions during save operation
            catch (DbUpdateConcurrencyException ex)
            {
                // Log the concurrency error with exception details
                _logger.LogError(ex, "Concurrency error occurred while saving changes: {Message}", ex.Message);
                // Rethrow the concurrency exception to preserve the stack trace
                throw;
            }
            // Catch database update exceptions during save operation
            catch (DbUpdateException ex)
            {
                // Log the database update error with exception details
                _logger.LogError(ex, "Database update error occurred while saving changes: {Message}", ex.Message);
                // Rethrow the database update exception to preserve the stack trace
                throw;
            }
            // Catch any other unexpected exceptions during save operation
            catch (Exception ex)
            {
                // Log the unexpected error with exception details
                _logger.LogError(ex, "Unexpected error occurred while saving changes: {Message}", ex.Message);
                // Rethrow the unexpected exception to preserve the stack trace
                throw;
            }
        }




        #endregion


        #region Dispose

        public void Dispose()
        {
            // Log the attempt to dispose the Unit of Work
            _logger.LogDebug("Attempting to dispose UnitOfWork and associated resources.");
            // Call the protected Dispose method with disposing set to true
            Dispose(true);
            // Suppress finalization to prevent the finalizer from running
            GC.SuppressFinalize(this);
        }

        // Protected virtual Dispose method for implementing the disposable pattern
        protected virtual void Dispose(bool disposing)
        {
            // Check if the Unit of Work has already been disposed
            if (_disposed)
            {
                // Log a debug message if the Unit of Work is already disposed
                _logger.LogDebug("UnitOfWork is already disposed. No further action taken.");
                // Exit the method to prevent redundant disposal
                return;
            }
            // Check if disposing is true to handle managed resources
            if (disposing)
            {
                // Check if there is an active transaction
                if (_context?.Database?.CurrentTransaction != null)
                {
                    // Log a warning that an active transaction exists and will be rolled back
                    _logger.LogWarning("Active transaction detected during disposal. Rolling back transaction.");
                    // Roll back the active transaction to prevent partial commits
                    _context.Database.RollbackTransaction();
                    // Clear the transaction field to avoid stale references
                    _transaction = null;
                    // Log successful rollback of the transaction
                    _logger.LogInformation("Transaction rolled back during disposal.");
                }
                // Check if the repository dictionary is not empty
                if (_repos != null && !_repos.IsEmpty)
                {
                    // Log the clearing of the repository dictionary
                    _logger.LogDebug("Clearing repository dictionary during disposal.");
                    // Clear the repository dictionary to release references
                    _repos.Clear();
                }
                // Check if the DbContext is not null and not already disposed
                if (_context != null && !(_context.GetType().GetProperty("IsDisposed")?.GetValue(_context) as bool? ?? false))
                {
                    // Log the disposal of the DbContext
                    _logger.LogDebug("Disposing database context.");
                    // Dispose of the DbContext to release database connections
                    _context.Dispose();
                }
            }
            // Set the disposed flag to true to mark the Unit of Work as disposed
            _disposed = true;
            // Log successful completion of the disposal process
            _logger.LogInformation("UnitOfWork and associated resources disposed successfully.");
        }

        // Finalizer to handle cleanup if Dispose is not called
        ~UnitOfWork()
        {
            // Call the protected Dispose method with disposing set to false
            Dispose(false);
        }

        #endregion
    }
}
