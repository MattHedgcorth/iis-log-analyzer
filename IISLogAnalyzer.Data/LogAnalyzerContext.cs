using Microsoft.EntityFrameworkCore;
using IISLogAnalyzer.Domain.Models;
using IISLogAnalyzer.Domain.Interfaces;
using System;
using System.IO;

namespace IISLogAnalyzer.Data
{
    public class LogAnalyzerContext : DbContext, ILogAnalyzerContext
    {
        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<ActivityStatistics> ActivityStats { get; set; }
        public DbSet<AccessStatistics> AccessStats { get; set; }
        public DbSet<VisitorStatistics> VisitorStats { get; set; }
        public DbSet<ReferrerStatistics> ReferrerStats { get; set; }
        public DbSet<BrowserStatistics> BrowserStats { get; set; }
        public DbSet<ErrorStatistics> ErrorStats { get; set; }

        public string DbPath { get; private set; }

        public LogAnalyzerContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = Path.Join(path, "IISLogAnalyzer.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // LogEntry indexes
            modelBuilder.Entity<LogEntry>()
                .HasIndex(e => e.Date);
            modelBuilder.Entity<LogEntry>()
                .HasIndex(e => e.ClientIp);
            modelBuilder.Entity<LogEntry>()
                .HasIndex(e => e.UriStem);
            modelBuilder.Entity<LogEntry>()
                .HasIndex(e => e.StatusCode);

            // Activity statistics indexes
            modelBuilder.Entity<ActivityStatistics>()
                .HasIndex(e => e.Date);
            modelBuilder.Entity<ActivityStatistics>()
                .HasIndex(e => e.Hour);
            modelBuilder.Entity<ActivityStatistics>()
                .HasIndex(e => e.DayOfWeek);

            // Access statistics indexes
            modelBuilder.Entity<AccessStatistics>()
                .HasIndex(e => e.Page);
            modelBuilder.Entity<AccessStatistics>()
                .HasIndex(e => e.Directory);
            modelBuilder.Entity<AccessStatistics>()
                .HasIndex(e => e.FileType);

            // Visitor statistics indexes
            modelBuilder.Entity<VisitorStatistics>()
                .HasIndex(e => e.Host);
            modelBuilder.Entity<VisitorStatistics>()
                .HasIndex(e => e.Country);

            // Referrer statistics indexes
            modelBuilder.Entity<ReferrerStatistics>()
                .HasIndex(e => e.Site);
            modelBuilder.Entity<ReferrerStatistics>()
                .HasIndex(e => e.SearchEngine);

            // Browser statistics indexes
            modelBuilder.Entity<BrowserStatistics>()
                .HasIndex(e => e.BrowserType);
            modelBuilder.Entity<BrowserStatistics>()
                .HasIndex(e => e.OperatingSystem);

            // Error statistics indexes
            modelBuilder.Entity<ErrorStatistics>()
                .HasIndex(e => e.StatusCode);
            modelBuilder.Entity<ErrorStatistics>()
                .HasIndex(e => e.Page);
        }
    }
}