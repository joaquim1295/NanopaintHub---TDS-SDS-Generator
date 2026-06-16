---
name: offline-sync
description: Regras fundamentais e algoritmos para manter a consistência de dados no SQLite encriptado local e governar a transmissão bidirecional com a cloud corporativa PostgreSQL de forma tolerante a falhas elétricas ou de intranet.
---

# SKILL: Sincronismo Offline-First (NanopaintHub)

Esta _skill_ ensina o agente Antigravity a criar processos em segundo plano robustos para gravação na base de dados de laboratório sem comprometer a CPU ou RAM do ambiente Windows Desktop do operador.

## 1. Padrão Unit Of Work nas Entidades de Domínio
A persistência não pode ocorrer acidentalmente nem pela metade.
- Em `NanopaintHub.Application`, os Use Cases (Serviços Lógicos) invocam métodos estúpidos nos Repositórios (`IProductRepository.AddAsync(Product)`). Estes repositórios NÃO chamam `SaveChangesAsync()`.
- O fim do processo de negócio é selado na camada aplicacional com o `IUnitOfWork.CommitAsync()`. Isto garante Atomicidade de transação SQLite.

## 2. A Máquina de Estados (SyncStatus)
Sempre que o Domínio for consultado ou atualizado, verifique a enumeração `SyncStatus`:
- `LocalOnly`: Modo default. O utilizador laboratorial trabalha em modo rascunho sem autenticação na app.
- `PendingInsert` / `PendingUpdate`: O Background Service irá acionar-se ao apanhar este sinal. Tranca o campo para leitura e proíbe o utilizador de o editar no WinUI 3 até conclusão.
- `ValidationError`: Colisões (HTTP 409). Ocorre se outro operador tentar criar o mesmo número de lote remoto, obrigando a um conflito resolvido manualmente no WinUI 3.

## 3. Prevenção de Fugas de Memória no Background
Se acionar Background Services `IHostedService` dentro da camada `NanopaintHub.WinUIClient`:
- Use **Sempre** uma `IDbContextFactory` limpa para gerar transientes da infraestrutura.
- Quando ler listas massivas (ex: Lotes históricos) para JSON Serialization e POST de rede: Adicione o modificador estrito `.AsNoTracking()`. O SQLite é rápido, mas se instanciar grafos de objetos no Tracker do EF Core num loop de serviço infinito, vai gerar falhas Catastróficas `OutOfMemoryException` no Desktop WinUI 3.
