using CommandLine;

namespace IISLogAnalyzer.Domain.Models
{
    public class CommandLineOptions
    {
        [Option('l', Required = true, HelpText = "Comma-delimited list of directories to scan for log files")]
        public string LogFolders { get; set; }

        [Option('r', "recursive", Default = false, HelpText = "Search subdirectories recursively for log files")]
        public bool Recursive { get; set; }

        [Option('o', "output", HelpText = "Output directory or file path for the report")]
        public string OutputPath { get; set; }

        [Option('e', "extensions", Default = ".log", HelpText = "Comma-delimited list of file extensions to filter log files")]
        public string FileExtensions { get; set; }

        [Option('f', "filter", HelpText = "Comma-delimited list of domains/IPs to filter report data")]
        public string DomainFilter { get; set; }

        [Option("force", HelpText = "Force reload of all files regardless of previous parsing")]
        public bool ForceReload { get; set; }

        [Option("rebuild", HelpText = "Delete and recreate database before parsing")]
        public bool RebuildDatabase { get; set; }

        [Option('h', "help", HelpText = "Display parameter descriptions and usage examples")]
        public bool Help { get; set; }

        [Option('d', "debug", Default = false, HelpText = "Display debug information including database location")]
        public bool Debug { get; set; }
    }
}