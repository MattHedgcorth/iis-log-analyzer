using System;

namespace IISLogAnalyzer.Domain.Models
{
    public class LogEntry
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string ServerIp { get; set; }
        public string Method { get; set; }
        public string UriStem { get; set; }
        public string UriQuery { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string ClientIp { get; set; }
        public string UserAgent { get; set; }
        public string Referrer { get; set; }
        public int StatusCode { get; set; }
        public int SubStatusCode { get; set; }
        public int Win32Status { get; set; }
        public int TimeTaken { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
    }
}