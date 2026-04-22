using OfficeOpenXml;
using OlapAnalytics.Application.DTOs.Upload;
using OlapAnalytics.Application.Interfaces;
using OlapAnalytics.Domain.Entities;
using OlapAnalytics.Domain.Interfaces;
using System.Text.Json;

namespace OlapAnalytics.Application.Services;

public class FileService : IFileService
{
    private readonly IDatasetRepository _datasetRepository;

    public FileService(IDatasetRepository datasetRepository)
    {
        _datasetRepository = datasetRepository;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    // ── Read sample rows from stream ───────────────────────────────────────────
    public async Task<List<Dictionary<string, object?>>> ReadExcelSampleAsync(Stream stream, int maxRows = 200)
    {
        using var package = new ExcelPackage(stream);
        return ParseWorksheet(package, maxRows);
    }

    // ── Read ALL rows from stream (for bulk insert) ────────────────────────────
    public async Task<(List<string> Headers, List<Dictionary<string, object?>> Rows)> ReadExcelAllAsync(Stream stream)
    {
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) throw new InvalidOperationException("Excel file is empty.");

        int rows = worksheet.Dimension?.Rows ?? 0;
        int cols = worksheet.Dimension?.Columns ?? 0;

        var headers = new List<string>();
        for (int c = 1; c <= cols; c++)
        {
            var val = worksheet.Cells[1, c].Text;
            headers.Add(string.IsNullOrWhiteSpace(val) ? $"Column{c}" : val.Trim());
        }

        var allRows = new List<Dictionary<string, object?>>();
        for (int r = 2; r <= rows; r++)
        {
            var rowDict = new Dictionary<string, object?>();
            for (int c = 1; c <= cols; c++)
                rowDict[headers[c - 1]] = worksheet.Cells[r, c].Value;
            allRows.Add(rowDict);
        }

        return (headers, allRows);
    }

    // ── Save upload: persist file bytes + sample JSON → DB ────────────────────
    public async Task<UploadResponse> SaveUploadAsync(int userId, string fileName, Stream fileStream)
    {
        // Read all bytes so we can both parse and store
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        var rawBytes = ms.ToArray();

        // Parse with EPPlus
        ms.Position = 0;
        using var package = new ExcelPackage(ms);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) throw new InvalidOperationException("Excel file is empty.");

        int rowCount = Math.Max(0, (worksheet.Dimension?.Rows ?? 1) - 1);
        int cols = worksheet.Dimension?.Columns ?? 0;

        var columns = new List<string>();
        for (int c = 1; c <= cols; c++)
            columns.Add(worksheet.Cells[1, c].Text.Trim());

        // Build sample (up to 200 rows)
        var sample = ParseWorksheet(package, 200);
        var sampleJson = JsonSerializer.Serialize(sample);

        var dataset = new Dataset
        {
            UserId       = userId,
            FileName     = fileName,
            DbName       = $"DW_{userId}_{DateTime.UtcNow.Ticks}",
            RowCount     = rowCount,
            Status       = "pending",
            RawFileBytes = rawBytes,
            SampleJson   = sampleJson
        };

        var dsId = await _datasetRepository.CreateAsync(dataset);

        // Preview = first 50 rows
        var preview = sample.Take(50).ToList();

        return new UploadResponse
        {
            DatasetId = dsId,
            FileName  = fileName,
            RowCount  = rowCount,
            Columns   = columns,
            Preview   = preview
        };
    }

    // ── Internal helper ────────────────────────────────────────────────────────
    private static List<Dictionary<string, object?>> ParseWorksheet(ExcelPackage package, int maxRows)
    {
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return new();

        int rows = worksheet.Dimension?.Rows ?? 0;
        int cols = worksheet.Dimension?.Columns ?? 0;
        var result = new List<Dictionary<string, object?>>();
        if (rows == 0 || cols == 0) return result;

        var headers = new List<string>();
        for (int c = 1; c <= cols; c++)
        {
            var val = worksheet.Cells[1, c].Text;
            headers.Add(string.IsNullOrWhiteSpace(val) ? $"Column{c}" : val.Trim());
        }

        int limit = Math.Min(rows, maxRows + 1);
        for (int r = 2; r <= limit; r++)
        {
            var row = new Dictionary<string, object?>();
            for (int c = 1; c <= cols; c++)
                row[headers[c - 1]] = worksheet.Cells[r, c].Value;
            result.Add(row);
        }
        return result;
    }
}
