---
description: Padrões obrigatórios para componentes React específicos para agentes AI.
author: Cline Prompt Engineering Workspace
version: 1.0
category: Frontend
tags: ["react", "components", "frontend", "hooks", "standards"]
priority: 80
auto_load_when: sempre que eu criar, alterar ou trabalhar com componentes React
---

# React Components Standards

> ⚠️ **Esta regra é carregada AUTOMATICAMENTE SEMPRE QUE:**
> Eu for criar componentes, hooks ou trabalhar com React / Next.js.

---

## ✅ EU SEMPRE FAÇO, SEM EXCEÇÕES:

1. Eu SEMPRE crio componentes pequenos e de responsabilidade única
2. Todos os componentes são funções. Nunca uso classes
3. Eu SEMPRE uso Server Components por padrão
4. Adiciono `'use client'` APENAS e SOMENTE quando é absolutamente necessário
5. Eu SEMPRE extraio lógica para hooks customizados
6. Evito estado desnecessário. Preferir computed values
7. Todos os componentes tem nomes descritivos em PascalCase
8. Eu uso children e composição ao invés de props hell

---

## ❌ EU NUNCA FAÇO, SOB NENHUMA CIRCUNSTÂNCIA:

1. ❌ Nunca crio componentes com mais de 200 linhas
2. ❌ Nunca adiciono `'use client'` em todos os componentes
3. ❌ Nunca uso useEffect para atualizar estado derivado
4. ❌ Nunca passo mais de 5 props para um componente
5. ❌ Nunca faço fetching de dados dentro de useEffect
6. ❌ Nunca uso indexes como key em listas
7. ❌ Nunca crio componentes dentro de outros componentes
8. ❌ Nunca escondo erros com try/catch vazio

---

## ✅ Checklist OBRIGATÓRIO que eu executo ANTES de entregar QUALQUER trabalho:

- [ ] Apenas componentes que precisam de interatividade tem `'use client'`
- [ ] Não existe nenhum useEffect desnecessário
- [ ] Todas as listas tem key única e não é index
- [ ] Nenhum componente tem mais de 5 props
- [ ] Nenhum componente tem mais de 200 linhas
- [ ] Toda a lógica reutilizável está em hooks customizados
- [ ] Não existe estado que pode ser computado

---

## 📝 Notas Adicionais:

> Quando em dúvida: o componente mais simples é sempre o melhor componente.
> Esta regra tem precedência sobre todas as outras regras genéricas sempre que eu trabalhar com React.