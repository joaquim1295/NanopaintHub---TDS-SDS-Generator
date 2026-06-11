namespace NanopaintHub.Application.Contracts;
using System.Threading.Tasks;

public interface IUnitOfWork
{
    Task<int> CommitAsync();
}

