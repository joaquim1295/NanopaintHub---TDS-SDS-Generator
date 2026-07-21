using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        string dbPath = Environment.ExpandEnvironmentVariables(@""%LocalAppData%\NanopaintHub\nanopainthub.db"");
        using (var connection = new SqliteConnection($""Data Source={dbPath}""))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = ""SELECT TemplateName, DocumentType, BuilderJson FROM DocumentTemplates;"";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($""--- {reader.GetString(0)} ({reader.GetString(1)}) ---"");
                    if (!reader.IsDBNull(2))
                    {
                        var json = reader.GetString(2);
                        if (json.Length > 200) {
                            Console.WriteLine(json.Substring(0, 200) + ""..."");
                            
                            // Check Footer manually
                            int footerIdx = json.IndexOf(""\""Footer\"":"");
                            if (footerIdx != -1) {
                                int endIdx = json.IndexOf(""}"", footerIdx);
                                if (endIdx != -1) {
                                    Console.WriteLine(json.Substring(footerIdx, endIdx - footerIdx + 1));
                                }
                            }
                        } else {
                            Console.WriteLine(json);
                        }
                    }
                    else Console.WriteLine(""NULL"");
                }
            }
        }
    }
}
