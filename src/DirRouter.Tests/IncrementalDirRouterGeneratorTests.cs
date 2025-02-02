using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DirRouter.Tests;

public class IncrementalDirRouterGeneratorTests
{
    private const string EndpointsClassText = @"
namespace TestNamespace;

[Authorize(Roles = ""Admin"")]
public class Endpoints
{
    public async Task<IResult> Get() => Task.FromResult(Results.Ok(""Ok""));
    public async Task<IResult> Post([FromBody] object payload) => Task.FromResult(Results.Ok(""Created""));
}";

    private const string ExpectedGeneratedClassText = @"
namespace TestNamespace
{

[Authorize(Roles = ""Admin"")]
    [Route(""/"")]
    public class RootController : Controller
    {
        [HttpGet]
        public async Task<IResult> Get() 
        

        [HttpPost]
        public async Task<IResult> Post([FromBody] object payload) 
        

    }
}
";

    [Fact]
    public void GenerateReportMethod()
    {
        // Create an instance of the source generator.
        IIncrementalGenerator generator = new IncrementalDirRouterGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        var driver = CSharpGeneratorDriver.Create(generator);

        // We need to create a compilation with the required source code.
        var compilation = CSharpCompilation.Create(nameof(IncrementalDirRouterGeneratorTests),
            [CSharpSyntaxTree.ParseText(EndpointsClassText, path: "/projects/Routes/Endpoints.cs")]
            // ,
            // [
            //     // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
            //     MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            // ]
            );

        // Run generators and retrieve all results.
        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // All generated files can be found in 'RunResults.GeneratedTrees'.
        var generatedFileSyntax = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith("RootController.g.cs"));
        
        var text = generatedFileSyntax.GetText().ToString();

        // Complex generators should be tested using text comparison.
        Assert.Equal(ExpectedGeneratedClassText, generatedFileSyntax.GetText().ToString(),
            ignoreLineEndingDifferences: true);
    }
}