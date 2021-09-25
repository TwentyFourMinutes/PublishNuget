using System;
using CommandLine;

namespace PublishNuget
{
    public class ActionInputs
    {
        private string _tagFormat = "v[*]";

        [Option("name",
            Required = true,
            HelpText = "The name of the NuGet package.")]
        public string Name { get; set; } = null!;

        [Option("project-file-path",
            Required = true,
            HelpText = "The relative path of the project file.")]
        public string ProjectFilePath { get; set; } = null!;

        [Option("version-file-path",
            Required = false,
            HelpText = "The relative path of the version file.")]
        public string VersionFilePath { get; set; } = null!;

        [Option("version-regex",
            Required = false,
            HelpText = "The Regex pattern which is used to extract the version info. Defaults to '<Version>(.*)<\\/Version>'.")]
        public string VersionRegex { get; set; } = @"<Version>(.*)<\/Version>";

        [Option("tag-format",
                    Required = false,
                    HelpText = "Format of the git tag. [*] gets replaced with the actual version number. Defaults to v[*].")]
        public string TagFormat
        {
            get => _tagFormat;
            set
            {
                int index = -1, count = -1;

                do
                {
                    index = value.IndexOf("[*]", index + 1);

                    count++;
                } while (index != -1);

                if (count != 1)
                {
                    throw new ArgumentException("The 'tag-format' needs this string [*] exactly once.", "tag-format");
                }

                _tagFormat = value;
            }
        }

        [Option("tag-commit",
            Required = false,
            HelpText = "Determines whether or not to create a git tag. Default to true.")]
        public bool? TagCommit { get; set; } = false;

        [Option("nuget-key",
            Required = true,
            HelpText = "An API key to authenticate with the NuGet server.")]
        public string NugetKey { get; set; } = null!;

        [Option("include-symbols",
            Required = false,
            HelpText = "Determines whether or not to push symbols along with the NuGet package. Defaults to false.")]
        public bool? IncludesSymbols { get; set; } = false;

        [Option("fail-on-build-error",
            Required = false,
            HelpText = "Determines whether or not to fail on a build/pack error. Defaults to true.")]
        public bool? FailOnBuildError { get; set; } = true;
    }
}
