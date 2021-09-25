using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PublishNuget;
using static CommandLine.Parser;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((services) =>
    {
        services.AddSingleton<HttpClient>();
        services.AddLogging(options => options.AddConsole());
    })
    .Build();

var logger = Get<ILogger<Program>>(host);

logger.LogInformation("Parsing arguments...");

var parser = Default.ParseArguments<ActionInputs>(() => new(), args);
parser.WithNotParsed(
    errors =>
    {
        logger.LogError(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));

        Environment.Exit(1);

        return;
    });

logger.LogInformation("Finished the parsing of the arguments.");

await parser.WithParsedAsync(options => HandleProject(options, host));

await host.RunAsync();

static TService Get<TService>(IHost host) where TService : notnull
    => host.Services.GetRequiredService<TService>();

static async Task HandleProject(ActionInputs inputs, IHost host)
{
    var logger = Get<ILogger<Program>>(host);
    var httpClient = Get<HttpClient>(host);

    var versionRegex = new Regex(inputs.VersionRegex);

    if (!File.Exists(inputs.ProjectFilePath))
    {
        logger.LogError($"The project file '{inputs.ProjectFilePath}' does not exist.");

        Environment.Exit(1);
    }

    logger.LogInformation($"Project '{inputs.Name}' found at '{inputs.ProjectFilePath}'.");

    string versionNumber = await GetVersionNumberFromFile(logger, string.IsNullOrWhiteSpace(inputs.VersionFilePath) ? inputs.ProjectFilePath : inputs.VersionFilePath, versionRegex);

    var fullVersion = inputs.TagFormat.Replace("[*]", versionNumber);

    logger.LogInformation($"Extracted version: '{fullVersion}'");

    using var request = await httpClient.GetAsync("https://api.nuget.org/v3-flatcontainer/Venflow/index.json");

    if (request.IsSuccessStatusCode)
    {
        var document = JsonDocument.Parse(await request.Content.ReadAsStringAsync());

        var doesVersionExist = false;

        foreach (var element in document.RootElement.GetProperty("versions").EnumerateArray())
        {
            if (element.GetString() == fullVersion)
            {
                doesVersionExist = true;

                break;
            }
        }

        if (doesVersionExist)
        {
            logger.LogInformation($"The version '{fullVersion}' already exists.");
        }

        logger.LogInformation($"The version '{fullVersion}' does not exist, continuing...");
    }
    else if (request.StatusCode == HttpStatusCode.NotFound)
    {
        logger.LogInformation($"This is the first version '{fullVersion}', continuing...");
    }
    else
    {
        logger.LogError($"Invalid Status code {request.StatusCode}: {await request.Content.ReadAsStringAsync()}");
    }

    logger.LogInformation($"This is the first version '{fullVersion}', continuing...");

    logger.LogInformation($"Building package {inputs.Name}...");

    if (!await GitHubProcess.ExecuteCommandAsync(@$"dotnet build -c Release ""{inputs.ProjectFilePath}""", ExceptionCallback, OutputCallback))
    {
        if (inputs.FailOnBuildError)
        {
            Environment.Exit(1);

            return;
        }
    }

    logger.LogInformation($"Built package {inputs.Name}.");

    logger.LogInformation($"Packing package {inputs.Name}...");

    var tempFolder = Path.Combine(Path.GetTempPath(), "PublishNuget");

    if (!Directory.Exists(tempFolder))
        Directory.CreateDirectory(tempFolder);

    logger.LogInformation("Output Directory: " + tempFolder);

    var packagePackCommand = @$"dotnet pack {(inputs.IncludesSymbols ? "--include-symbols -p:SymbolPackageFormat=snupkg" : string.Empty)} --no-build -c Release ""{inputs.ProjectFilePath}"" -o ""{tempFolder}""";

    if (!await GitHubProcess.ExecuteCommandAsync(packagePackCommand, ExceptionCallback, OutputCallback) &&
        inputs.FailOnBuildError)
    {
        Environment.Exit(1);

        return;
    }

    logger.LogInformation($"Packed package {inputs.Name}.");

    if (inputs.TagCommit)
    {
        logger.LogInformation($"Creating tag '{versionNumber}'.");

        if (!await GitHubProcess.ExecuteCommandAsync("git tag " + versionNumber, ExceptionCallback, OutputCallback) ||
            !await GitHubProcess.ExecuteCommandAsync("git push origin " + versionNumber, ExceptionCallback, OutputCallback))
        {
            logger.LogError($"Tag '{versionNumber}' could not be created.");
        }
        else
        {
            logger.LogInformation($"Tag '{versionNumber}' created.");
        }
    }

    logger.LogInformation($"Pushing package {inputs.Name}...");

    var packagePushCommand = $"dotnet nuget push *.nupkg -k {inputs.NugetKey} -s https://api.nuget.org/v3/index.json --skip-duplicate{(!inputs.IncludesSymbols ? " -n" : string.Empty)}";
    var packageLogCommand = $"dotnet nuget push *.nupkg -k *** -s https://api.nuget.org/v3/index.json --skip-duplicate{(!inputs.IncludesSymbols ? " -n" : string.Empty)}";

    if (!await GitHubProcess.ExecuteCommandAsync(packagePushCommand, ExceptionCallback, OutputCallback, packageLogCommand) &&
        inputs.FailOnBuildError)
    {
        Environment.Exit(1);

        return;
    }

    logger.LogInformation($"Packed package {inputs.Name}.");

    logger.LogInformation($"Finished.");

    Environment.Exit(0);

    void ExceptionCallback(string? msg)
        => logger.LogError(msg);

    void OutputCallback(string? msg)
        => logger.LogInformation(msg);
}

static async Task<string> GetVersionNumberFromFile(ILogger logger, string fileName, Regex regex)
{
    if (!File.Exists(fileName))
    {
        logger.LogError($"The version-file-path '{fileName}' is not valid.");

        Environment.Exit(1);
    }

    var content = await File.ReadAllTextAsync(fileName);

    var match = regex.Match(content);

    if (!match.Success)
    {
        logger.LogError($"The version-file-path '{fileName}' doesn't contain any match for the Regex '{regex}'.");

        Environment.Exit(1);
    }

    return match.Groups[1].Value;
}