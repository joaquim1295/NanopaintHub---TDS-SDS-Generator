using System;
using System.Collections.Generic;
using System.Text;
using HandlebarsDotNet;

namespace Test {
    class Program {
        static void Main() {
            try {
                var sb = new StringBuilder();
                sb.AppendLine("  <div class=\"page\">");
                sb.AppendLine("    <header class=\"header-banner\" style=\"font-family: 'Segoe UI', sans-serif; color: #ffffff;\">");
                sb.AppendLine("      <div class=\"header-top-row\">");
                sb.AppendLine("        <div class=\"logo-wrap\">");
                sb.AppendLine("          <img class=\"logo-image\" src=\"{{Assets.Logo}}\" alt=\"Logo\" height=\"34\" />");
                sb.AppendLine("        </div>");
                sb.AppendLine("        <div class=\"doc-meta\">");
                sb.AppendLine("          <strong>Functional Inks</strong>");
                sb.AppendLine("          Technical Data Sheet<br />");
                sb.AppendLine("          Revised: {{RevisionDate}} · Version {{Version}}");
                sb.AppendLine("        </div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("      <h1 class=\"product-title\">Piezoresistive Ink</h1>");
                sb.AppendLine("      <p class=\"product-subtitle\">{{Name}} · {{Code}}</p>");
                sb.AppendLine("      <p class=\"product-description\">{{Description}}</p>");
                sb.AppendLine("    </header>");
                sb.AppendLine("    <main class=\"main-content\">");
                sb.AppendLine("      {{#if HasBatch}}");
                sb.AppendLine("      <p class=\"batch-note\">Batch data applied: <strong>{{BatchNumber}}</strong></p>");
                sb.AppendLine("      {{/if}}");
                sb.AppendLine("      <div class=\"two-column\">");
                sb.AppendLine("        {{#if Features}}");
                sb.AppendLine("        <section class=\"section\" style=\"font-family: 'Segoe UI', sans-serif; color: #223145;\">");
                sb.AppendLine("          <h2 class=\"section-title\">Ink features</h2>");
                sb.AppendLine("          <div class=\"features-grid\">");
                sb.AppendLine("            {{#each Features}}");
                sb.AppendLine("            <div class=\"feature-item\"><span class=\"feature-check\">✓</span><span>{{this}}</span></div>");
                sb.AppendLine("            {{/each}}");
                sb.AppendLine("          </div>");
                sb.AppendLine("        </section>");
                sb.AppendLine("        {{/if}}");
                sb.AppendLine("        {{#if LiquidProperties}}");
                sb.AppendLine("        <section class=\"section\" style=\"font-family: 'Segoe UI', sans-serif; color: #223145;\">");
                sb.AppendLine("          <h2 class=\"section-title\">Ink properties</h2>");
                sb.AppendLine("          <table class=\"data-table\">");
                sb.AppendLine("            <tbody>");
                sb.AppendLine("              <!-- {{#each LiquidProperties}} -->");
                sb.AppendLine("              <tr><th>{{@key}}</th><td>{{this}}</td></tr>");
                sb.AppendLine("              <!-- {{/each}} -->");
                sb.AppendLine("            </tbody>");
                sb.AppendLine("          </table>");
                sb.AppendLine("        </section>");
                sb.AppendLine("        {{/if}}");
                sb.AppendLine("      </div>");
                sb.AppendLine("    </main>");
                sb.AppendLine("  </div>");
                
                var html = sb.ToString();
                Handlebars.Compile(html);
                Console.WriteLine("OK!");
            } catch(Exception ex) {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
    }
}
