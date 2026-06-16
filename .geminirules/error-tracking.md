---
description: Estratégias de diagnóstico e fallback contínuo para manter resiliência offline.
author: Antigravity
version: 1.0
---

# Error Tracking & Diagnóstico (NanopaintHub)

## 1. Registo de Telemetria Offline (Offline Logging)
Dado que a aplicação operará em fábricas frequentemente desconectadas da Intranet, os erros não podem depender de SaaS (como Sentry ou Application Insights) de imediato.
- **Auditoria Contínua**: Exceções não tratadas e eventos críticos de negócio devem ser registados localmente em `%LocalAppData%\NanopaintHub\Logs\lims_offline_log.txt`.
- **Background Sync Exceptions**: Falhas no Background Service (Network Monitor) devem aplicar lógica de *Exponential Backoff com Jitter*, registando apenas a causa raiz no disco (ex: Timeout, Socket Exception, ou erro HTTP 413 Payload Too Large) para não poluir os logs.

## 2. Resiliência do Pipeline de Renderização UI e PDF
Componentes críticos de motor Web (usados para o editor TinyMCE e geração final de PDF) falham frequentemente num ambiente local Windows devido a perfis corrompidos ou Anti-Vírus.
- Se o **WebView2** embebido WinUI falhar (`CoreWebView2 null`), a aplicação NUNCA pode emparedar a experiência do utilizador.
- **Fallback Obrigatório**: O sistema aplica a injeção do `IChromiumTinyMceEditorService` via PuppeteerSharp como via paralela altamente estável para renderização (Headless) e edição isolada (Headed).
- Os logs de falhas do Puppeteer/Chromium devem conter sempre informações de ficheiros em `file:///` e caminhos de acesso local que comprovem a inexistência de assets ou bloqueios de execução do Chrome isolado.
