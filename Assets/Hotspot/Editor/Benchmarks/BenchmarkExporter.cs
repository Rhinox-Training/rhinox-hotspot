using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
using DataTableExtensions = Rhinox.Lightspeed.DataTableExtensions;

namespace Hotspot.Editor
{
    public static class BenchmarkExporter
    {
        public static bool Export(Benchmark benchmark, string filePath, bool append = true)
        {
            if (string.IsNullOrWhiteSpace(filePath) || benchmark == null || benchmark.Results.IsNullOrEmpty())
                return false;
            filePath = ParseFilePath(filePath);

            DataTable table = null;
            if (FileHelper.Exists(filePath) && append)
            {
                table = ReadTable(filePath);
                var sortedResults = new List<BenchmarkResultEntry>();
                for (int i = 0; i < benchmark.Results.Count; ++i)
                    sortedResults.Add(EmptyEntry());

                var results = benchmark.Results.ToArray();
                for (var i = 0; i < results.Length; i++)
                {
                    var result = results[i];
                    int index = table.Columns.IndexOf(string.Join("/", result.Name, "AVERAGE"));
                    if (index == -1)
                    {
                        table.Columns.Add(string.Join("/", result.Name, "AVERAGE"));
                        table.Columns.Add(string.Join("/", result.Name, "STD.DEV"));

                        sortedResults.Add(result);
                    }
                    else
                    {
                        sortedResults[index / 2] = result;
                    }
                }

                table.Rows.Add(sortedResults.SelectMany(x => new[] { x.Average, x.StdDev }));
                
            }
            else
            {
                table = new DataTable();
                foreach (var result in benchmark.Results)
                {
                    table.Columns.Add(string.Join("/", result.Name, "AVERAGE"));
                    table.Columns.Add(string.Join("/", result.Name, "STD.DEV"));
                }
                table.Rows.Add(benchmark.Results.SelectMany(x => new[] { x.Average, x.StdDev }));
            }
            
            string csvFileStr = table.ToCsv();
            FileInfo info = new FileInfo(filePath);
            FileHelper.CreateDirectoryIfNotExists(info.DirectoryName);
            File.WriteAllText(filePath, csvFileStr);
            return true;
        }

        private static DataTable ReadTable(string filePath)
        {
            string[] lines = FileHelper.ReadAllLines(filePath);
            return DataTableExtensions.ReadCsvTable(lines);
        }

        private static string ParseFilePath(string filePath)
        {
            return filePath;
        }

        private static BenchmarkResultEntry _emptyEntry;
        private static BenchmarkResultEntry EmptyEntry()
        {
            if (_emptyEntry == null)
                _emptyEntry = new BenchmarkResultEntry();
            return _emptyEntry;
        }
    }
}