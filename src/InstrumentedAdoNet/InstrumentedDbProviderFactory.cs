using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security.Permissions;

namespace InstrumentedAdoNet
{
    /// <summary>
    /// Wrapper for a database provider factory to enable instrumentation
    /// </summary>
    public class InstrumentedDbProviderFactory : DbProviderFactory
    {
        private DbProviderFactory _factory;
        private readonly IInstrumentationHandler _instrumentationHandler;

        /// <summary>
        /// The <see cref="DbProviderFactory"/> that this version wraps.
        /// </summary>
        public DbProviderFactory WrappedDbProviderFactory => this._factory;

        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "This does not appear to be used anywhere, we need to refactor it.")]
        public readonly static InstrumentedDbProviderFactory Instance = new InstrumentedDbProviderFactory();

        /// <summary>
        /// Initializes a new instance of the <see cref="InstrumentedDbProviderFactory"/> class.
        /// A proxy provider factory
        /// </summary>
        /// <param name="factory">The provider factory to wrap.</param>
        /// <param name="instrumentationHandler"></param>
        /// <remarks>
        /// </remarks>
        public InstrumentedDbProviderFactory(DbProviderFactory factory, IInstrumentationHandler instrumentationHandler)
        {
            this._factory = factory;
            this._instrumentationHandler = instrumentationHandler;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="InstrumentedDbProviderFactory"/> class from being created.
        /// Used for database provider APIS internally
        /// </summary>
        private InstrumentedDbProviderFactory()
        {
        }

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbCommand"/> class.</summary>
        /// <returns>A new instance of <see cref="DbCommand"/>.</returns>
        public override DbCommand CreateCommand()
        {
            var command = this._factory.CreateCommand();

            return _instrumentationHandler != null 
                ? new InstrumentedDbCommand(command, null, _instrumentationHandler)
                : command;
        }

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbConnection"/> class.</summary>
        /// <returns>A new instance of <see cref="DbConnection"/>.</returns>
        public override DbConnection CreateConnection()
        {
            var connection = this._factory.CreateConnection();

            return _instrumentationHandler != null
                ? new InstrumentedDbConnection(connection, _instrumentationHandler)
                : connection;
        }

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbConnectionStringBuilder"/> class.</summary>
        /// <returns>A new instance of <see cref="DbConnectionStringBuilder"/>.</returns>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder() => this._factory.CreateConnectionStringBuilder();

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbParameter"/> class.</summary>
        /// <returns>A new instance of <see cref="DbParameter"/>.</returns>
        public override DbParameter CreateParameter() => this._factory.CreateParameter();

        /// <summary>
        /// Allow to re-initialize the provider factory.
        /// </summary>
        /// <param name="tail">The tail.</param>
        public void InitProfiledDbProviderFactory(DbProviderFactory tail) => this._factory = tail;

        /// <summary>
        /// Specifies whether the specific <see cref="DbProviderFactory"/> supports the <see cref="DbDataSourceEnumerator"/> class.
        /// </summary>
        public override bool CanCreateDataSourceEnumerator => this._factory.CanCreateDataSourceEnumerator;

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbCommandBuilder"/> class.</summary>
        /// <returns>A new instance of <see cref="DbCommandBuilder"/>.</returns>
        public override DbCommandBuilder CreateCommandBuilder() => this._factory.CreateCommandBuilder();

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbDataAdapter"/> class.</summary>
        /// <returns>A new instance of <see cref="DbDataAdapter"/>.</returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            var dataAdapter = this._factory.CreateDataAdapter();

            return _instrumentationHandler != null
                ? new InstrumentedDbDataAdapter(dataAdapter, _instrumentationHandler)
                : dataAdapter;
        }

        /// <summary>Returns a new instance of the provider's class that implements the <see cref="DbDataSourceEnumerator"/> class.</summary>
        /// <returns>A new instance of <see cref="DbDataSourceEnumerator"/>.</returns>
        public override DbDataSourceEnumerator CreateDataSourceEnumerator() => this._factory.CreateDataSourceEnumerator();
    }
}
