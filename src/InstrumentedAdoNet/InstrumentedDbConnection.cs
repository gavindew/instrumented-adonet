using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace InstrumentedAdoNet
{
    /// <summary>
    /// Wraps a database connection
    /// </summary>
    [System.ComponentModel.DesignerCategory("")]
    public class InstrumentedDbConnection : DbConnection
    {
        private readonly DbConnection _connection;
        private bool _disposed;
        private IInstrumentationHandler _instrumentationHandler;

        /// <summary>
        /// Gets the current profiler instance; could be null.
        /// </summary>
        public IInstrumentationHandler InstrumentationHandler => this._instrumentationHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstrumentedDbConnection"/> class. 
        /// Returns a new <see cref="InstrumentedDbConnection"/> that wraps <paramref name="connection"/>, 
        /// providing query execution profiling. If profiler is null, no profiling will occur.
        /// </summary>
        /// <param name="connection"><c>Your provider-specific flavour of connection, e.g. SqlConnection, OracleConnection</c></param>
        /// <param name="instrumentationHandler">The currently started <see cref="InstrumentationHandler"/> or null.</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="connection"/> is <c>null</c>.</exception>
        public InstrumentedDbConnection(DbConnection connection, IInstrumentationHandler instrumentationHandler)
        {
            this._connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this._connection.StateChange += this.StateChangeHandler;

            if (instrumentationHandler != null)
            {
                this._instrumentationHandler = instrumentationHandler;
            }
        }

        /// <summary>
        /// Gets the connection that this ProfiledDbConnection wraps.
        /// </summary>
        public DbConnection WrappedConnection => this._connection;

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        public override string ConnectionString
        {
            get => this._connection.ConnectionString;
            set => this._connection.ConnectionString = value;
        }

        /// <summary>
        /// Gets the time to wait while establishing a connection before terminating the attempt and generating an error.
        /// </summary>
        public override int ConnectionTimeout => this._connection.ConnectionTimeout;

        /// <summary>
        /// Gets the name of the current database after a connection is opened, 
        /// or the database name specified in the connection string before the connection is opened.
        /// </summary>
        public override string Database => this._connection.Database;

        /// <summary>
        /// Gets the name of the database server to which to connect.
        /// </summary>
        public override string DataSource => this._connection.DataSource;

        /// <summary>
        /// Gets a string that represents the version of the server to which the object is connected.
        /// </summary>
        public override string ServerVersion => this._connection.ServerVersion;

        /// <summary>
        /// Gets the current state of the connection.
        /// </summary>
        public override ConnectionState State => this._connection.State;

        /// <summary>
        /// Changes the current database for an open connection.
        /// </summary>
        /// <param name="databaseName">The new database name.</param>
        public override void ChangeDatabase(string databaseName) => this._connection.ChangeDatabase(databaseName);

        /// <summary>
        /// Closes the connection to the database.
        /// This is the preferred method of closing any open connection.
        /// </summary>
        public override void Close()
        {
            this._connection.Close();
        }

        /// <summary>
        /// Opens a database connection with the settings specified by the <see cref="ConnectionString"/>.
        /// </summary>
        public override void Open()
        {
            this._connection.Open();
        }

        /// <summary>
        /// Asynchronously opens a database connection with the settings specified by the <see cref="ConnectionString"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this async operation.</param>
        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return this._connection.OpenAsync(cancellationToken);
        }

        /// <summary>
        /// Starts a database transaction.
        /// </summary>
        /// <param name="isolationLevel">Specifies the isolation level for the transaction.</param>
        /// <returns>An object representing the new transaction.</returns>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new InstrumentedDbTransaction(this._connection.BeginTransaction(isolationLevel), this);
        }

        /// <summary>
        /// Creates and returns a <see cref="DbCommand"/> object associated with the current connection.
        /// </summary>
        /// <returns>A <see cref="InstrumentedDbCommand"/> wrapping the created <see cref="DbCommand"/>.</returns>
        protected virtual DbCommand CreateDbCommand(DbCommand original, IInstrumentationHandler instrumentationHandler)
                => new InstrumentedDbCommand(original, this, instrumentationHandler);

        /// <summary>
        /// Creates and returns a <see cref="DbCommand"/> object associated with the current connection.
        /// </summary>
        /// <returns>A <see cref="InstrumentedDbCommand"/> wrapping the created <see cref="DbCommand"/>.</returns>
        protected override DbCommand CreateDbCommand() => this.CreateDbCommand(this._connection.CreateCommand(), this._instrumentationHandler);

        /// <summary>
        /// Dispose the underlying connection.
        /// </summary>
        /// <param name="disposing">false if preempted from a <c>finalizer</c></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !this._disposed)
            {
                this._connection.StateChange -= this.StateChangeHandler;
                this._connection.Dispose();
                this._disposed = true;
            }
            base.Dispose(disposing);
            this._instrumentationHandler = null;
        }

        /// <summary>
        /// The state change handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="stateChangeEventArguments">The state change event arguments.</param>
        private void StateChangeHandler(object sender, StateChangeEventArgs stateChangeEventArguments)
        {
            this.OnStateChange(stateChangeEventArguments);
        }

        /// <summary>
        /// Gets a value indicating whether events can be raised.
        /// </summary>
        protected override bool CanRaiseEvents => true;

        /// <summary>
        /// Enlist the transaction.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        public override void EnlistTransaction(System.Transactions.Transaction transaction) =>this._connection.EnlistTransaction(transaction);

        /// <summary>
        /// Gets the database schema.
        /// </summary>
        /// <returns>The <see cref="DataTable"/>.</returns>
        public override DataTable GetSchema() => this._connection.GetSchema();

        /// <summary>
        /// Gets the collection schema.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        /// <returns>The <see cref="DataTable"/>.</returns>
        public override DataTable GetSchema(string collectionName) => this._connection.GetSchema(collectionName);

        /// <summary>
        /// Gets the filtered collection schema.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="restrictionValues">The restriction values.</param>
        /// <returns>The <see cref="DataTable"/>.</returns>
        public override DataTable GetSchema(string collectionName, string[] restrictionValues) => this._connection.GetSchema(collectionName, restrictionValues);
    }
}
