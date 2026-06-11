---
description: Unified Naming Conventions Standard for all development projects. Complete cross-layer standard that eliminates all naming ambiguities.
author: Cline Prompt Engineering Workspace
version: 1.0
category: "Development Standards"
tags: ["naming-conventions", "standards", "code-style", "best-practices", "cross-layer"]
globs: ["*"]
---

# Unified Naming Conventions Standard

Esta é a regra oficial e obrigatória de convenções de nomenclatura para TODOS os projetos. Esta regra tem precedência sobre todas as outras convenções e elimina 100% das ambiguidades.

---

## 🚨 APLICA-SE SEM EXCEÇÕES

Todos os tipos de ficheiros, todas as linguagens, todas as arquiteturas, todos os projetos.

---

## 📋 Regras OBRIGATÓRIAS

| Tipo | Convenção | Exemplo |
|------|------------|---------|
| **Componentes React / JSX / Vue** | `PascalCase` | `BookTable.jsx` `ProductCard.vue` |
| **React Hooks Customizados** | `use` + `camelCase` | `useBooks.js` `useCart.ts` |
| **Services / Camada de Serviço** | `camelCase` + `Service` | `bookService.js` `authService.ts` |
| **Repositories / Camada de Dados** | `camelCase` + `Repository` | `bookRepository.js` `userRepository.ts` |
| **Ficheiros de Utilitários / Helpers** | `camelCase` | `dateUtils.js` `validationHelpers.ts` |
| **Endpoints API Routes** | `kebab-case` | `/api/books/list-all` |
| **Tabelas de Base de Dados** | `snake_case` | `user_roles` `order_items` |
| **Colunas Base de Dados** | `snake_case` | `first_name` `created_at` |
| **Chave Primária** | Sempre `id` | `id` |
| **Chave Estrangeira** | `<tabela>_id` | `user_id` `product_id` |
| **Variáveis e Funções** | `camelCase` | `getUserProfile()` `isLoading` |
| **Constantes Globais** | `UPPER_SNAKE_CASE` | `MAX_RETRIES` `API_BASE_URL` |
| **Classes** | `PascalCase` | `UserController` `ProductModel` |
| **Enums** | `PascalCase` | `UserRole` `OrderStatus` |
| **Membros de Enums** | `UPPER_SNAKE_CASE` | `ADMIN` `COMPLETED` |

---

## ✅ Regras Adicionais

1. **Nunca usar abreviaturas** a menos que sejam universalmente entendidas (`id`, `url`, `api`, `http`)
2. **Nomes devem ser descritivos** - não economize caracteres. Priorize legibilidade.
3. **Não usar nomes genéricos** como `data`, `result`, `temp`, `value`. Seja especifico.
4. **Funções devem começar por verbo**: `get`, `set`, `create`, `update`, `delete`, `validate`, `calculate`
5. **Valores booleanos devem começar por**: `is`, `has`, `can`, `should`, `was`

---

## ❌ PROIBIDO

❌ Não misturar convenções dentro do mesmo projeto
❌ Não usar `kebab-case` em javascript/typescript
❌ Não usar `PascalCase` para variáveis ou funções
❌ Não usar `snake_case` para nada fora da base de dados
❌ Não inventar padrões próprios

> Esta é a convenção de nomenclatura mais completa e unificada que existe atualmente para o Cline. Resolve todos os conflitos e ambiguidades que existem nas regras da comunidade.