using System;
using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Moq;
using NUnit.Framework;

namespace InstrumentedAdoNet.UnitTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test_ExceptionIsPickedUp()
        {
            var mockInstrumenter = new Mock<IInstrumentationHandler>();
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            
            var instrumentedConnection = new InstrumentedDbConnection(connection, mockInstrumenter.Object);
            
            // Force an error and confirm the instrumenter picks it up
            try
            {
                instrumentedConnection.ExecuteScalar("SELECT * FROM NonExistentTable");
                
                throw new Exception("Should not get here");
            }
            catch (Exception e)
            {
            }

            mockInstrumenter.Verify(x => x.ExecuteStart(It.IsAny<IDbCommand>(), SqlExecuteType.Scalar));
            mockInstrumenter.Verify(x => x.OnError(It.IsAny<InstrumentedDbCommand>(), SqlExecuteType.Scalar, It.IsAny<Exception>()));
        }

        [Test]
        public void Test_OpenReaderWorks()
        {
            var mockInstrumenter = new Mock<IInstrumentationHandler>();
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            var instrumentedConnection = new InstrumentedDbConnection(connection, mockInstrumenter.Object);

            // Force an error and confirm the instrumenter picks it up
            instrumentedConnection.Execute("CREATE TABLE T (Id int null)");
            instrumentedConnection.Execute("INSERT INTO T (Id) VALUES (1), (2)");
            var cmd = instrumentedConnection.CreateCommand();
            cmd.CommandText = "SELECT * FROM T";
            
            using var reader = cmd.ExecuteReader();
            
            reader.NextResult();
            reader.NextResult();
        }
    }
}