using Esprima;
using Esprima.Ast;

namespace Lft.Ast.JavaScript;

public class JavaScriptInjectionService : IJavaScriptInjectionService
{
    public async Task InjectImportAsync(string filePath, string importStatement)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var content = await File.ReadAllTextAsync(filePath);
        var parser = new JavaScriptParser(new ParserOptions { Range = true, Tolerant = true });
        Program script;
        try
        {
            script = parser.ParseScript(content);
        }
        catch (ParserException ex)
        {
            // Fallback: if script parsing fails, try module parsing
            try
            {
                script = parser.ParseModule(content);
            }
            catch
            {
                throw new InvalidOperationException($"Failed to parse JavaScript file: {filePath}. Error: {ex.Message}", ex);
            }
        }

        if (content.Contains(importStatement.Trim()))
        {
            return;
        }

        // Find the last import statement
        var lastImport = script.Body.LastOrDefault(n => n.Type == Nodes.ImportDeclaration);

        int insertPosition = 0;
        if (lastImport != null)
        {
            // Insert after the last import
            // We need to find the end of the line of the last import
            // Esprima nodes have Location/Range if configured. 
            // By default, we might need to rely on the node's range if available, 
            // or just simple line scanning if we don't have range info enabled by default.
            // Let's re-parse with location info to be safe.

            parser = new JavaScriptParser(new ParserOptions { Tokens = true, Tolerant = true, Range = true });
            try
            {
                script = parser.ParseModule(content);
            }
            catch
            {
                script = parser.ParseScript(content);
            }

            lastImport = script.Body.LastOrDefault(n => n.Type == Nodes.ImportDeclaration);
            if (lastImport != null)
            {
                insertPosition = lastImport.Range.End;
            }
        }

        // Insert the new import
        var newContent = content.Insert(insertPosition, Environment.NewLine + importStatement);

        // If we inserted at 0 and there was no newline, add one
        if (insertPosition == 0 && newContent.Length > importStatement.Length && !newContent.StartsWith(importStatement + Environment.NewLine))
        {
            newContent = content.Insert(0, importStatement + Environment.NewLine);
        }

        await File.WriteAllTextAsync(filePath, newContent);
    }

    public async Task InjectIntoArrayAsync(string filePath, string arrayName, string snippet)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var content = await File.ReadAllTextAsync(filePath);
        var parser = new JavaScriptParser(new ParserOptions { Tokens = true, Tolerant = true, Range = true });
        Program script;
        try
        {
            script = parser.ParseModule(content);
        }
        catch
        {
            script = parser.ParseScript(content);
        }

        // Find the array
        // It could be:
        // 1. const routes = [ ... ];
        // 2. export const routes = [ ... ];
        // 3. export default [ ... ]; (if arrayName is null/empty, maybe?) -> For now assume named array.

        ArrayExpression? arrayExpression = null;

        foreach (var node in script.Body)
        {
            if (node is VariableDeclaration variableDeclaration)
            {
                foreach (var declaration in variableDeclaration.Declarations)
                {
                    if (declaration.Id is Identifier identifier && identifier.Name == arrayName)
                    {
                        if (declaration.Init is ArrayExpression arr)
                        {
                            arrayExpression = arr;
                            break;
                        }
                    }
                }
            }
            else if (node is ExportNamedDeclaration exportNamed)
            {
                if (exportNamed.Declaration is VariableDeclaration varDecl)
                {
                    foreach (var declaration in varDecl.Declarations)
                    {
                        if (declaration.Id is Identifier identifier && identifier.Name == arrayName)
                        {
                            if (declaration.Init is ArrayExpression arr)
                            {
                                arrayExpression = arr;
                                break;
                            }
                        }
                    }
                }
            }

            if (arrayExpression != null) break;
        }

        if (arrayExpression == null)
        {
            throw new InvalidOperationException($"Array '{arrayName}' not found in {filePath}");
        }

        // Check if snippet already exists within the array text (idempotency)
        var arrayText = content.Substring(arrayExpression.Range.Start, arrayExpression.Range.End - arrayExpression.Range.Start);
        if (arrayText.Contains(snippet.Trim()))
        {
            return;
        }

        // Find the insertion point (end of the array, before ']')
        
        int insertPos;
        string prefix = "";

        if (arrayExpression.Elements.Count > 0)
        {
            var lastElement = arrayExpression.Elements.Last();
            if (lastElement != null)
            {
                insertPos = lastElement.Range.End;
                prefix = "," + Environment.NewLine + "    "; // Add comma and newline
            }
            else
            {
                // Array has holes? rare for config arrays.
                insertPos = arrayExpression.Range.End - 1;
            }
        }
        else
        {
            // Empty array []
            insertPos = arrayExpression.Range.End - 1;
            prefix = Environment.NewLine + "    ";
        }

        var newContent = content.Insert(insertPos, prefix + snippet);
        await File.WriteAllTextAsync(filePath, newContent);
    }
}
