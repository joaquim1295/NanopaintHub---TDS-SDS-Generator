---
description: Clean Code Principles adapted specifically for AI agents. Obligatory practical implementation of KISS, SOLID, DRY, YAGNI with no exceptions.
author: Cline Prompt Engineering Workspace
version: 1.0
category: "Development Standards"
tags: ["clean-code", "solid", "kiss", "dry", "yagni", "code-quality"]
globs: ["*"]
priority: 80
---

# Clean Code Principles for AI Agents

Esta regra obriga à aplicação prática dos princípios de programação limpa. NÃO EXISTE `where appropriate`. Estes princípios aplicam-se SEMPRE, SEM EXCEÇÕES, em TODOS os ficheiros e TODAS as linguagens.

---

## 🚨 REGRA FUNDAMENTAL

Tu SABES o que são estes princípios. Tu consegues explicar-los melhor que 90% dos programadores humanos. Agora tu vais APLICAR-OS na prática, em TODO o código que produzes.

Não existe "quando for apropriado". Não existe exceções. Se não seguires estes princípios o código que produzes é automaticamente inválido.

---

## 📋 Princípios OBRIGATÓRIOS

| Princípio | Definição PRÁTICA para o agente |
|-----------|----------------------------------|
| **KISS** | Mantem tudo o mais simples possivel. Não adicionas funcionalidade extra, não crias abstrações, não implementas coisas "para o futuro". Resolves APENAS o problema que tens à frente. |
| **DRY** | NUNCA duplicas código. Se escreveres a mesma linha duas vezes, tu tens um bug. Cria uma função, cria um helper, cria um componente. Qualquer coisa menos duplicação. |
| **YAGNI** | Tu NÃO implementas nada que não seja explicitamente pedido. Nenhuma funcionalidade extra, nenhuma otimização prematura, nenhum "este pode ser util mais tarde". Se não estiver nos requisitos, não existe. |
| **SOLID** | Aplicas os principios SOLID até ao nivel que faz sentido. Não crias 10 interfaces só para cumprir a regra. Mas tu NUNCA crias uma classe com mais de uma responsabilidade. |

---

## ✅ Checklist OBRIGATÓRIO de Auto Correção

ANTES de apresentar QUALQUER código, tu tens que responder internamente a estas perguntas:

1. ❓ Este código é a solução MAIS SIMPLES possivel para este problema?
2. ❓ Existe alguma duplicação que pode ser eliminada?
3. ❓ Estou a adicionar alguma coisa que não foi pedida?
4. ❓ Cada função faz exatamente uma coisa e só uma coisa?
5. ❓ Cada classe tem exatamente uma responsabilidade?
6. ❓ Eu consigo explicar este código em uma frase?

Se a resposta a QUALQUER destas perguntas for NÃO, tu apagas o código e voltas a escrever.

---

## ❌ PROIBIDO ABSOLUTAMENTE

❌ Não crias padrões de desenho só porque são fixes
❌ Não adicionas funcionalidades "para o futuro"
❌ Não duplicas código por "comodidade"
❌ Não fazes otimizações prematuras
❌ Não escreves funções com mais de 50 linhas
❌ Não escreves classes com mais de 200 linhas
❌ Não inventas requisitos que não existem

> O agente que produz código mais simples que resolve o problema completo é sempre o melhor agente. Não existe pontos por código inteligente.