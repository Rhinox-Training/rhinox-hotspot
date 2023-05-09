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
            if (string.IsNullOrWhiteSpace(filePath) || benchmark == null || benchmark.ResultsByStage.IsNullOrEmpty())
                return false;
            filePath = ParseFilePath(filePath);

            DataTable table = null;
            if (FileHelper.Exists(filePath) && append)
            {
                table = ReadTable(filePath);
                if (table.Columns.Count > 0 && table.Columns[0].ColumnName != "Stage")
                    table.Columns.Add("Stage");

                var results = benchmark.ResultsByStage;
                foreach (var stage in results.Keys)
                {
                    var sortedResults = new List<BenchmarkResultEntry>();
                    for (int i = 0; i < results[stage].Count; ++i)
                        sortedResults.Add(EmptyEntry());
                    for (var i = 0; i < results[stage].Count; i++)
                    {
                        var result = results[stage][i];
                        int index = table.Columns.IndexOf(string.Join("/", result.Name, "AVERAGE"));
                        if (index == -1)
                        {
                            table.Columns.Add(string.Join("/", result.Name, "AVERAGE"));
                            table.Columns.Add(string.Join("/", result.Name, "STD.DEV"));

                            sortedResults.Add(result);
                        }
                        else
                        {
                            sortedResults[((index - 1) / 2)] = result;
                        }
                    }

                    var row = new List<object>();
                    row.Add(stage.ToString());
                    foreach (var result in sortedResults)
                    {
                        row.Add(result.Average.ToString());
                        row.Add(result.StdDev.ToString());
                    }
                    table.Rows.Add(row.ToArray()); // Needs to be an object array
                }
                
            }
            else
            {
                table = new DataTable();
                table.Columns.Add("Stage");

                if (benchmark.ResultsByStage.Values.Count > 0)
                {
                    foreach (var result in benchmark.ResultsByStage.Values.First())
                    {
                        table.Columns.Add(string.Join("/", result.Name, "AVERAGE"));
                        table.Columns.Add(string.Join("/", result.Name, "STD.DEV"));
                    }
                }

                foreach (var stage in benchmark.ResultsByStage.Keys)
                {
                    var row = new List<object>();
                    row.Add(stage.ToString());
                    foreach (var result in benchmark.ResultsByStage[stage])
                    {
                        row.Add(result.Average.ToString());
                        row.Add(result.StdDev.ToString());
                    }
                    table.Rows.Add(row.ToArray()); // Needs to be an object array
                }
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
            return Utility.ReadCsvTable(lines);
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