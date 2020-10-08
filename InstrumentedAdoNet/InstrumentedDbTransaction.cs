using System;
using System.Data;
using System.Data.Common;

namespace InstrumentedAdoNet
{
    /// <summary>
    /// The profiled database transaction.
    /// </summary>
    public class InstrumentedDbTransaction : DbTransaction
    {
        private InstrumentedDbConnection _connection;
        private DbTransaction _transaction;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstrumentedDbTransaction"/> class.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="connection">The connection.</param>
        /// <exception cref="ArgumentNullException">Throws when the <paramref name="transaction"/> or <paramref name="connection"/> is <c>null</c>.</exception>
        public InstrumentedDbTransaction(DbTransaction transaction, InstrumentedDbConnection connection)
        {
            this._transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this._connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Gets the database connection.
        /// </summary>
        protected override DbConnection DbConnection => this._connection;

        /// <summary>
        /// Gets the wrapped transaction.
        /// </summary>
        public DbTransaction WrappedTransaction => this._transaction;

        /// <summary>
        /// Gets the isolation level.
        /// </summary>
        public override IsolationLevel IsolationLevel => this._transaction.IsolationLevel;

        /// <summary>
        /// Commits the database transaction.
        /// </summary>
        public override void Commit() => this._transaction.Commit();

        /// <summary>
        /// Rolls back a transaction from a pending state.
        /// </summary>
        public override void Rollback() => this._transaction.Rollback();

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="DbTransaction"/>.
        /// </summary>
        /// <param name="disposing">false if being called from a <c>finalizer</c></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && this._transaction != null)
            {
                this._transaction.Dispose();
            }
            this._transaction = null;
            this._connection = null;
            base.Dispose(disposing);
        }
    }
}
