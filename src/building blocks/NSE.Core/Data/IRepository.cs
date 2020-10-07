using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NSE.Core.Data
{
    public interface IRepository<T, TKey> : IDisposable where T : IAggregateRoot
    {
        Task<T> ObterPorId(TKey id);
        Task<IEnumerable<T>> ObterTodos();
        Task<IEnumerable<T>> Encontrar(Expression<Func<T, bool>> predicate);

        void Adicionar(T entity);
        void Atualizar(T entity);
    }
}
