using System;
using System.Data.Common;

namespace InstrumentedAdoNet
{
    public partial class InstrumentedDbCommand : ICloneable
    {
        /// <summary>
        /// Clone the command, Entity Framework expects this behavior.
        /// </summary>
        /// <returns>The <see cref="InstrumentedDbCommand"/>.</returns>
        object ICloneable.Clone()
        {
            var tail = this._command as ICloneable ?? throw new NotSupportedException("Underlying " + this._command.GetType().Name + " is not cloneable");
            return new InstrumentedDbCommand((DbCommand)tail.Clone(), this._connection, this._instrumentationHandler);
        }
    }
}
