# Editor Quill — alternativas (WebView2 indisponível)

## Veredicto neste PC

| Componente | Estado |
|------------|--------|
| Quill no browser (`file://`) | OK |
| Runtime WebView2 (`CreateEnvironment`) | OK (~20 ms) |
| WebView2 embebido WinUI (Templates + janela isolada) | **Falha** — `CoreWebView2 null` após Ensure |
| Editor Chromium (Puppeteer headed) | **Solução recomendada** |

O bloqueio é **hosting WinUI/HWND**, não Quill nem runtime.

## Solução activa: Editor Chromium (Quill)

Na página **Templates** (tab **Conteúdo**):

1. Clique **«Editar documento (Quill)»**
2. Abre janela Chromium com barra Quill (mesmo motor do preview/PDF)
3. Edite visualmente (sem HTML)
4. Clique **«Aplicar na NanopaintHub»**
5. Tab **Pré-visualização** actualiza via Chromium headless

Ferramentas WebView2 embebido: menu rodapé **«Diagnóstico»** (não no fluxo principal).

Implementação: `IChromiumQuillEditorService` + servidor HTTP local (`EditorAssetHttpServer`) + Puppeteer `Headless = false`.

## Reparar WebView2 (opcional)

Executar **PowerShell como Administrador**:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\repair-webview2-environment.ps1
```

Inclui: `dotnet restore/build`, winget WebView2 Runtime, regras firewall, limpeza cache UDF, exclusões Defender.

Se após reparo o WebView2 embebido continuar a falhar, mantenha o editor Chromium.

## Outras alternativas (não implementadas)

| Opção | Prós | Contras |
|-------|------|---------|
| **CefSharp embebido** | Quill dentro da janela WinUI | +100 MB, HWND interop WinUI3, terceiro stack Chromium |
| **RichEditBox WinUI** | Nativo, zero browser | Formatação limitada, sem placeholders ricos |
| **Word / LibreOffice + mail merge** | Familiar para utilizadores | Fora do pipeline HTML/PDF actual |
| **Editor web SaaS (TinyMCE Cloud)** | WYSIWYG completo | Internet, dados na cloud, custo |
| **Avalonia + WebView** | Outro framework UI | Reescrever cliente |

Para utilizadores **leigos**, o fluxo **«Editar documento (Quill)» → Aplicar** é o equivalente directo ao editor embebido, sem expor HTML.

Modo **«HTML avançado»** (toggle) permanece para suporte técnico.

## Comandos

```powershell
dotnet run --project Clients\NanopaintHub.WinUiClient\NanopaintHub.WinUIClient.csproj -c Debug
```

Primeira abertura do editor Chromium pode demorar (download Puppeteer Chromium, partilhado com PDF).
