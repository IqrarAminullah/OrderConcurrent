using Microsoft.Data.SqlClient;
using OrderConcurrent.Data.Interfaces;
using System.Data;

namespace OrderConcurrent.Data
{
    public class DbContext : IDbContext
    {
        private readonly string _connectionString;
        private IDbConnection? _connection;
        private IDbTransaction? _transaction;

        public DbContext(string connectionString)
        {
            _connectionString = connectionString;
        }   

        public IDbConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = new SqlConnection(_connectionString);
                    _connection.Open();
                }
                return _connection;
            }
        }

        public IDbTransaction? Transaction => _transaction;

        public void BeginTransaction() => _transaction = Connection.BeginTransaction(IsolationLevel.Serializable);

        public void Commit()
        {
            _transaction?.Commit();
            DisposeTransaction();
        }

        public void Rollback()
        {
            _transaction?.Rollback();
            DisposeTransaction();
        }

        private void DisposeTransaction()
        {
            _transaction?.Dispose();
            _transaction = null;
        }

        public void Dispose()
        {
            DisposeTransaction();
            _connection?.Dispose();
            _connection = null;
        }
    }
}
