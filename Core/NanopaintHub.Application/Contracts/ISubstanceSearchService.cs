using System.Collections.Generic;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

using NanopaintHub.Application.DTOs;

namespace NanopaintHub.Application.Contracts;

public interface ISubstanceSearchService
{
    Task<List<FederatedSearchResult>> SearchAsync(string query);
}
