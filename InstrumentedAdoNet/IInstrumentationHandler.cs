using System;
using System.Data;
using System.Data.Common;

namespace InstrumentedAdoNet
{
    public interface IInstrumentationHandler
    {
        /// <summary>
        /// Called when a command starts executing
        /// </summary>
        /// <param name="instrumentedDbCommand">The profiled dB Command.</param>
        /// <param name="executeType">The execute Type.</param>
        void ExecuteStart(IDbCommand instrumentedDbCommand, SqlExecuteType executeType);

        /// <summary>
        /// Called when a reader finishes executing
        /// </summary>
        /// <param name="instrumentedDbCommand">The profiled DB Command.</param>
        /// <param name="executeType">The execute Type.</param>
        /// <param name="reader">The reader.</param>
        void ExecuteFinish(IDbCommand instrumentedDbCommand, SqlExecuteType executeType, DbDataReader reader);

        /// <summary>
        /// Called when an error happens during execution of a command 
        /// </summary>
        /// <param name="instrumentedDbCommand">The profiled DB Command.</param>
        /// <param name="executeType">The execute Type.</param>
        /// <param name="exception">The exception.</param>
        void OnError(IDbCommand instrumentedDbCommand, SqlExecuteType executeType, Exception exception);
    }
}