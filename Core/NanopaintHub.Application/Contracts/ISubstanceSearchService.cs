using System.Collections.Generic;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

namespace NanopaintHub.Application.Contracts;

public interface ISubstanceSearchService
{
    Task<List<EchaSubstance>> SearchAsync(string query);
}
