using NanopaintHub.Application.Contracts;
using NanopaintHub.Infrastructure.Documents.Services;

namespace NanopaintHub.UnitTests.Documents;

public sealed class ChromiumTemplateEditorModeTests
{
    [Theory]
    [InlineData(ChromiumTemplateEditorMode.WordLike, "wysiwyg-host.html")]
    [InlineData(ChromiumTemplateEditorMode.QuillSimple, "quill-host.html")]
    public void ResolveHostPage_ReturnsExpectedFile(ChromiumTemplateEditorMode mode, string expected)
    {
        Assert.Equal(expected, ChromiumTinyMceEditorService.ResolveHostPage(mode));
    }

    [Fact]
    public async Task MinimalTdsAsset_ContainsPremiumLayoutAndHandlebars()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Templates", "MinimalTds.html");
        Assert.True(File.Exists(path), $"Asset em falta: {path}");

        var html = await File.ReadAllTextAsync(path);
        Assert.Contains("header-banner", html, StringComparison.Ordinal);
        Assert.Contains("{{Name}}", html, StringComparison.Ordinal);
        Assert.Contains("{{#each Features}}", html, StringComparison.Ordinal);
        Assert.Contains("Assets.Logo", html, StringComparison.Ordinal);
    }

    [Fact]
    public void WysiwygHostAsset_ExistsInEditorFolder()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Editor", "wysiwyg-host.html");
        Assert.True(File.Exists(path), $"Asset em falta: {path}");
        var html = File.ReadAllText(path);
        Assert.Contains("HANDLEBARS_PROTECT", html, StringComparison.Ordinal);
        Assert.Contains("entity_encoding: \"raw\"", html, StringComparison.Ordinal);
    }
}
