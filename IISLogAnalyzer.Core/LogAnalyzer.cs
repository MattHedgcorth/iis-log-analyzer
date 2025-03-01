using IISLogAnalyzer.Domain.Models;
using IISLogAnalyzer.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace IISLogAnalyzer.Core
{
    public class LogAnalyzer
    {
        private readonly ILogAnalyzerContext _context;
        private readonly Regex _logEntryRegex;

        public LogAnalyzer(ILogAnalyzerContext context)
        {
            _context = context;
            _logEntryRegex = new Regex(@"^(\d{4}-\d{2}-\d{2})\s(\d{2}:\d{2}:\d{2})\s(\S+)\s(\S+)\s(\S+)\s(\S+)\s(\d+)\s(\S+)\s(\S+)\s(\S+)\s(\S+)\s(\d+)\s(\d+)\s(\d+)\s(\d+)\s(\d+)\s(\d+)");
        }

        public async Task ProcessLogFilesAsync(List<string> folders, List<string> extensions, bool forceReload)
        {
            var processedFiles = forceReload ? 
                new List<string>() :
                await _context.LogEntries.Select(l => l.FileName).Distinct().ToListAsync();

            foreach (var folder in folders)
            {
                var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                    .Where(f => extensions.Contains(Path.GetExtension(f)))
                    .Where(f => !processedFiles.Contains(f));

                foreach (var file in files)
                {
                    await ProcessLogFileAsync(file);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task ProcessLogFileAsync(string filePath)
        {
            System.Console.WriteLine($"Processing {filePath}...");
            var entries = new List<LogEntry>();

            foreach (var line in File.ReadLines(filePath))
            {
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;

                var match = _logEntryRegex.Match(line);
                if (!match.Success) continue;

                try
                {
                    var entry = new LogEntry
                    {
                        FileName = filePath,
                        Date = DateTime.Parse(match.Groups[1].Value),
                        Time = match.Groups[2].Value,
                        ServerIp = match.Groups[3].Value,
                        Method = match.Groups[4].Value,
                        UriStem = match.Groups[5].Value,
                        UriQuery = match.Groups[6].Value,
                        Port = int.Parse(match.Groups[7].Value),
                        Username = match.Groups[8].Value,
                        ClientIp = match.Groups[9].Value,
                        UserAgent = match.Groups[10].Value,
                        Referrer = match.Groups[11].Value,
                        StatusCode = int.Parse(match.Groups[12].Value),
                        SubStatusCode = int.Parse(match.Groups[13].Value),
                        Win32Status = int.Parse(match.Groups[14].Value),
                        TimeTaken = int.Parse(match.Groups[15].Value),
                        BytesSent = long.Parse(match.Groups[16].Value),
                        BytesReceived = long.Parse(match.Groups[17].Value)
                    };

                    entries.Add(entry);

                    if (entries.Count >= 1000)
                    {
                        await _context.LogEntries.AddRangeAsync(entries);
                        await _context.SaveChangesAsync();
                        entries.Clear();
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error processing line in {filePath}: {ex.Message}");
                }
            }

            if (entries.Any())
            {
                await _context.LogEntries.AddRangeAsync(entries);
                await _context.SaveChangesAsync();
            }
        }

        public async Task CalculateStatisticsAsync(List<string>? domainFilters = null)
        {
            System.Console.WriteLine("Calculating statistics...");

            var query = _context.LogEntries.AsQueryable();
            if (domainFilters?.Any() == true)
            {
                query = query.Where(e => domainFilters.Contains(e.ServerIp) || domainFilters.Contains(e.ClientIp));
            }

            // Clear existing statistics
            _context.ActivityStats.RemoveRange(_context.ActivityStats);
            _context.AccessStats.RemoveRange(_context.AccessStats);
            _context.VisitorStats.RemoveRange(_context.VisitorStats);
            _context.ReferrerStats.RemoveRange(_context.ReferrerStats);
            _context.BrowserStats.RemoveRange(_context.BrowserStats);
            _context.ErrorStats.RemoveRange(_context.ErrorStats);
            await _context.SaveChangesAsync();

            // Calculate new statistics
            await CalculateActivityStatisticsAsync(query);
            await CalculateAccessStatisticsAsync(query);
            await CalculateVisitorStatisticsAsync(query);
            await CalculateReferrerStatisticsAsync(query);
            await CalculateBrowserStatisticsAsync(query);
            await CalculateErrorStatisticsAsync(query);
        }

        private async Task CalculateActivityStatisticsAsync(IQueryable<LogEntry> query)
        {
            var stats = await query
                .GroupBy(e => new { e.Date, Hour = e.Time.Substring(0, 2) })
                .Select(g => new ActivityStatistics
                {
                    Date = g.Key.Date,
                    Hour = int.Parse(g.Key.Hour),
                    DayOfWeek = g.Key.Date.DayOfWeek.ToString(),
                    Week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(g.Key.Date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday),
                    Month = g.Key.Date.Month,
                    Hits = g.Count(),
                    PageViews = g.Count(e => !e.UriStem.Contains(".")),
                    Visitors = g.Select(e => e.ClientIp).Distinct().Count(),
                    Bandwidth = g.Sum(e => e.BytesSent + e.BytesReceived)
                })
                .ToListAsync();

            await _context.ActivityStats.AddRangeAsync(stats);
            await _context.SaveChangesAsync();
        }

        private async Task CalculateAccessStatisticsAsync(IQueryable<LogEntry> query)
        {
            var stats = await query
                .GroupBy(e => new { e.UriStem })
                .Select(g => new AccessStatistics
                {
                    Page = g.Key.UriStem,
                    Directory = Path.GetDirectoryName(g.Key.UriStem) ?? string.Empty,
                    FileType = Path.GetExtension(g.Key.UriStem),
                    Hits = g.Count(),
                    Bandwidth = g.Sum(e => e.BytesSent + e.BytesReceived),
                    LastAccess = g.Max(e => e.Date)
                })
                .ToListAsync();

            await _context.AccessStats.AddRangeAsync(stats);
            await _context.SaveChangesAsync();
        }

        private async Task CalculateVisitorStatisticsAsync(IQueryable<LogEntry> query)
        {
            var stats = await query
                .GroupBy(e => e.ClientIp)
                .Select(g => new VisitorStatistics
                {
                    Host = g.Key,
                    TopLevelDomain = g.Key.Contains('.') ? g.Key.Substring(g.Key.LastIndexOf('.')) : "",
                    Hits = g.Count(),
                    Bandwidth = g.Sum(e => e.BytesSent + e.BytesReceived),
                    LastVisit = g.Max(e => e.Date)
                })
                .ToListAsync();

            await _context.VisitorStats.AddRangeAsync(stats);
            await _context.SaveChangesAsync();
        }

        private async Task CalculateReferrerStatisticsAsync(IQueryable<LogEntry> query)
        {
            var stats = await query
                .Where(e => !string.IsNullOrEmpty(e.Referrer))
                .GroupBy(e => e.Referrer)
                .Select(g => new ReferrerStatistics
                {
                    Url = g.Key,
                    Site = new Uri(g.Key).Host,
                    Hits = g.Count(),
                    LastReferral = g.Max(e => e.Date)
                })
                .ToListAsync();

            await _context.ReferrerStats.AddRangeAsync(stats);
            await _context.SaveChangesAsync();
        }

        private async Task CalculateBrowserStatisticsAsync(IQueryable<LogEntry> query)
        {
            var stats = await query
                .GroupBy(e => e.UserAgent)
                .Select(g => new BrowserStatistics
                {
                    BrowserType = g.Key,
                    IsSpider = g.Key.ToLower().Contains("bot") || g.Key.ToLower().Contains("spider"),
                    Hits = g.Count(),
                    LastAccess = g.Max(e => e.Date)
                })
                .ToListAsync();

            await _context.BrowserStats.AddRangeAsync(stats);
            await _context.SaveChangesAsync();
        }

        private async Task CalculateErrorStatisticsAsync(IQueryable<LogEntry> query)
        {
            var stats = await query
                .Where(e => e.StatusCode >= 400)
                .GroupBy(e => new { e.StatusCode, e.SubStatusCode, e.Win32Status, e.UriStem })
                .Select(g => new ErrorStatistics
                {
                    StatusCode = g.Key.StatusCode,
                    SubStatusCode = g.Key.SubStatusCode,
                    Win32Status = g.Key.Win32Status,
                    Page = g.Key.UriStem,
                    Count = g.Count(),
                    LastOccurrence = g.Max(e => e.Date)
                })
                .ToListAsync();

            await _context.ErrorStats.AddRangeAsync(stats);
            await _context.SaveChangesAsync();
        }

        public async Task GenerateReportAsync()
        {
            System.Console.WriteLine("Generating report...");
            var reportGenerator = new ReportGenerator(_context);
            await reportGenerator.GenerateReportAsync();
        }
    }
}