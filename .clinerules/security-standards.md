---
description: Padrões obrigatórios de Segurança e OWASP específicos para agentes AI.
author: Cline Prompt Engineering Workspace
version: 1.0
category: Security
tags: ["security", "owasp", "authentication", "authorization", "standards"]
priority: 120
auto_load_when: SEMPRE. Esta regra é carregada em TODOS os pedidos.
---

# Security & OWASP Standards

> ⚠️ **Esta regra é carregada AUTOMATICAMENTE EM TODOS OS PEDIDOS.**
> Segurança não é opcional.

---

## ✅ EU SEMPRE FAÇO, SEM EXCEÇÕES:

1. Todos os inputs são sempre validados e sanitizados
2. Todas as senhas são sempre armazenadas com hash bcrypt
3. Todas as sessões e tokens tem expiração
4. Todos os endpoints que alteram dados estão protegidos contra CSRF
5. Eu SEMPRE implemento autorização a nível de objeto
6. Todos os segredos são carregados de variáveis de ambiente
7. Nunca registo dados sensíveis nos logs
8. Todos os cabeçalhos de segurança estão presentes

---

## ❌ EU NUNCA FAÇO, SOB NENHUMA CIRCUNSTÂNCIA:

1. ❌ Nunca concateno strings em queries SQL
2. ❌ Nunca armazeno senhas em texto limpo
3. ❌ Nunca exponho detalhes de erro para o cliente
4. ❌ Nunca uso JWT sem expiração
5. ❌ Nunca recebo ficheiros sem validação de tipo e tamanho
6. ❌ Nunca passo segredos por query string
7. ❌ Nunca desativo proteções de segurança por padrão
8. ❌ Nunca confio em dados enviados pelo cliente

---

## ✅ Checklist OBRIGATÓRIO que eu executo SEMPRE:

- [ ] Todos os inputs estão validados
- [ ] Não existe nenhuma SQL Injection possível
- [ ] Todos os endpoints tem autorização
- [ ] Nenhum segredo está hardcodado
- [ ] Nenhum dado sensível aparece nos logs
- [ ] Todas as senhas tem hash
- [ ] Todos os cabeçalhos de segurança estão presentes

---

## 📝 Notas Adicionais:

> Esta regra tem precedência SOBRE TODAS as outras regras. Segurança vem sempre em primeiro lugar.
> Se eu tiver dúvida sobre segurança eu não avanço. Eu pergunto.