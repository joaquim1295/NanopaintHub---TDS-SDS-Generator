# Windows Sandbox — checklist WebView2 (A6)

Ambiente limpo para separar bug de PC vs código. Requer **Windows 11 Pro/Enterprise** com Sandbox activo.

## Preparar ficheiros no host

1. Compilar no PC principal:
   ```powershell
   dotnet build Tools\WebView2.WinUiSmoke\WebView2.WinUiSmoke.csproj -c Debug
   dotnet build Tools\WebView2.WinFormsSmoke\WebView2.WinFormsSmoke.csproj -c Debug
   ```
2. Copiar para uma pasta partilhada (ex. `C:\Wv2SmokePack`):
   - `Tools\WebView2.WinUiSmoke\bin\Debug\net9.0-windows10.0.26100.0\win-x64\` (pasta completa)
   - `Tools\WebView2.WinFormsSmoke\bin\Debug\net9.0-windows\` (pasta completa)
   - Instalador WebView2 Evergreen: https://go.microsoft.com/fwlink/p/?LinkId=2124703
   - Instalador .NET 9 Desktop Runtime x64 (se necessário)

## Dentro do Sandbox

1. Instalar WebView2 Runtime Evergreen.
2. Instalar .NET 9 Desktop Runtime (WinForms smoke).
3. Executar `WebView2.WinFormsSmoke.exe` → verificar log `%LocalAppData%\NanopaintHub\Logs\wv2-smoke-winforms.log`.
4. Executar `WebView2.WinUiSmoke.exe` → verificar log `wv2-smoke-winui.log`.
5. Abrir `quill-host.html` no Edge (copiar também `Assets\Editor\`).

## Registar veredicto

| Teste | Host | Sandbox |
|-------|------|---------|
| diagnose-webview2.ps1 | | |
| WinForms smoke Ensure OK | | |
| WinUI smoke Ensure OK | | |
| Quill no Edge (A1) | | |

- **Sandbox OK + Host falha** → configuração do PC (AV, política, perfil corrupto).
- **Ambos falham** → investigar runtime/build.
- **Ambos OK + NanopaintHub falha** → layout/pipeline da app principal.

Registe resultados em [`docs/webview2-debug.md`](webview2-debug.md).
