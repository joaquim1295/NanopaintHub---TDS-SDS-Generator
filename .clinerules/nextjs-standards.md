---
description: Padrões obrigatórios para Next.js App Router específicos para agentes AI.
author: Cline Prompt Engineering Workspace
version: 1.0
category: Frontend
tags: ["nextjs", "app-router", "fullstack", "react", "standards"]
priority: 80
auto_load_when: sempre que eu trabalhar em projetos Next.js
---

# Next.js App Router Standards

> ⚠️ **Esta regra é carregada AUTOMATICAMENTE SEMPRE QUE:**
> Eu for trabalhar em projetos Next.js com App Router.

---

## 🚧 WORKFLOW OBRIGATÓRIO

Tu deves seguir SEMPRE esta sequência, sem exceções:

1.  **Entender**
    Identifica primeiro: objetivo, restrições, ficheiros envolvidos, dependencias.

2.  **Planear**
    Explica a abordagem que vais usar ANTES de escrever qualquer código.

3.  **Implementar**
    Escreve código limpo, mínimo e seguindo todos os standards abaixo.

4.  **Revisar**
    Verifica que não existe duplicação, que os ficheiros estão no sitio certo, que respeita a arquitetura.

---

## ✅ EU SEMPRE FAÇO, SEM EXCEÇÕES:

1. Eu SEMPRE sigo a estrutura oficial do Next.js:
   ```
   app/          # Rotas e Server Components
   components/   # Componentes partilhados
   lib/          # Utilitários e lógica
   models/       # Modelos de base de dados
   public/       # Ficheiros estáticos
   ```
2. Toda a lógica de servidor fica SEMPRE no lado do servidor
3. Eu SEMPRE faço fetching de dados diretamente nos Server Components
4. Todas as Server Actions são criadas corretamente
5. Eu SEMPRE uso `cache`, `revalidate` e `dynamic` corretamente
6. Navegação é feita SEMPRE com o componente `<Link>`
7. Todos os erros são tratados com `error.tsx` por rota
8. Loading states são implementados com `loading.tsx`

---

## ❌ EU NUNCA FAÇO, SOB NENHUMA CIRCUNSTÂNCIA:

1. ❌ Nunca uso `'use client'` sem necessidade absoluta
2. ❌ Nunca faço fetching de dados dentro de Client Components
3. ❌ Nunca exponho secrets do servidor no cliente
4. ❌ Nunca crio páginas sem tratamento de erro e loading
5. ❌ Nunca uso `window` sem verificar se estamos no cliente
6. ❌ Nunca faço revalidação desnecessária
7. ❌ Nunca implemento autenticação no lado do cliente
8. ❌ Nunca uso `any` nos componentes Next.js

---

## ✅ Checklist OBRIGATÓRIO que eu executo ANTES de entregar QUALQUER trabalho:

- [ ] Apenas os componentes que precisam de interatividade tem `'use client'`
- [ ] Todo o fetching de dados é feito em Server Components
- [ ] Todas as rotas tem `error.tsx` e `loading.tsx`
- [ ] Nenhum secret do servidor é exposto
- [ ] A estrutura de pastas segue o padrão oficial
- [ ] Todas as Server Actions estão protegidas
- [ ] Não existe nenhum acesso direto a `window`

---

## 📝 Notas Adicionais:

> Server Components é o padrão. Client Components é a exceção.
> Esta regra tem precedência sobre todas as outras regras genéricas sempre que eu trabalhar com Next.js.