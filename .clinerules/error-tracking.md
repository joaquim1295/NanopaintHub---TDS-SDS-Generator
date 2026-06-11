---
description: Error Tracking System for structured registration of all errors and corrections
author: Cline Community
version: 1.0
category: "Core System"
tags: ["errors", "tracking", "logs", "debug"]
globs: ["docs/errors.md"]
priority: 120
---

# Error Tracking Rule

> ⚠️ **Hierarquia**: Esta regra está subordinada à metodologia Baby Steps. É executada automaticamente APÓS cada passo validado.
>
> Durante o processo de desenvolvimento completo o agente deve manter um registo de erros e correções estruturais.

File location:

docs/errors.md

This file must be created if it does not exist.

Rules:

1. Every runtime error must be recorded.
2. Every architecture violation must be recorded.
3. Every refactor done to fix incorrect implementation must be recorded.
4. Each entry must include:

date
file affected
problem
cause
fix applied

Example format:

## Error Log

### 2026-02-24

File:
features/chat/api/route.ts

Problem:
Server component importing client hook.

Cause:
Architecture rule violation.

Fix:
Split component into server and client components.