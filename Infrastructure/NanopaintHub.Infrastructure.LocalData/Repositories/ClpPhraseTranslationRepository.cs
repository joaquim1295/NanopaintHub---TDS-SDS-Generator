using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.LocalData.Persistence;

namespace NanopaintHub.Infrastructure.LocalData.Repositories;

public class ClpPhraseTranslationRepository : IClpPhraseTranslationRepository
{
    private readonly NanopaintHubDbContext _context;

    public ClpPhraseTranslationRepository(NanopaintHubDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ClpPhraseTranslation>> GetTranslationsAsync(
        IEnumerable<string> hazardCodes, string languageCode)
    {
        var codes = hazardCodes.ToList();

        // Procura exacta no idioma pedido
        var results = await _context.ClpPhraseTranslations
            .Where(t => codes.Contains(t.ClpPhraseCode) && t.LanguageCode == languageCode)
            .OrderBy(t => t.ClpPhraseCode)
            .ThenBy(t => t.SectionType)
            .ToListAsync();

        // Fallback para EN se não houver traduções no idioma pedido
        if (!results.Any() && languageCode != "EN")
        {
            results = await _context.ClpPhraseTranslations
                .Where(t => codes.Contains(t.ClpPhraseCode) && t.LanguageCode == "EN")
                .OrderBy(t => t.ClpPhraseCode)
                .ThenBy(t => t.SectionType)
                .ToListAsync();
        }

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<string>> GetAvailableLanguagesAsync()
    {
        return await _context.ClpPhraseTranslations
            .Select(t => t.LanguageCode)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();
    }
}
