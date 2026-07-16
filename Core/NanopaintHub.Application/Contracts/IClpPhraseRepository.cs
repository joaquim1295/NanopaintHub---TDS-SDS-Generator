using System.Collections.Generic;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

namespace NanopaintHub.Application.Contracts;

public interface IClpPhraseRepository
{
    Task<IEnumerable<ClpPhrase>> GetAllAsync();
    Task<ClpPhrase?> GetByCodeAsync(string code);
}
