﻿using CommandLine;
using IISLogAnalyzer.Core;
using IISLogAnalyzer.Domain.Models;
using IISLogAnalyzer.Data;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IISLogAnalyzer.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsedAsync(RunAnalyzerAsync)
                .ConfigureAwait(false);
        }

        private static async Task RunAnalyzerAsync(CommandLineOptions opts)
        {
            try
            {
                if (opts.Help)
                {
                    DisplayHelp();
                    return;
                }

                using var context = new LogAnalyzerContext();

                if (opts.RebuildDatabase)
                {
                    System.Console.WriteLine("Rebuilding database...");
                    await context.Database.EnsureDeletedAsync();
                }

                await context.Database.EnsureCreatedAsync();

                var logFolders = opts.LogFolders.Split(',')
                    .Select(f => f.Trim())
                    .Where(f => Directory.Exists(f))
                    .ToList();

                if (!logFolders.Any())
                {
                    System.Console.WriteLine("No valid log folders specified.");
                    return;
                }

                var extensions = opts.FileExtensions.Split(',')
                    .Select(e => e.Trim())
                    .Select(e => e.StartsWith(".") ? e : "." + e)
                    .ToList();

                var logAnalyzer = new LogAnalyzer(context);
                await logAnalyzer.ProcessLogFilesAsync(logFolders, extensions, opts.ForceReload);

                var domainFilters = !string.IsNullOrWhiteSpace(opts.DomainFilter)
                    ? opts.DomainFilter.Split(',').Select(d => d.Trim()).ToList()
                    : null;

                await logAnalyzer.CalculateStatisticsAsync(domainFilters);
                await logAnalyzer.GenerateReportAsync();

                System.Console.WriteLine("Analysis complete. Opening report...");
                var reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "report.html");
                await OpenReportAsync(reportPath);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                System.Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }

        private static void DisplayHelp()
        {
            System.Console.WriteLine(@"IIS Log Analyzer - Command Line Usage

Required Arguments:
  Log File Folders     Comma-delimited list of directories to scan for log files (enclosed in quotes)

Optional Arguments:
  -e, --extensions    Comma-delimited list of file extensions to filter log files (default: .log)
  -f, --filter       Comma-delimited list of domains/IPs to filter report data
  --force            Force reload of all files regardless of previous parsing
  --rebuild          Delete and recreate database before parsing
  -h, --help         Display this help message

Examples:
  IISLogAnalyzer.Console ""C:\inetpub\logs\LogFiles""
  IISLogAnalyzer.Console ""C:\Logs\Site1,C:\Logs\Site2"" -e "".log,.txt"" -f ""example.com,192.168.1.1""
");
        }

        private static async Task OpenReportAsync(string reportPath)
        {
            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo(reportPath)
                {
                    UseShellExecute = true
                };
                process.Start();
                await Task.Delay(100); // Brief delay to allow process to start
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to open report: {ex.Message}");
                System.Console.WriteLine($"Report saved to: {reportPath}");
            }
        }
    }
}
