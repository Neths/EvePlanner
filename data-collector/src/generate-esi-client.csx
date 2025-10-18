#!/usr/bin/env dotnet-script

#r "nuget: NSwag.CodeGeneration.CSharp, 14.6.1"

using NSwag;
using NSwag.CodeGeneration.CSharp;
using System.IO;

var swaggerFile = Path.GetFullPath("esi-swagger.json");
var outputFile = Path.GetFullPath("EveDataCollector.Infrastructure/ESI/Generated/EsiClient.cs");

Console.WriteLine($"Reading Swagger from: {swaggerFile}");
Console.WriteLine($"Output will be generated to: {outputFile}");

// Load the Swagger document
var document = await OpenApiDocument.FromFileAsync(swaggerFile);

// Configure the CSharp client generator
var settings = new CSharpClientGeneratorSettings
{
    ClassName = "EsiClient",
    CSharpGeneratorSettings =
    {
        Namespace = "EveDataCollector.Infrastructure.ESI",
        JsonLibrary = NJsonSchema.CodeGeneration.CSharp.CSharpJsonLibrary.SystemTextJson,
        GenerateNullableReferenceTypes = true,
        GenerateDataAnnotations = true,
        ClassStyle = NJsonSchema.CodeGeneration.CSharp.CSharpClassStyle.Poco
    },
    GenerateClientInterfaces = true,
    GenerateExceptionClasses = true,
    ExceptionClass = "EsiApiException",
    InjectHttpClient = true,
    DisposeHttpClient = false,
    GenerateSyncMethods = false,
    UseBaseUrl = true,
    GenerateBaseUrlProperty = true
};

// Generate the client
var generator = new CSharpClientGenerator(document, settings);
var code = generator.GenerateFile();

// Ensure the output directory exists
Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

// Write the generated code to file
await File.WriteAllTextAsync(outputFile, code);

Console.WriteLine($"✓ ESI Client generated successfully!");
Console.WriteLine($"  Output: {outputFile}");
Console.WriteLine($"  Size: {new FileInfo(outputFile).Length / 1024} KB");
