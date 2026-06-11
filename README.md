# NanopaintHub

Solução .NET para dados de produtos de tinta e geração de **TDS** (ficha técnica) e **SDS** (ficha de segurança) em PDF a partir de templates HTML (Handlebars + Chromium via PuppeteerSharp).

## Estrutura

| Projeto | Função |
|---------|--------|
| `NanopaintHub.Domain` | Entidades e enums |
| `NanopaintHub.Application` | Casos de uso, contratos, exceções |
| `NanopaintHub.Infrastructure.LocalData` | EF Core + SQLite, repositórios, migrações, seed |
| `NanopaintHub.Infrastructure.Documents` | Geração de PDF |
| `NanopaintHub.WinUIClient` | Aplicação Windows (WinUI 3) para as utilizadoras |

No cliente WinUI, a navegação principal é **Produtos**, **Templates TDS/SDS** e **Gerar PDF** (shell com `NavigationView`).

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (bibliotecas)
- [.NET 9 SDK](https://dotnet.microsoft.com/download) (cliente WinUI)
- Windows 10/11 (WinUI + Puppeteer/Chromium)

## Compilar

```powershell
dotnet restore NanopaintHub.sln
dotnet build NanopaintHub.sln
```

O projeto WinUI declara `RuntimeIdentifiers` (`win-x86`, `win-x64`, `win-arm64`) para evitar **NETSDK1047** quando a solução usa plataformas diferentes. As páginas **Produtos**, **Templates** e **Gerar PDF** usam `{Binding}` em vez de `x:Bind` nos formulários: em ambientes com UI em português, combinações como `ComboBox`/`CalendarDatePicker` com `x:Bind` podem falhar no compilador XAML com **WMC9999** (mensagem genérica sobre recursos do `XamlCompiler`); consulte `obj/.../output.json` se precisar de diagnóstico.

### UX atual (WinUI)

- **Produtos:** edição de `Features`, `LiquidProperties`, `MechanicalProperties` e `FunctionalProperties` por interface dinâmica de chave/valor. O JSON é gerado automaticamente no `Save`.
- **Templates:** editor visual com **WebView2 + Quill** (`Assets/Editor/quill-host.html`) e preview HTML lado a lado, com placeholders rápidos (`{{Name}}`, `{{Code}}`, etc.).
- **Templates default:** ficheiros externos em `Assets/Templates/MinimalTds.html` e `Assets/Templates/MinimalSds.html` (sem HTML hardcoded no ViewModel).
- **Fallback híbrido do Quill:** tenta assets locais (`Assets/Editor/quill.min.js` e `quill.snow.css`) e, se falhar, usa CDN.

Executar o cliente (primeira execução descarrega o Chromium do Puppeteer):

```powershell
dotnet run --project Clients\NanopaintHub.WinUiClient\NanopaintHub.WinUIClient.csproj
```

## Dados locais (2 PCs sem servidor)

- A base **SQLite** é criada em `%LocalAppData%\NanopaintHub\nanopaint.db`.
- Na primeira execução são aplicadas as **migrações** e inserido um **conjunto mínimo** de exemplo (categoria, produto, fornecedor, componente SDS, templates TDS/SDS, lote de teste).

### Partilhar dados entre duas máquinas

1. **Uma base por PC (recomendado se editam em paralelo)**  
   Cada utilizadora mantém a sua cópia. Para alinhar dados, copiar o ficheiro `nanopaint.db` (com a app fechada) ou planear export/import manual no futuro.

2. **Uma base numa pasta de rede**  
   Só com **uma** instância da app de cada vez a escrever na base; SQLite em ficheiro partilhado é sensível a bloqueios e corrupção se duas apps gravarem em simultâneo.

3. **Servidor local no futuro**  
   A arquitetura em camadas e o enum `SyncStatus` em `TestBatch` preparam evolução para API + base central sem reescrever o domínio.

## Papel timbrado (letterhead)

- Em `DocumentTemplate.LetterheadPdfPath` pode indicar-se o caminho a uma **imagem** (`.png`, `.jpg`, `.jpeg`, `.webp`, `.gif`). Caminhos absolutos ou relativos à pasta `%LocalAppData%\NanopaintHub\`.
- **PDF como fundo** ainda não é fundido ao PDF gerado; use imagem ou aguarde integração futura (merge PDF).

## Persistência e Unit of Work

Os repositórios **não** chamam `SaveChanges` nos métodos de escrita. Após `Add`/`Update`/`Remove`, chame `await unitOfWork.CommitAsync()` no mesmo âmbito (scoped) que o `DbContext`. O seed inicial usa este padrão.

## Migrações EF Core

```powershell
cd Infrastructure\NanopaintHub.Infrastructure.LocalData
dotnet ef migrations add NomeDaMigracao --context NanopaintHubDbContext
```

## SDS e conformidade legal

A SDS gerada inclui dados de `SupplierComponent` e propriedades JSON para templates, mas **não substitui** revisão regulatória (REACH/CLP, secções obrigatórias, etc.).
