using System;
using System.Collections.Generic;

namespace IISLogAnalyzer.Domain.Models
{
    public class SummaryStatistics
    {
        public int TotalHits { get; set; }
        public int VisitorHits { get; set; }
        public int SpiderHits { get; set; }
        public int CachedHits { get; set; }
        public int FailedHits { get; set; }
        public double AverageHitsPerDay { get; set; }
        public double AverageHitsPerVisitor { get; set; }
        public int TotalPageViews { get; set; }
        public double AveragePageViewsPerDay { get; set; }
        public double AveragePageViewsPerVisitor { get; set; }
        public int TotalVisitors { get; set; }
        public double AverageVisitorsPerDay { get; set; }
        public int UniqueIPs { get; set; }
        public long TotalBandwidth { get; set; }
        public long VisitorBandwidth { get; set; }
        public long SpiderBandwidth { get; set; }
        public double AverageBandwidthPerDay { get; set; }
        public double AverageBandwidthPerHit { get; set; }
        public double AverageBandwidthPerVisitor { get; set; }
    }

    public class ActivityStatistics
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int Hour { get; set; }
        public string DayOfWeek { get; set; }
        public int Week { get; set; }
        public int Month { get; set; }
        public int Hits { get; set; }
        public int PageViews { get; set; }
        public int Visitors { get; set; }
        public long Bandwidth { get; set; }
    }

    public class AccessStatistics
    {
        public int Id { get; set; }
        public string Page { get; set; }
        public string Directory { get; set; }
        public string FileType { get; set; }
        public string VirtualDomain { get; set; }
        public int Hits { get; set; }
        public long Bandwidth { get; set; }
        public DateTime LastAccess { get; set; }
    }

    public class VisitorStatistics
    {
        public int Id { get; set; }
        public string Host { get; set; }
        public string TopLevelDomain { get; set; }
        public string Country { get; set; }
        public string AuthenticatedUser { get; set; }
        public int Hits { get; set; }
        public long Bandwidth { get; set; }
        public DateTime LastVisit { get; set; }
    }

    public class ReferrerStatistics
    {
        public int Id { get; set; }
        public string Site { get; set; }
        public string Url { get; set; }
        public string SearchEngine { get; set; }
        public string SearchPhrase { get; set; }
        public int Hits { get; set; }
        public DateTime LastReferral { get; set; }
    }

    public class BrowserStatistics
    {
        public int Id { get; set; }
        public string BrowserType { get; set; }
        public string Version { get; set; }
        public string OperatingSystem { get; set; }
        public string DeviceType { get; set; }
        public bool IsSpider { get; set; }
        public int Hits { get; set; }
        public DateTime LastAccess { get; set; }
    }

    public class ErrorStatistics
    {
        public int Id { get; set; }
        public int StatusCode { get; set; }
        public int SubStatusCode { get; set; }
        public int Win32Status { get; set; }
        public string Page { get; set; }
        public string Referrer { get; set; }
        public int Count { get; set; }
        public DateTime LastOccurrence { get; set; }
    }
}