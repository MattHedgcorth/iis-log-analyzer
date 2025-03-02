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
        private readonly Dictionary<string, int> _fieldIndexes;
        private readonly Regex _logEntryRegex;

        public LogAnalyzer(ILogAnalyzerContext context)
        {
            _context = context;
            _fieldIndexes = new Dictionary<string, int>();
            _logEntryRegex = new Regex(@"^(\S+)", RegexOptions.Compiled);
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
            bool fieldsFound = false;

            foreach (var line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Skip comments except for #Fields directive
                if (line.StartsWith("#"))
                {
                    if (line.StartsWith("#Fields: "))
                    {
                        ParseFieldsDirective(line);
                        fieldsFound = true;
                    }
                    continue;
                }

                if (!fieldsFound)
                {
                    System.Console.WriteLine($"Warning: No #Fields directive found in {filePath}. Skipping file.");
                    break;
                }

                try
                {
                    var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length != _fieldIndexes.Count) continue;

                    var entry = new LogEntry
                    {
                        FileName = filePath,
                        Date = DateTime.Parse(GetFieldValue(fields, "date")),
                        Time = GetFieldValue(fields, "time"),
                        ServerIp = GetFieldValue(fields, "s-ip"),
                        Method = GetFieldValue(fields, "cs-method"),
                        UriStem = GetFieldValue(fields, "cs-uri-stem"),
                        UriQuery = GetFieldValue(fields, "cs-uri-query"),
                        Port = int.Parse(GetFieldValue(fields, "s-port")),
                        Username = GetFieldValue(fields, "cs-username"),
                        ClientIp = GetFieldValue(fields, "c-ip"),
                        UserAgent = GetFieldValue(fields, "cs(User-Agent)"),
                        Referrer = GetFieldValue(fields, "cs(Referer)"),
                        StatusCode = int.Parse(GetFieldValue(fields, "sc-status")),
                        SubStatusCode = int.Parse(GetFieldValue(fields, "sc-substatus")),
                        Win32Status = int.Parse(GetFieldValue(fields, "sc-win32-status")),
                        TimeTaken = int.Parse(GetFieldValue(fields, "time-taken")),
                        BytesSent = long.Parse(GetFieldValue(fields, "sc-bytes")),
                        BytesReceived = long.Parse(GetFieldValue(fields, "cs-bytes"))
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

        private void ParseFieldsDirective(string line)
        {
            _fieldIndexes.Clear();
            var fields = line.Substring("#Fields: ".Length).Split(' ');
            for (int i = 0; i < fields.Length; i++)
            {
                _fieldIndexes[fields[i].Trim()] = i;
            }
        }

        private string GetFieldValue(string[] fields, string fieldName)
        {
            if (_fieldIndexes.TryGetValue(fieldName, out int index) && index < fields.Length)
            {
                return fields[index] == "-" ? string.Empty : fields[index];
            }
            return string.Empty;
        }

        public async Task CalculateStatisticsAsync(List<string>? domainFilters = null)
        {
            System.Console.WriteLine("Calculating statistics...");

            var query = _context.LogEntries.AsQueryable();
            if (domainFilters?.Any() == true)
            {
                query = query.Where(e => domainFilters.Contains(e.ServerIp) || domainFilters.Contains(e.ClientIp));
            }

            // Clear existing statistics in batches
            await ClearStatisticsAsync<ActivityStatistics>(_context.ActivityStats);
            await ClearStatisticsAsync<AccessStatistics>(_context.AccessStats);
            await ClearStatisticsAsync<VisitorStatistics>(_context.VisitorStats);
            await ClearStatisticsAsync<ReferrerStatistics>(_context.ReferrerStats);
            await ClearStatisticsAsync<BrowserStatistics>(_context.BrowserStats);
            await ClearStatisticsAsync<ErrorStatistics>(_context.ErrorStats);

            // Calculate new statistics
            System.Console.WriteLine("Calculating new statistics...");
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
            const int batchSize = 1000;
            var processedCount = 0;

            var statsQuery = query
                .GroupBy(e => new { e.UriStem })
                .Select(g => new
                {
                    UriStem = g.Key.UriStem,
                    Hits = g.Count(),
                    Bandwidth = g.Sum(e => e.BytesSent + e.BytesReceived),
                    LastAccess = g.Max(e => e.Date)
                });

            var totalCount = await statsQuery.CountAsync();

            while (processedCount < totalCount)
            {
                var batch = await statsQuery
                    .Skip(processedCount)
                    .Take(batchSize)
                    .ToListAsync();

                var stats = batch.Select(g => new AccessStatistics
                {
                    Page = g.UriStem ?? string.Empty,
                    Directory = GetSafeDirectoryName(g.UriStem),
                    FileType = GetSafeExtension(g.UriStem),
                    VirtualDomain = string.Empty, // Initialize with empty string
                    Hits = g.Hits,
                    Bandwidth = g.Bandwidth,
                    LastAccess = g.LastAccess
                }).ToList();

                await _context.AccessStats.AddRangeAsync(stats);
                await _context.SaveChangesAsync();

                processedCount += batch.Count;
                System.Console.WriteLine($"Processed {processedCount}/{totalCount} access statistics...");
            }
        }

        private string GetSafeDirectoryName(string uriStem)
        {
            try
            {
                return Path.GetDirectoryName(uriStem ?? string.Empty) ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private string GetSafeExtension(string uriStem)
        {
            try
            {
                return Path.GetExtension(uriStem ?? string.Empty);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private async Task ClearStatisticsAsync<T>(DbSet<T> dbSet) where T : class
        {
            const int batchSize = 1000;
            var itemsRemaining = true;

            while (itemsRemaining)
            {
                var batch = await dbSet.Take(batchSize).ToListAsync();
                if (batch.Count == 0)
                {
                    itemsRemaining = false;
                    continue;
                }

                dbSet.RemoveRange(batch);
                await _context.SaveChangesAsync();
                System.Console.WriteLine($"Cleared {batch.Count} {typeof(T).Name} records...");
            }
        }

        private async Task CalculateVisitorStatisticsAsync(IQueryable<LogEntry> query)
        {
            const int batchSize = 1000;
            var processedCount = 0;

            var statsQuery = query
                .GroupBy(e => e.ClientIp)
                .Select(g => new
                {
                    ClientIp = g.Key,
                    Hits = g.Count(),
                    Bandwidth = g.Sum(e => e.BytesSent + e.BytesReceived),
                    LastVisit = g.Max(e => e.Date)
                });

            var totalCount = await statsQuery.CountAsync();

            while (processedCount < totalCount)
            {
                var batch = await statsQuery
                    .Skip(processedCount)
                    .Take(batchSize)
                    .ToListAsync();

                var stats = batch.Select(g => new VisitorStatistics
                {
                    Host = g.ClientIp ?? string.Empty,
                    TopLevelDomain = GetTopLevelDomain(g.ClientIp),
                    Country = string.Empty,
                    AuthenticatedUser = string.Empty,
                    Hits = g.Hits,
                    Bandwidth = g.Bandwidth,
                    LastVisit = g.LastVisit
                }).ToList();

                await _context.VisitorStats.AddRangeAsync(stats);
                await _context.SaveChangesAsync();

                processedCount += batch.Count;
                System.Console.WriteLine($"Processed {processedCount}/{totalCount} visitor statistics...");
            }
        }

        private string GetTopLevelDomain(string ip)
        {
            if (string.IsNullOrEmpty(ip) || !ip.Contains('.'))
                return string.Empty;
            
            try
            {
                return ip.Substring(ip.LastIndexOf('.'));
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task CalculateReferrerStatisticsAsync(IQueryable<LogEntry> query)
        {
            const int batchSize = 1000;
            var processedCount = 0;

            var statsQuery = query
                .Where(e => !string.IsNullOrEmpty(e.Referrer))
                .GroupBy(e => e.Referrer)
                .Select(g => new
                {
                    Referrer = g.Key,
                    Hits = g.Count(),
                    LastReferral = g.Max(e => e.Date)
                });

            var totalCount = await statsQuery.CountAsync();

            while (processedCount < totalCount)
            {
                var batch = await statsQuery
                    .Skip(processedCount)
                    .Take(batchSize)
                    .ToListAsync();

                var stats = batch.Select(g => new ReferrerStatistics
                {
                    Url = g.Referrer ?? string.Empty,
                    Site = GetSafeHost(g.Referrer),
                    SearchEngine = string.Empty,
                    SearchPhrase = string.Empty,
                    Hits = g.Hits,
                    LastReferral = g.LastReferral
                }).ToList();

                await _context.ReferrerStats.AddRangeAsync(stats);
                await _context.SaveChangesAsync();

                processedCount += batch.Count;
                System.Console.WriteLine($"Processed {processedCount}/{totalCount} referrer statistics...");
            }
        }

        private string GetSafeHost(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            try
            {
                return new Uri(url).Host;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task CalculateBrowserStatisticsAsync(IQueryable<LogEntry> query)
        {
            const int batchSize = 1000;
            var processedCount = 0;

            var statsQuery = query
                .GroupBy(e => e.UserAgent)
                .Select(g => new
                {
                    UserAgent = g.Key,
                    Hits = g.Count(),
                    LastAccess = g.Max(e => e.Date)
                });

            var totalCount = await statsQuery.CountAsync();

            while (processedCount < totalCount)
            {
                var batch = await statsQuery
                    .Skip(processedCount)
                    .Take(batchSize)
                    .ToListAsync();

                var stats = batch.Select(g => new BrowserStatistics
                {
                    BrowserType = g.UserAgent ?? string.Empty,
                    Version = string.Empty,
                    OperatingSystem = string.Empty,
                    DeviceType = string.Empty,
                    IsSpider = !string.IsNullOrEmpty(g.UserAgent) &&
                        (g.UserAgent.ToLower().Contains("bot") || g.UserAgent.ToLower().Contains("spider")),
                    Hits = g.Hits,
                    LastAccess = g.LastAccess
                }).ToList();

                await _context.BrowserStats.AddRangeAsync(stats);
                await _context.SaveChangesAsync();

                processedCount += batch.Count;
                System.Console.WriteLine($"Processed {processedCount}/{totalCount} browser statistics...");
            }
        }

        private async Task CalculateErrorStatisticsAsync(IQueryable<LogEntry> query)
        {
            const int batchSize = 1000;
            var processedCount = 0;

            var statsQuery = query
                .Where(e => e.StatusCode >= 400)
                .GroupBy(e => new { e.StatusCode, e.SubStatusCode, e.Win32Status, e.UriStem })
                .Select(g => new
                {
                    g.Key.StatusCode,
                    g.Key.SubStatusCode,
                    g.Key.Win32Status,
                    UriStem = g.Key.UriStem,
                    Count = g.Count(),
                    LastOccurrence = g.Max(e => e.Date)
                });

            var totalCount = await statsQuery.CountAsync();

            while (processedCount < totalCount)
            {
                var batch = await statsQuery
                    .Skip(processedCount)
                    .Take(batchSize)
                    .ToListAsync();

                var stats = batch.Select(g => new ErrorStatistics
                {
                    StatusCode = g.StatusCode,
                    SubStatusCode = g.SubStatusCode,
                    Win32Status = g.Win32Status,
                    Page = g.UriStem ?? string.Empty,
                    Referrer = string.Empty,
                    Count = g.Count,
                    LastOccurrence = g.LastOccurrence
                }).ToList();

                await _context.ErrorStats.AddRangeAsync(stats);
                await _context.SaveChangesAsync();

                processedCount += batch.Count;
                System.Console.WriteLine($"Processed {processedCount}/{totalCount} error statistics...");
            }
        }

        public async Task GenerateReportAsync()
        {
            System.Console.WriteLine("Generating report...");
            var reportGenerator = new ReportGenerator(_context);
            await reportGenerator.GenerateReportAsync();
        }
    }
}