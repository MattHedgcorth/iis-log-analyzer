using IISLogAnalyzer.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace IISLogAnalyzer.Domain.Interfaces
{
    public interface ILogAnalyzerContext
    {
        DbSet<LogEntry> LogEntries { get; set; }
        DbSet<ActivityStatistics> ActivityStats { get; set; }
        DbSet<AccessStatistics> AccessStats { get; set; }
        DbSet<VisitorStatistics> VisitorStats { get; set; }
        DbSet<ReferrerStatistics> ReferrerStats { get; set; }
        DbSet<BrowserStatistics> BrowserStats { get; set; }
        DbSet<ErrorStatistics> ErrorStats { get; set; }

        Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default);
        
        Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
    }
}