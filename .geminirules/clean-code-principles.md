---
description: Princípios de Clean Code adaptados à Arquitetura Limpa e Offline-First do NanopaintHub.
author: Antigravity
version: 1.0
---

# Clean Code & Arquitetura Limpa (NanopaintHub)

## 1. Separação Estrita de Responsabilidades (Separation of Concerns)
O NanopaintHub usa uma Clean Architecture rígida. O código deve respeitar sempre as fronteiras das camadas:
- **`NanopaintHub.Domain`**: Onde vivem as Entidades Puras (ex: `Product`, `TestBatch`). NUNCA injete referências a bases de dados, frameworks UI ou bibliotecas de JSON aqui.
- **`NanopaintHub.Application`**: O maestro. Contém Use Cases, DTOs e Interfaces (contratos) como `IProductRepository` e `IUnitOfWork`. NUNCA referencie bibliotecas de persistência física (Entity Framework) aqui.
- **`NanopaintHub.Infrastructure`**: O músculo externo. Onde o EF Core implementa os repositórios (`NanopaintHub.LocalData`, `NanopaintHub.Persistence`) e ferramentas como o PuppeteerSharp residem (`NanopaintHub.Documents`).
- **`NanopaintHub.WinUIClient`**: Apenas gere ViewModels e Views. NUNCA faça queries diretas à DB no Code Behind XAML.

## 2. O Padrão Unit of Work
A persistência local no SQLite é governada pelo Unit of Work para garantir Atomicidade contra falhas elétricas nas fábricas.
- **Repositórios "Estúpidos"**: Os repositórios APENAS interagem com o estado em memória (`DbSet.Add`, `DbSet.Update`). Eles **NUNCA** devem chamar `SaveChangesAsync()`.
- **Maestro Inteligente**: A chamada a `CommitAsync()` (que encapsula o SaveChangesAsync) é exclusiva dos Use Cases da camada Application, garantindo que tudo é gravado num único transacional atómico.

## 3. Gestão de Contexto do EF Core (Sem Tracking Finito)
Quando construir processos em *Background* para Sincronização:
- Use sempre o padrão Factory (`IDbContextFactory`) em vez de injetar um único `DbContext` em Singleton.
- Para consultas exclusivas de leitura e envio de dados para a API (JSON export), utilize estritamente `.AsNoTracking()` para mitigar fugas de memória RAM durante processamentos longos no laboratório.
