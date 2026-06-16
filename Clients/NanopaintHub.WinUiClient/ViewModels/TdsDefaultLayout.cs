namespace NanopaintHub_WinUIClient.ViewModels;

public static class TdsDefaultLayout
{
    public const string HandlebarsHtml = @"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"" />
  <title>Technical Data Sheet - {{Name}}</title>
  <style>
    :root {
      color-scheme: light;
      --primary: #0f2a4a;
      --secondary: #19528d;
      --accent: #3182ce;
      --text-main: #223145;
      --text-muted: #556376;
      --line: #d8e1ec;
      --bg-alt: #f6f9fc;
    }

    * { box-sizing: border-box; }
    
    img[src=""""], img[src=""{{{Assets.Logo}}}""], img:not([src]) { display: none; }
    p:empty, div:empty, span:empty { display: none; }

    body {
      margin: 0;
      padding: 0;
      background: #e2e2e2;
      color: var(--text-main);
      font-family: ""Segoe UI"", ""Helvetica Neue"", Arial, sans-serif;
      font-size: 10pt;
      line-height: 1.45;
    }

    .page {
      width: 210mm;
      min-height: 297mm;
      margin: 0 auto;
      background: #fff;
      box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
      position: relative;
      display: flex;
      flex-direction: column;
    }

    .header-banner {
      background: linear-gradient(180deg, #14365d 0%, #0f2a4a 100%);
      color: #fff;
      padding: 14mm 14mm 10mm;
    }

    .header-top-row {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 12px;
      border-bottom: 1px solid rgba(255, 255, 255, 0.28);
      padding-bottom: 8px;
      margin-bottom: 10px;
    }

    .logo-wrap {
      min-height: 30px;
      display: flex;
      align-items: center;
      background: transparent;
    }

    .logo-wrap img.logo-image {
      max-height: 34px;
      width: auto;
      object-fit: contain;
      display: block;
      background: transparent;
    }

    .logo-fallback {
      font-size: 16pt;
      font-weight: 700;
      letter-spacing: 0.04em;
      color: #fff;
      text-transform: lowercase;
    }

    .doc-meta {
      text-align: right;
      font-size: 8.8pt;
      color: #d5e4f5;
      line-height: 1.35;
    }

    .doc-meta strong {
      display: block;
      color: #9ac7f4;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      font-size: 9.2pt;
      margin-bottom: 2px;
    }

    .product-title {
      margin: 0;
      font-size: 18pt;
      font-weight: 700;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      line-height: 1.2;
    }

    .product-subtitle {
      margin: 4px 0 10px;
      color: #b7d7f6;
      font-size: 11pt;
      font-weight: 500;
    }

    .product-description {
      margin: 0;
      max-width: 78%;
      color: #e6f0fb;
      text-align: justify;
      font-size: 10pt;
      line-height: 1.5;
    }

    .main-content {
      padding: 10mm 14mm 13mm;
      flex: 1;
    }

    .footer-area {
      padding: 8px 14mm 10px;
      border-top: 1px solid var(--line);
      background: #fff;
    }

    .footer-contact {
      display: flex;
      gap: 20px;
      font-size: 8.5pt;
      font-weight: 600;
      color: var(--primary);
      margin-bottom: 4px;
    }

    .meta-bar {
      display: flex;
      justify-content: space-between;
      flex-wrap: wrap;
      gap: 8px;
      margin: 0 0 10px;
      padding-bottom: 8px;
      border-bottom: 1px solid var(--line);
      color: var(--text-muted);
      font-size: 9pt;
    }

    .batch-note {
      margin: 0 0 12px;
      padding: 8px 10px;
      border-left: 3px solid var(--accent);
      background: #ebf6ff;
      color: #1f4f7d;
      font-size: 9pt;
    }

    .two-column {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
      align-items: start;
    }

    .section {
      page-break-inside: avoid;
      break-inside: avoid;
      margin-bottom: 14px;
    }

    .section-title {
      margin: 0 0 8px;
      font-size: 10pt;
      letter-spacing: 0.07em;
      text-transform: uppercase;
      color: var(--secondary);
      border-bottom: 2px solid var(--line);
      padding-bottom: 4px;
      font-weight: 700;
    }

    .features-grid {
      display: grid;
      gap: 6px;
      font-size: 9.6pt;
    }

    .feature-item {
      display: flex;
      align-items: flex-start;
      gap: 7px;
      line-height: 1.35;
    }

    .feature-check {
      flex-shrink: 0;
      width: 15px;
      height: 15px;
      margin-top: 1px;
      border-radius: 50%;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      background: var(--primary);
      color: #fff;
      font-size: 9px;
      font-weight: 700;
    }

    table.data-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 9.1pt;
    }

    table.data-table th,
    table.data-table td {
      border: 1px solid var(--line);
      padding: 5px 7px;
      vertical-align: top;
    }

    table.data-table th {
      width: 44%;
      text-align: left;
      color: var(--primary);
      background: #fff;
      font-weight: 600;
    }

    table.data-table td {
      text-align: left;
      color: var(--text-main);
    }

    table.data-table tr:nth-child(even) th,
    table.data-table tr:nth-child(even) td {
      background: var(--bg-alt);
    }

    .full-width {
      grid-column: 1 / span 2;
    }

    .chart-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
      gap: 10px;
    }

    .chart-block {
      border: 1px solid var(--line);
      background: #fff;
      border-radius: 3px;
      padding: 8px;
      max-width: 300px;
      margin: 0 auto;
      width: 100%;
      page-break-inside: avoid;
      break-inside: avoid;
    }

    .chart-block h4 {
      margin: 0 0 6px;
      color: var(--secondary);
      font-size: 9pt;
      font-weight: 600;
    }

    .chart-block img,
    .chart-block svg,
    .chart-block figure {
      max-width: 100%;
      height: auto;
      display: block;
    }

    .legal-intro {
      margin: 12px 0 8px;
      color: var(--text-muted);
      text-align: justify;
      font-size: 8.2pt;
      line-height: 1.4;
    }

    .footer-legal {
      margin: 0;
      color: var(--text-muted);
      font-size: 8pt;
      line-height: 1.35;
    }

    @media print {
      body { background: #fff; }
      .page { box-shadow: none; margin: 0; width: 100%; min-height: auto; }
      @page { size: A4; margin: 0; }
      .header-banner { padding: 12mm 12mm 8mm; }
      .main-content { padding: 9mm 12mm 11mm; }
      .footer-area { padding: 6px 12mm 8px; }
    }
  </style>
</head>
<body>
  <div class=""page"">
    <header class=""header-banner"">
      <div class=""header-top-row"">
        <div class=""logo-wrap"">
          <img class=""logo-image"" src=""{{{Assets.Logo}}}"" alt=""Nanopaint Logo"" />
        </div>
        <div class=""doc-meta"">
          <strong>Functional Inks</strong>
          Technical Data Sheet<br />
          Revised: {{RevisionDate}} · Version {{Version}}
        </div>
      </div>
      <h1 class=""product-title"">Piezoresistive Ink</h1>
      <p class=""product-subtitle"">{{Name}} · {{Code}}</p>
      <p class=""product-description"">{{Description}}</p>
    </header>

    <main class=""main-content"">
      <p class=""batch-note"" data-batch=""{{BatchNumber}}"">Batch data applied: <strong>{{BatchNumber}}</strong> (merged with product specifications where applicable).</p>

      <div class=""meta-bar"">
        <span>NanoPaint, Lda. · Universidade do Minho · Braga, Portugal</span>
        <span>www.nanopaint-tech.com · info@nanopaint-tech.com</span>
      </div>

      <div class=""two-column"">
        <section class=""section"">
          <h2 class=""section-title"">Ink features</h2>
          <div class=""features-grid"">
            {{#each Features}}
            <div class=""feature-item""><span class=""feature-check"">✓</span><span>{{this}}</span></div>
            {{/each}}
          </div>
        </section>

        <section class=""section"">
          <h2 class=""section-title"">Ink properties</h2>
          <table class=""data-table"">
            <tbody>
              <!-- {{#each LiquidProperties}} -->
              <tr><th>{{@key}}</th><td>{{this}}</td></tr>
              <!-- {{/each}} -->
            </tbody>
          </table>
        </section>

        <section class=""section full-width"">
          <h2 class=""section-title"">Handling guidelines</h2>
          <table class=""data-table"">
            <tbody>
              <!-- {{#each MechanicalProperties}} -->
              <tr><th>{{@key}}</th><td>{{this}}</td></tr>
              <!-- {{/each}} -->
            </tbody>
          </table>
        </section>

        <section class=""section full-width"">
          <h2 class=""section-title"">Sensor specifications</h2>
          <table class=""data-table"">
            <tbody>
              <tr><th>Sensitivity (Volts)</th><td>{{Tds.SensitivityVolts}} V</td></tr>
              <tr><th>Sensitivity (Per Unit)</th><td>{{Tds.SensitivityPerUnit}} {{Tds.SensitivityLabel}}</td></tr>
              <tr><th>Response time</th><td>{{Tds.ResponseTimeSec}} s</td></tr>
              <tr><th>Recovery time</th><td>{{Tds.RecoveryTimeSec}} s</td></tr>
              <tr><th>Repeatability</th><td>{{Tds.RepeatabilityPercent}} %</td></tr>
              <tr><th>Rest resistance</th><td>{{Tds.RestResistanceOhms}} Ω</td></tr>
              <tr><th>Operating temperature</th><td>{{Tds.OperatingTempMinC}} °C to {{Tds.OperatingTempMaxC}} °C ({{Tds.OperatingTempMinF}} °F to {{Tds.OperatingTempMaxF}} °F)</td></tr>
              <tr><th>Range</th><td>{{Tds.RangeMin}} – {{Tds.RangeMax}} {{Tds.ForceUnit}}</td></tr>
              <tr><th>Hysteresis</th><td>{{Tds.HysteresisPercent}} %</td></tr>
            </tbody>
          </table>
        </section>

        <section class=""section full-width"">
          <h2 class=""section-title"">Performance charts</h2>
          <div class=""chart-grid"">
            <div class=""chart-block"">
              <h4>Linearity</h4>
              {{{Charts.LinearitySvg}}}
            </div>
            <div class=""chart-block"">
              <h4>Resistance vs Force</h4>
              {{{Charts.ResistanceSvg}}}
            </div>
            <div class=""chart-block"">
              <h4>Hysteresis</h4>
              {{{Charts.HysteresisSvg}}}
            </div>
          </div>
        </section>
      </div>

      <p class=""legal-intro"">
        Safety Data Sheets (SDS) are available by emailing us or contacting your sales representative. Always consult
        the appropriate SDS before using any of our products. The information and products are for technically skilled users
        at their own discretion and risk and do not relate to use in combination with other substances or processes.
      </p>
    </main>

    <footer class=""footer-area"">
      <div class=""footer-contact"">
        <span>www.nanopaint-tech.com</span>
        <span>info@nanopaint-tech.com</span>
      </div>
      <p class=""footer-legal"">
        © 2026 NanoPaint Lda. All rights reserved. This Technical Data Sheet is generated from product records in NanopaintHub.
        Values shown reflect stored product data with optional batch overlay.
      </p>
    </footer>
  </div>
</body>
</html>";
}
