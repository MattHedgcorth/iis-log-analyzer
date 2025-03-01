using IISLogAnalyzer.Domain.Models;
using IISLogAnalyzer.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISLogAnalyzer.Core
{
    public class ReportGenerator
    {
        private readonly ILogAnalyzerContext _context;

        public ReportGenerator(ILogAnalyzerContext context)
        {
            _context = context;
        }

        public async Task GenerateReportAsync()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("    <title>IIS Log Analysis Report</title>");
            sb.AppendLine("    <link href=\"https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css\" rel=\"stylesheet\">");
            sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        .nav-fixed { position: fixed; top: 0; width: 100%; z-index: 1000; background: white; }");
            sb.AppendLine("        .content { margin-top: 60px; }");
            sb.AppendLine("        .chart-container { height: 400px; margin-bottom: 20px; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Navigation
            sb.AppendLine("<nav class=\"navbar navbar-expand-lg navbar-light bg-light nav-fixed\">");
            sb.AppendLine("    <div class=\"container-fluid\">");
            sb.AppendLine("        <a class=\"navbar-brand\" href=\"#\">IIS Log Analysis</a>");
            sb.AppendLine("        <div class=\"collapse navbar-collapse\">");
            sb.AppendLine("            <ul class=\"navbar-nav\">");
            sb.AppendLine("                <li class=\"nav-item\"><a class=\"nav-link\" href=\"#general\">General</a></li>");
            sb.AppendLine("                <li class=\"nav-item\"><a class=\"nav-link\" href=\"#activity\">Activity</a></li>");
            sb.AppendLine("                <li class=\"nav-item\"><a class=\"nav-link\" href=\"#access\">Access</a></li>");
            sb.AppendLine("                <li class=\"nav-item\"><a class=\"nav-link\" href=\"#visitors\">Visitors</a></li>");
            sb.AppendLine("                <li class=\"nav-item\"><a class=\"nav-link\" href=\"#referrers\">Referrers</a></li>");
            sb.AppendLine("                <li class=\"nav-item\"><a class=\"nav-link\" href=\"#browsers\">Browsers</a></li>");
            sb.AppendLine("                <li class=\"nav-item\"><a class=\"nav-link\" href=\"#errors\">Errors</a></li>");
            sb.AppendLine("            </ul>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</nav>");

            sb.AppendLine("<div class=\"container content\">");

            // General Statistics
            sb.AppendLine("<section id=\"general\" class=\"mb-5\">");
            sb.AppendLine("    <h2>General Statistics</h2>");
            await AppendGeneralStatisticsAsync(sb);
            sb.AppendLine("</section>");

            // Activity Statistics
            sb.AppendLine("<section id=\"activity\" class=\"mb-5\">");
            sb.AppendLine("    <h2>Activity Statistics</h2>");
            await AppendActivityStatisticsAsync(sb);
            sb.AppendLine("</section>");

            // Access Statistics
            sb.AppendLine("<section id=\"access\" class=\"mb-5\">");
            sb.AppendLine("    <h2>Access Statistics</h2>");
            await AppendAccessStatisticsAsync(sb);
            sb.AppendLine("</section>");

            // Visitor Statistics
            sb.AppendLine("<section id=\"visitors\" class=\"mb-5\">");
            sb.AppendLine("    <h2>Visitor Statistics</h2>");
            await AppendVisitorStatisticsAsync(sb);
            sb.AppendLine("</section>");

            // Referrer Statistics
            sb.AppendLine("<section id=\"referrers\" class=\"mb-5\">");
            sb.AppendLine("    <h2>Referrer Statistics</h2>");
            await AppendReferrerStatisticsAsync(sb);
            sb.AppendLine("</section>");

            // Browser Statistics
            sb.AppendLine("<section id=\"browsers\" class=\"mb-5\">");
            sb.AppendLine("    <h2>Browser Statistics</h2>");
            await AppendBrowserStatisticsAsync(sb);
            sb.AppendLine("</section>");

            // Error Statistics
            sb.AppendLine("<section id=\"errors\" class=\"mb-5\">");
            sb.AppendLine("    <h2>Error Statistics</h2>");
            await AppendErrorStatisticsAsync(sb);
            sb.AppendLine("</section>");

            sb.AppendLine("</div>");

            // JavaScript for interactivity
            sb.AppendLine("<script>");
            sb.AppendLine("const charts = {};");
            sb.AppendLine("function createChart(canvasId, type, labels, data, title) {");
            sb.AppendLine("    const ctx = document.getElementById(canvasId).getContext('2d');");
            sb.AppendLine("    charts[canvasId] = new Chart(ctx, {");
            sb.AppendLine("        type: type,");
            sb.AppendLine("        data: {");
            sb.AppendLine("            labels: labels,");
            sb.AppendLine("            datasets: [{");
            sb.AppendLine("                label: title,");
            sb.AppendLine("                data: data,");
            sb.AppendLine("                backgroundColor: 'rgba(54, 162, 235, 0.2)',");
            sb.AppendLine("                borderColor: 'rgba(54, 162, 235, 1)',");
            sb.AppendLine("                borderWidth: 1");
            sb.AppendLine("            }]");
            sb.AppendLine("        },");
            sb.AppendLine("        options: {");
            sb.AppendLine("            responsive: true,");
            sb.AppendLine("            maintainAspectRatio: false");
            sb.AppendLine("        }");
            sb.AppendLine("    });");
            sb.AppendLine("}");
            sb.AppendLine("</script>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            var reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "report.html");
            await File.WriteAllTextAsync(reportPath, sb.ToString());
        }

        private async Task AppendGeneralStatisticsAsync(StringBuilder sb)
        {
            var totalHits = await _context.LogEntries.CountAsync();
            var uniqueVisitors = await _context.LogEntries.Select(e => e.ClientIp).Distinct().CountAsync();
            var totalBandwidth = await _context.LogEntries.SumAsync(e => e.BytesSent + e.BytesReceived);
            var errorCount = await _context.LogEntries.CountAsync(e => e.StatusCode >= 400);

            sb.AppendLine("<div class=\"row\">");
            sb.AppendLine($"<div class=\"col-md-3\"><div class=\"card\"><div class=\"card-body\"><h5>Total Hits</h5><p>{totalHits:N0}</p></div></div></div>");
            sb.AppendLine($"<div class=\"col-md-3\"><div class=\"card\"><div class=\"card-body\"><h5>Unique Visitors</h5><p>{uniqueVisitors:N0}</p></div></div></div>");
            sb.AppendLine($"<div class=\"col-md-3\"><div class=\"card\"><div class=\"card-body\"><h5>Total Bandwidth</h5><p>{FormatBytes(totalBandwidth)}</p></div></div></div>");
            sb.AppendLine($"<div class=\"col-md-3\"><div class=\"card\"><div class=\"card-body\"><h5>Error Count</h5><p>{errorCount:N0}</p></div></div></div>");
            sb.AppendLine("</div>");
        }

        private async Task AppendActivityStatisticsAsync(StringBuilder sb)
        {
            var dailyStats = await _context.ActivityStats
                .OrderBy(s => s.Date)
                .Take(30)
                .ToListAsync();

            sb.AppendLine("<div class=\"chart-container\">");
            sb.AppendLine("    <canvas id=\"dailyHitsChart\"></canvas>");
            sb.AppendLine("</div>");

            sb.AppendLine("<script>");
            sb.AppendLine("createChart('dailyHitsChart', 'line', ");
            sb.AppendLine($"    [{string.Join(",", dailyStats.Select(s => $"\"{s.Date:MM/dd}\""))}], ");
            sb.AppendLine($"    [{string.Join(",", dailyStats.Select(s => s.Hits))}], ");
            sb.AppendLine("    'Daily Hits');");
            sb.AppendLine("</script>");
        }

        private async Task AppendAccessStatisticsAsync(StringBuilder sb)
        {
            var topPages = await _context.AccessStats
                .OrderByDescending(s => s.Hits)
                .Take(10)
                .ToListAsync();

            sb.AppendLine("<div class=\"chart-container\">");
            sb.AppendLine("    <canvas id=\"topPagesChart\"></canvas>");
            sb.AppendLine("</div>");

            sb.AppendLine("<script>");
            sb.AppendLine("createChart('topPagesChart', 'bar', ");
            sb.AppendLine($"    [{string.Join(",", topPages.Select(s => $"\"{s.Page}\""))}], ");
            sb.AppendLine($"    [{string.Join(",", topPages.Select(s => s.Hits))}], ");
            sb.AppendLine("    'Top Pages');");
            sb.AppendLine("</script>");
        }

        private async Task AppendVisitorStatisticsAsync(StringBuilder sb)
        {
            var topVisitors = await _context.VisitorStats
                .OrderByDescending(s => s.Hits)
                .Take(10)
                .ToListAsync();

            sb.AppendLine("<div class=\"chart-container\">");
            sb.AppendLine("    <canvas id=\"topVisitorsChart\"></canvas>");
            sb.AppendLine("</div>");

            sb.AppendLine("<script>");
            sb.AppendLine("createChart('topVisitorsChart', 'bar', ");
            sb.AppendLine($"    [{string.Join(",", topVisitors.Select(s => $"\"{s.Host}\""))}], ");
            sb.AppendLine($"    [{string.Join(",", topVisitors.Select(s => s.Hits))}], ");
            sb.AppendLine("    'Top Visitors');");
            sb.AppendLine("</script>");
        }

        private async Task AppendReferrerStatisticsAsync(StringBuilder sb)
        {
            var topReferrers = await _context.ReferrerStats
                .OrderByDescending(s => s.Hits)
                .Take(10)
                .ToListAsync();

            sb.AppendLine("<div class=\"chart-container\">");
            sb.AppendLine("    <canvas id=\"topReferrersChart\"></canvas>");
            sb.AppendLine("</div>");

            sb.AppendLine("<script>");
            sb.AppendLine("createChart('topReferrersChart', 'bar', ");
            sb.AppendLine($"    [{string.Join(",", topReferrers.Select(s => $"\"{s.Site}\""))}], ");
            sb.AppendLine($"    [{string.Join(",", topReferrers.Select(s => s.Hits))}], ");
            sb.AppendLine("    'Top Referrers');");
            sb.AppendLine("</script>");
        }

        private async Task AppendBrowserStatisticsAsync(StringBuilder sb)
        {
            var browserStats = await _context.BrowserStats
                .OrderByDescending(s => s.Hits)
                .Take(10)
                .ToListAsync();

            sb.AppendLine("<div class=\"chart-container\">");
            sb.AppendLine("    <canvas id=\"browserStatsChart\"></canvas>");
            sb.AppendLine("</div>");

            sb.AppendLine("<script>");
            sb.AppendLine("createChart('browserStatsChart', 'pie', ");
            sb.AppendLine($"    [{string.Join(",", browserStats.Select(s => $"\"{s.BrowserType}\""))}], ");
            sb.AppendLine($"    [{string.Join(",", browserStats.Select(s => s.Hits))}], ");
            sb.AppendLine("    'Browser Distribution');");
            sb.AppendLine("</script>");
        }

        private async Task AppendErrorStatisticsAsync(StringBuilder sb)
        {
            var errorStats = await _context.ErrorStats
                .OrderByDescending(s => s.Count)
                .Take(10)
                .ToListAsync();

            sb.AppendLine("<div class=\"chart-container\">");
            sb.AppendLine("    <canvas id=\"errorStatsChart\"></canvas>");
            sb.AppendLine("</div>");

            sb.AppendLine("<script>");
            sb.AppendLine("createChart('errorStatsChart', 'bar', ");
            sb.AppendLine($"    [{string.Join(",", errorStats.Select(s => $"\"{s.StatusCode} - {s.Page}\""))}], ");
            sb.AppendLine($"    [{string.Join(",", errorStats.Select(s => s.Count))}], ");
            sb.AppendLine("    'Top Errors');");
            sb.AppendLine("</script>");
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }
}