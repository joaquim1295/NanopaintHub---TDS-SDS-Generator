using System;
using HandlebarsDotNet;
using System.Collections.Generic;

var html = @"
    <tbody>
      <!-- {{#each }} -->
      <tr><th>{{@key}}</th><td>{{this}}</td></tr>
      <!-- {{/each}} -->
    </tbody>";

try {
    var template = Handlebars.Compile(html);
    var result = template(new { });
    Console.WriteLine("SUCCESS:");
    Console.WriteLine(result);
} catch (Exception ex) {
    Console.WriteLine("ERROR:");
    Console.WriteLine(ex.Message);
}
