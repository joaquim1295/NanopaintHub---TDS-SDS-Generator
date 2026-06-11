# WebView2 / Quill — Diagnóstico e alternativas

Documento vivo para perseguir falhas de `CoreWebView2` no editor Quill e validar fallbacks.

## UI da aplicação (WinUI)

| Área | Onde |
|------|------|
| Editor Quill (produção) | **Templates** → tab **Conteúdo** → **Editar documento (Quill)** (Chromium) |
| WebView2 embebido / smoke / logs | Menu rodapé **Diagnóstico** (`DiagnosticPage`) |
| Fluxo leigo | Sem HTML nem pipeline WebView2 na página Templates |

Ver também `docs/quill-editor-alternatives.md`.

## Sintoma

- Editor visual indisponível: `CoreWebView2 não foi criado`
- **Modo HTML puro + preview Chromium** deve funcionar sem WebView2
- Runtime Evergreen reportado como instalado (ex.: v148.x)

## Logs

| Ficheiro | Conteúdo |
|----------|----------|
| `%LocalAppData%\NanopaintHub\Logs\webview2-trace.log` | Checkpoints `WV2-xxx` (NanopaintHub) |
| `%LocalAppData%\NanopaintHub\Logs\wv2-smoke-winui.log` | Smoke test WinUI |
| `%LocalAppData%\NanopaintHub\Logs\wv2-smoke-winforms.log` | Smoke test WinForms |
| `%LocalAppData%\NanopaintHub\Logs\wv2-quill-test.log` | Janela de teste Quill (isolada) |
| `%LocalAppData%\NanopaintHub\Logs\webview2-matrix-report.txt` | Relatório da matriz |
| `debug-f8703a.log` (raiz do repo) | NDJSON sessão debug agente |

## Matriz de testes isolados (máquina vs código)

### A1 — Quill no browser (sem WebView2)

```powershell
powershell scripts\open-quill-host-browser.ps1
```

| Resultado | Interpretação |
|-----------|---------------|
| Quill OK no Edge | Quill/assets **não** são o problema |
| Quill falha no Edge | Corrigir `Clients/NanopaintHub.WinUiClient/Assets/Editor/` |

**Resultado neste PC:** _preencher após verificação manual (barra Quill + rodapé «Editor pronto»)_

### A2 — Diagnóstico runtime

```powershell
powershell scripts\diagnose-webview2.ps1
```

### A3 — Smoke WinUI mínimo

```powershell
dotnet run --project Tools\WebView2.WinUiSmoke\WebView2.WinUiSmoke.csproj -c Debug
```

Log: `wv2-smoke-winui.log`. Sucesso = `Ensure OK` + example.com visível.

### A4 — Smoke WinForms (contraste)

```powershell
dotnet run --project Tools\WebView2.WinFormsSmoke\WebView2.WinFormsSmoke.csproj -c Debug
```

Log: `wv2-smoke-winforms.log`.

### A5 — Matriz completa

```powershell
powershell scripts\run-webview2-matrix.ps1
```

### A6 — Windows Sandbox / VM

Ver [`docs/sandbox-webview2-checklist.md`](sandbox-webview2-checklist.md).

### A7 — Janela teste Quill (dentro da app, sem Templates)

Na app: menu **«Teste Quill (WebView2)»** ou botão **«Janela teste Quill»** na página Templates.

- Janela WinUI separada (`QuillTestWindow`) — HWND próprio, WebView2 visível, UDF limpo, `quill-host.html`
- Log: `wv2-quill-test.log`
- Sucesso = status «SUCESSO: Quill editor-ready recebido»

| QuillTestWindow | TemplatesPage | Interpretação |
|-----------------|---------------|---------------|
| OK | Falha | Pipeline/layout Templates (Frame aninhado, timing) |
| Falha | Falha | WinUI hosting neste PC (comparar com smoke A3/A4) |
| OK | OK | Resolvido |

### Matriz de decisão

| WinForms smoke | WinUI smoke | NanopaintHub | Veredicto |
|----------------|-------------|--------------|-----------|
| Falha | Falha | Falha | PC/runtime/política/AV |
| OK | Falha | Falha | WinUI composition/HWND |
| OK | OK | Falha | Pipeline/layout NanopaintHub |
| OK | OK | OK | Resolvido |

### Evidência logs (sessão f8703a)

- `MÁQUINA OK`: runtime + `CreateEnvironment` ~13–22 ms
- Quill no browser externo: **OK** (A1 confirmado)
- `spawned=false` durante Ensure 60 s — subprocesso `msedgewebview2` não arranca para o controlo embebido
- Com `Visibility=Collapsed` no início do pipeline o Ensure arranca colapsado (WV2-002…010); corrigido para manter `Visible`
- Mesmo com `Visibility=Visible` no fim do Ensure, timeout persiste (WV2-090 ~60 s) — hipótese: composição WinUI (Frame/Page) ou processos EdgeWebView presos (`msedgewebview2Count=6` constante)

## Fallbacks implementados (sem WebView2)

| Funcionalidade | Implementação |
|----------------|---------------|
| Editar visual (leigo) | **«Editar documento (Quill)»** — Chromium headed + Quill (`IChromiumQuillEditorService`) |
| Editar HTML | Toggle **«HTML avançado»** (`TextBox` Consolas) |
| Placeholders | Botões `{{Name}}` etc. |
| Preview | `IHtmlPreviewService` — Puppeteer/Chromium (mesmo motor do PDF) |
| WebView2 embebido | Experimental (Expander) — falha neste PC |
| Quill externo | «Abrir Quill no browser» |

Ver [`docs/quill-editor-alternatives.md`](quill-editor-alternatives.md) para matriz completa de alternativas.

### Preview Chromium

- Serviço: `IHtmlPreviewService` / `HtmlPreviewService`
- UI: painel «Preview HTML (Chromium)» + «Atualizar preview»
- Partilha runtime Chromium com PDF via `ChromiumBrowserSession`

## Comandos

```powershell
# Matriz de diagnóstico
powershell scripts\run-webview2-matrix.ps1

# App principal
dotnet run --project Clients\NanopaintHub.WinUiClient\NanopaintHub.WinUIClient.csproj -c Debug

# Testes unitários WebView2
dotnet test Tests\NanopaintHub.UnitTests\NanopaintHub.UnitTests.csproj --filter WebView2
```

## Pipeline NanopaintHub (referência)

1. `WV2-033` runtime no arranque
2. Templates: HTML mode + pipeline Quill em background
3. `WebView2HostHelper` — controlo WebView2 dinâmico
4. `WebView2EnvironmentProvider` — UDF em `%LocalAppData%`, Ensure único
5. Falha WebView2 → HTML mode + preview Chromium automático
