# instrumented-adonet

Basic library to be able to instrument adonet calls.

Borrows code from https://github.com/MiniProfiler/dotnet to wrap DbConnection and DbCommand with some basic instrumentation so we can handle events.

Basic usage:

Step 1 : Create an implementation of IInstrumentationHandler

``` c#

public class BasicConsoleLoggingInstrumenter : IInstrumentationHandler
    {
        public void ExecuteStart(IDbCommand instrumentedDbCommand, SqlExecuteType executeType)
        {
            Console.WriteLine($"ExecuteStart:{instrumentedDbCommand.CommandText}");
        }

        public void ExecuteFinish(IDbCommand instrumentedDbCommand, SqlExecuteType executeType, DbDataReader reader)
        {
            Console.WriteLine($"ExecuteFinish:{instrumentedDbCommand.CommandText}");
        }

        public void OnError(IDbCommand instrumentedDbCommand, SqlExecuteType executeType, Exception exception)
        {
            Console.WriteLine($"OnError:{instrumentedDbCommand.CommandText}\n{exception.Message}");
        }
    }
```

Step 2 : Ensure you created and use the InstrumentConnection

The code below assumes an in-memory Sqlite connection

```
var instrumenter = new BasicConsoleLoggingIstrumenter();
var connection = new SqlConnection("DataSource=:memory:");

var instrumentedConnection = new InstrumentedDbConnection(connection, instrumenter);
```