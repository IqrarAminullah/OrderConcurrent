using System.Data;

namespace OrderConcurrent.Data.Interfaces
{
    public interface IDbContext : IDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction? Transaction { get; }
        void BeginTransaction();
        void Commit();
        void Rollback();
    }

}
