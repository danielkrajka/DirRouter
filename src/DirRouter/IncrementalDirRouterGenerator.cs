using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DirRouter;

/// <summary>
/// A sample source generator that creates C# classes based on the text file (in this case, Domain Driven Design ubiquitous language registry).
/// When using a simple text file as a baseline, we can create a non-incremental source generator.
/// </summary>
[Generator]
public class IncrementalDirRouterGenerator : IIncrementalGenerator
{
    private static DiagnosticDescriptor _descriptor = new(
        "SG001", // Diagnostic ID
        "Source Generator Error", // Title
        "An error occurred in the source generator: {0}", // Message format
        "Source Generation", // Category
        DiagnosticSeverity.Error, // Severity
        isEnabledByDefault: true);
    private static bool _error = false;
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidateClass(s),
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Where(static m => m != null);

        // Combine the class declaration with the Compilation
        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
            = context.CompilationProvider.Combine(classDeclarations.Collect());
        
        // Generate the output
        context.RegisterSourceOutput(compilationAndClasses, static (ctx, compilationWithClasses) =>
        {
            var (compilation, classes) = compilationWithClasses;

             foreach (var classSyntax in classes)
             {
                 var root = classSyntax.SyntaxTree.GetRoot();
                 var (constructorParameters, isTraditionalCtor) = GetConstructorParameters(classSyntax);
                 var usings = root
                     .DescendantNodes()
                     .OfType<UsingDirectiveSyntax>()
                     .Select(x => x.ToString())
                     .ToArray();
                 var namespaceName = root
                                         .DescendantNodes()
                                         .OfType<FileScopedNamespaceDeclarationSyntax>()
                                         .FirstOrDefault()?.Name.ToString() ??
                                     root.DescendantNodes()
                                         .OfType<NamespaceDeclarationSyntax>()
                                         .FirstOrDefault()?.Name.ToString() ??
                                     "BORKED";
            
                 var path = classSyntax.SyntaxTree.FilePath;
                 var dirRoute = DecodeRoute(path);

                 var classSource = GenerateControllerSource(
                     ctx, 
                     usings, 
                     namespaceName, 
                     classSyntax.AttributeLists.Select(x => x.ToFullString()).ToArray(),
                     constructorParameters, 
                     isTraditionalCtor, 
                     dirRoute, 
                     classSyntax.Members);
                 var fileName = dirRoute switch
                 {
                     "/" => "Root",
                     _ => dirRoute.Replace("/", "").Replace("[", "").Replace("]", ""),
                 };

                 if (_error) return;
            
                 ctx.AddSource($"{fileName}Controller.g.cs", SourceText.From(classSource, Encoding.UTF8));
             }
        });
    }
    
    private static string GenerateControllerSource(
        SourceProductionContext context, 
        string[] usings, 
        string namespaceName, 
        string[] attributes,
        string constructorParameters,
        bool isTraditionalCtor,
        string dirRoute, 
        IEnumerable<MemberDeclarationSyntax> members)
    {
        var controllerName = dirRoute switch
        {
            "/" => "Root",
            _ => dirRoute.Replace("/", "").Replace("[", "").Replace("]", ""),
        };

        var route = dirRoute.Replace("[", "{").Replace("]", "}");
        var segmentName =
            route
                .Split('/')
                .FirstOrDefault(x => x.StartsWith("[") && x.EndsWith("]"))?
                .Replace("[", "")
                .Replace("]", "") ?? string.Empty;
        
        var classBuilder = new StringBuilder();
        foreach (var usingDirective in usings)
        {
            classBuilder.Append(usingDirective);
        }
        classBuilder.AppendLine("");

        classBuilder.AppendLine($"namespace {namespaceName}");
        classBuilder.AppendLine("{");

        // Attributes
        foreach (var attribute in attributes)
        {
            classBuilder.Append(attribute);
        }
        classBuilder.AppendLine($"    [Route(\"{route}\")]");
        
        classBuilder.AppendLine($"    public class {controllerName}Controller{(!isTraditionalCtor ? constructorParameters : "")} : Controller");
        classBuilder.AppendLine("    {");

        foreach (var member in members)
        {
            foreach (var attribute in member.AttributeLists)
            {
                classBuilder.AppendLine(attribute.ToFullString());
            }
            if (member is MethodDeclarationSyntax { Identifier.Text: "Get" or "Post" or "Put" or "Delete" } method)
            {
                if (segmentName != string.Empty && method.ParameterList.Parameters.All(x => x.Identifier.Text != segmentName))
                {
                    var diagnostic = Diagnostic.Create(_descriptor, Location.None, $"None of the parameters '{method.ParameterList.ToFullString()}' are matching route segment '{segmentName}'");
                    context.ReportDiagnostic(diagnostic);
                    _error = true;

                    return "";
                }
            
                classBuilder.AppendLine($"        [Http{method.Identifier.Text}]");
                classBuilder.AppendLine($"        public async {method.ReturnType} {method.Identifier.Text}{method.ParameterList.ToFullString()}");

                classBuilder.AppendLine($"        {method.Body}");

                classBuilder.AppendLine("");
            }
            else if (member is ConstructorDeclarationSyntax { ParameterList: var parameterList } constructor)
            {
                classBuilder.AppendLine(member.ToString().Replace("Endpoints", $"{controllerName}Controller"));
            }
            else
            {
                classBuilder.AppendLine(member.ToString());
            }
        }

        classBuilder.AppendLine("    }");
        classBuilder.AppendLine("}");

        return classBuilder.ToString();
    }
    
    private static (string ParameterList , bool IsTraditionalCtor) GetConstructorParameters(ClassDeclarationSyntax classDeclaration)
    {
        var constructors = classDeclaration.DescendantNodes()
            .OfType<ConstructorDeclarationSyntax>();

        var traditionalConstructor = constructors.FirstOrDefault();

        // Check for a primary constructor
        var primaryConstructorParameters = classDeclaration.ParameterList?.Parameters
            .Select(p => p.ToString())
            .ToArray() ?? [];
        
        if (traditionalConstructor != null)
        {
            return (traditionalConstructor.ParameterList.ToString(), true);
        }

        if (primaryConstructorParameters.Length > 0)
        {
            var paramList = string.Join(", ", primaryConstructorParameters);
            return ($"({paramList})", false);
        }

        return ("()", true); // Default empty constructor if none found
    }

    private static string DecodeRoute(string path)
    {
        var keyword = "Routes/";
        var keywordIndex = path.IndexOf(keyword, StringComparison.Ordinal);

        var result = keywordIndex != -1 ?
            // Start the substring right after "Routes/"
            path.Substring(keywordIndex - 1 + keyword.Length) :
            // Return an empty string, or handle however you prefer if "Routes" is not found
            string.Empty;

        var cleaned = result.Replace("/Endpoints.cs", "");
        cleaned = cleaned == "" ? "/" : cleaned;
        
        return cleaned;
    }
    
    private static bool IsCandidateClass(SyntaxNode node)
    {
        if (node is ClassDeclarationSyntax classDeclarationSyntax && 
            classDeclarationSyntax.SyntaxTree.FilePath.Contains("Routes/") && 
            classDeclarationSyntax.Identifier.Text == "Endpoints")
        {
            return true;
        }
        
        return false;
    }
}