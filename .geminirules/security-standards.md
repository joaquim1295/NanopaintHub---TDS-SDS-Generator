---
description: Padrões de segurança local, Idempotência e Autenticação Desconectada JWT.
author: Antigravity
version: 1.0
---

# Security Standards (NanopaintHub)

## 1. Modo Utilitário Livre vs Autenticação na API
A segurança atua exclusivamente nas extremidades, nunca no uso primário da ferramenta local:
- **Offline Sem Fricção**: Os operadores do laboratório NUNCA precisam de Login para criar Lotes de Teste ou gerar TDS/SDS. Tudo funciona de imediato com dados residentes no SQLite e gerados localmente (Soberania Pragmática).
- **Proteção da Base de Dados Local**: Todo o SQLite de laboratório `lims_local.db` deve ser obrigatoriamente encriptado e acedido via `SQLCipher`.

## 2. Validação JWT Matemática Offline (Modo Viagem)
A aplicação suporta Engenheiros fora da infraestrutura de rede da empresa:
- Tokens JWT são armazenados no `SQLCipher` local.
- Quando o computador está fora da intranet corporativa, não é possível validar o token na API central. A aplicação Desktop executa uma **Validação Matemática Offline** através do relógio interno e validação algorítmica RSA.
- Se o Token ainda não expirou, são libertados os privilégios de Administrador (como Edição Global de Templates).

## 3. Prevenção a Colisões de Sincronismo (Idempotência)
Ao enviar dados do laboratório para o servidor Postgres central:
- O sistema central impõe **Idempotência Lógica e Chaves Compostas** (ex: `numero_lote` + `produto_id`).
- Caso exista concorrência desfasada (outro laboratório enviou primeiro offline), o servidor rejeita com `HTTP 409 Conflict`.
- A App WinUI captura o `HTTP 409`, muda o estado do registo local para `ValidationError` e avisa visualmente o utilizador para reconciliar os dados antes do próximo envio. NUNCA sobrescreve os dados à força.
