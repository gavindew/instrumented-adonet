using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace InstrumentedAdoNet
{
    /// <summary>
    /// The instrumented database command.
    /// </summary>
    public partial class InstrumentedDbCommand : DbCommand
    {
        private readonly DbCommand _command;
        private IInstrumentationHandler _instrumentationHandler;
        private DbConnection _connection;
        private DbTransaction _transaction;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstrumentedDbCommand"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="profiler">The profiler.</param>
        /// <param name="instrumentationHandler">A handler to handle instrumentation events</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="command"/> is <c>null</c>.</exception>
        public InstrumentedDbCommand(DbCommand command, DbConnection connection, IInstrumentationHandler instrumentationHandler)
        {
            this._command = command ?? throw new ArgumentNullException(nameof(command));
            this._instrumentationHandler = instrumentationHandler;

            if (connection != null)
            {
                this._connection = connection;
                this.UnwrapAndAssignConnection(connection);
            }
        }

        /// <summary>
        /// Gets or sets the text command to run against the data source.
        /// </summary>
        public override string CommandText
        {
            get => this._command.CommandText;
            set => this._command.CommandText = value;
        }

        /// <summary>
        /// Gets or sets the command timeout.
        /// </summary>
        public override int CommandTimeout
        {
            get => this._command.CommandTimeout;
            set => this._command.CommandTimeout = value;
        }

        /// <summary>
        /// Gets or sets the command type.
        /// </summary>
        public override CommandType CommandType
        {
            get => this._command.CommandType;
            set => this._command.CommandType = value;
        }

        /// <summary>
        /// Gets or sets the database connection.
        /// </summary>
        protected override DbConnection DbConnection
        {
            get => this._connection;
            set
            {
                this._connection = value;
                this.UnwrapAndAssignConnection(value);
            }
        }

        private void UnwrapAndAssignConnection(DbConnection value)
        {
            if (value is InstrumentedDbConnection instrumentedDbConnection)
            {
                this._instrumentationHandler = instrumentedDbConnection.InstrumentationHandler;
                this._command.Connection = instrumentedDbConnection.WrappedConnection;
            }
            else
            {
                this._command.Connection = value;
            }
        }

        /// <summary>
        /// Gets the database parameter collection.
        /// </summary>
        protected override DbParameterCollection DbParameterCollection => this._command.Parameters;

        /// <summary>
        /// Gets or sets the database transaction.
        /// </summary>
        protected override DbTransaction DbTransaction
        {
            get => this._transaction;
            set
            {
                this._transaction = value;
                this._command.Transaction = value is InstrumentedDbTransaction awesomeTran ? awesomeTran.WrappedTransaction : value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the command is design time visible.
        /// </summary>
        public override bool DesignTimeVisible
        {
            get => this._command.DesignTimeVisible;
            set => this._command.DesignTimeVisible = value;
        }

        /// <summary>
        /// Gets or sets the updated row source.
        /// </summary>
        public override UpdateRowSource UpdatedRowSource
        {
            get => this._command.UpdatedRowSource;
            set => this._command.UpdatedRowSource = value;
        }

        /// <summary>
        /// Creates a wrapper data reader for <see cref="ExecuteDbDataReader"/> and <see cref="ExecuteDbDataReaderAsync"/> />
        /// </summary>
        protected virtual DbDataReader CreateDbDataReader(DbDataReader original, CommandBehavior behavior, IInstrumentationHandler instrumentationHandler)
            => new InstrumentedDbDataReader(original, behavior, instrumentationHandler);

        /// <summary>
        /// Executes a database data reader.
        /// </summary>
        /// <param name="behavior">The command behavior to use.</param>
        /// <returns>The resulting <see cref="DbDataReader"/>.</returns>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            DbDataReader result = null;
            if (this._instrumentationHandler == null)
            {
                result = this._command.ExecuteReader(behavior);
                return CreateDbDataReader(result, behavior, null);
            }

            this._instrumentationHandler.ExecuteStart(this, SqlExecuteType.Reader);
            try
            {
                result = this._command.ExecuteReader(behavior);
                result = this.CreateDbDataReader(result, behavior, this._instrumentationHandler);
            }
            catch (Exception e)
            {
                this._instrumentationHandler.OnError(this, SqlExecuteType.Reader, e);
                throw;
            }
            finally
            {
                this._instrumentationHandler.ExecuteFinish(this, SqlExecuteType.Reader, result);
            }

            return result;
        }

        /// <summary>
        /// Executes a database data reader asynchronously.
        /// </summary>
        /// <param name="behavior">The command behavior to use.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this async operation.</param>
        /// <returns>The resulting <see cref="DbDataReader"/>.</returns>
        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            DbDataReader result = null;
            if (this._instrumentationHandler == null)
            {
                return await this._command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
            }

            this._instrumentationHandler.ExecuteStart(this, SqlExecuteType.Reader);
            try
            {
                result = await this._command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
                result = this.CreateDbDataReader(result, behavior, this._instrumentationHandler);
            }
            catch (Exception e)
            {
                this._instrumentationHandler.OnError(this, SqlExecuteType.Reader, e);
                throw;
            }
            finally
            {
                this._instrumentationHandler.ExecuteFinish(this, SqlExecuteType.Reader, result);
            }

            return result;
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <returns>The number of rows affected.</returns>
        public override int ExecuteNonQuery()
        {
            if (this._instrumentationHandler == null)
            {
                return this._command.ExecuteNonQuery();
            }

            int result;
            this._instrumentationHandler.ExecuteStart(this, SqlExecuteType.NonQuery);
            try
            {
                result = this._command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                this._instrumentationHandler.OnError(this, SqlExecuteType.NonQuery, e);
                throw;
            }
            finally
            {
                this._instrumentationHandler.ExecuteFinish(this, SqlExecuteType.NonQuery, null);
            }

            return result;
        }

        /// <summary>
        /// Asynchronously executes a SQL statement against a connection object asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this async operation.</param>
        /// <returns>The number of rows affected.</returns>
        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            if (this._instrumentationHandler == null)
            {
                return await this._command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            int result;
            this._instrumentationHandler.ExecuteStart(this, SqlExecuteType.NonQuery);
            try
            {
                result = await this._command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this._instrumentationHandler.OnError(this, SqlExecuteType.NonQuery, e);
                throw;
            }
            finally
            {
                this._instrumentationHandler.ExecuteFinish(this, SqlExecuteType.NonQuery, null);
            }

            return result;
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query. 
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the result set.</returns>
        public override object ExecuteScalar()
        {
            if (this._instrumentationHandler == null)
            {
                return this._command.ExecuteScalar();
            }

            object result;
            this._instrumentationHandler.ExecuteStart(this, SqlExecuteType.Scalar);
            try
            {
                result = this._command.ExecuteScalar();
            }
            catch (Exception e)
            {
                this._instrumentationHandler.OnError(this, SqlExecuteType.Scalar, e);
                throw;
            }
            finally
            {
                this._instrumentationHandler.ExecuteFinish(this, SqlExecuteType.Scalar, null);
            }

            return result;
        }

        /// <summary>
        /// Asynchronously executes the query, and returns the first column of the first row in the result set returned by the query. 
        /// Additional columns or rows are ignored.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this async operation.</param>
        /// <returns>The first column of the first row in the result set.</returns>
        public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            if (this._instrumentationHandler == null)
            {
                return await this._command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }

            object result;
            this._instrumentationHandler.ExecuteStart(this, SqlExecuteType.Scalar);
            try
            {
                result = await this._command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this._instrumentationHandler.OnError(this, SqlExecuteType.Scalar, e);
                throw;
            }
            finally
            {
                this._instrumentationHandler.ExecuteFinish(this, SqlExecuteType.Scalar, null);
            }

            return result;
        }

        /// <summary>
        /// Attempts to cancels the execution of this command.
        /// </summary>
        public override void Cancel() => this._command.Cancel();

        /// <summary>
        /// Creates a prepared (or compiled) version of the command on the data source.
        /// </summary>
        public override void Prepare() => this._command.Prepare();

        /// <summary>
        /// Creates a new instance of an <see cref="DbParameter"/> object.
        /// </summary>
        /// <returns>The <see cref="DbParameter"/>.</returns>
        protected override DbParameter CreateDbParameter() => this._command.CreateParameter();

        /// <summary>
        /// Releases all resources used by this command.
        /// </summary>
        /// <param name="disposing">false if this is being disposed in a <c>finalizer</c>.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !this._disposed)
            {
                this._command.Dispose();
                this._disposed = true;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the internal command.
        /// </summary>
        public DbCommand InternalCommand => this._command;
    }
}
