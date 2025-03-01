# IIS Log Analyzer

A C# console application that analyzes IIS log files and produces an interactive HTML report.

## Features

- Parses IIS log files and stores data in SQLite database
- Generates interactive HTML reports with charts and statistics
- Supports filtering by domains/IPs
- Calculates various metrics including:
  - Activity statistics (daily, hourly, weekly)
  - Access statistics (popular pages, file types)
  - Visitor statistics (top hosts, countries)
  - Referrer statistics (referring sites, search engines)
  - Browser statistics (browser types, operating systems)
  - Error statistics (404s and other errors)

## Usage

```bash
IISLogAnalyzer.Console "C:\inetpub\logs\LogFiles"
```

### Command Line Arguments

Required:
- Log File Folders: Comma-delimited list of directories to scan for log files (enclosed in quotes)

Optional:
- `-e, --extensions`: Comma-delimited list of file extensions to filter log files (default: .log)
- `-f, --filter`: Comma-delimited list of domains/IPs to filter report data
- `--force`: Force reload of all files regardless of previous parsing
- `--rebuild`: Delete and recreate database before parsing
- `-h, --help`: Display parameter descriptions and usage examples

### Examples

```bash
# Basic usage
IISLogAnalyzer.Console "C:\inetpub\logs\LogFiles"

# Multiple folders with custom extensions and domain filters
IISLogAnalyzer.Console "C:\Logs\Site1,C:\Logs\Site2" -e ".log,.txt" -f "example.com,192.168.1.1"

# Force reload and rebuild database
IISLogAnalyzer.Console "C:\Logs" --force --rebuild
```

## Project Structure

- **IISLogAnalyzer.Console**: Command-line interface and program entry point
- **IISLogAnalyzer.Core**: Core business logic and report generation
- **IISLogAnalyzer.Data**: Database context and data access
- **IISLogAnalyzer.Domain**: Domain models and shared types

## Technical Details

- Built with .NET 7.0
- Uses SQLite for data storage
- Generates self-contained HTML reports with Chart.js for visualizations
- Bootstrap 5.1.3 for report styling

## Report Features

The generated HTML report includes:

1. General Statistics
   - Total hits
   - Unique visitors
   - Total bandwidth
   - Error count

2. Activity Statistics
   - Daily traffic trends
   - Hourly distribution
   - Weekly patterns

3. Access Statistics
   - Most requested pages
   - Popular file types
   - Directory analysis

4. Visitor Statistics
   - Top visitors
   - Geographic distribution
   - Authentication patterns

5. Referrer Statistics
   - Top referring sites
   - Search engine breakdown
   - Search terms analysis

6. Browser Statistics
   - Browser types
   - Operating systems
   - Device categories
   - Spider/bot activity

7. Error Statistics
   - Error code distribution
   - 404 analysis
   - Error trends

## License

This project is licensed under the MIT License - see the LICENSE file for details.
