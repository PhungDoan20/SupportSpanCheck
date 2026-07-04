using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace SupportSpanCheck.Models
{
    public class SpanStandard
    {
        public string Size { get; set; } = "";
        public double MaxSpan { get; set; } = 1000;
        public double MaxSpanInsulated { get; set; } = 1000;
    }

    public static class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SupportSpanCheck", "settings.json");

        public static ObservableCollection<SpanStandard> LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var list = JsonSerializer.Deserialize<List<SpanStandard>>(json);
                    if (list != null) return new ObservableCollection<SpanStandard>(list);
                }
            }
            catch { }
            return new ObservableCollection<SpanStandard>
            {
                new SpanStandard { Size = "50", MaxSpan = 3000, MaxSpanInsulated = 2500 },
                new SpanStandard { Size = "100", MaxSpan = 5000, MaxSpanInsulated = 4500 }
            };
        }

        public static void SaveSettings(IEnumerable<SpanStandard> standards)
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                string json = JsonSerializer.Serialize(standards, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }

    public static class SizeConverter
    {
        private static readonly Dictionary<string, string> MetricToImperial = new Dictionary<string, string>
        {
            { "15", "1/2\"" },
            { "20", "3/4\"" },
            { "25", "1\"" },
            { "40", "1.5\"" },
            { "50", "2\"" },
            { "65", "2.5\"" },
            { "80", "3\"" },
            { "100", "4\"" },
            { "150", "6\"" },
            { "200", "8\"" },
            { "250", "10\"" },
            { "300", "12\"" },
            { "350", "14\"" },
            { "400", "16\"" },
            { "450", "18\"" },
            { "500", "20\"" },
            { "600", "24\"" }
        };

        private static readonly Dictionary<string, string> ImperialToMetric = new Dictionary<string, string>();

        static SizeConverter()
        {
            foreach (var kvp in MetricToImperial)
            {
                ImperialToMetric[kvp.Value] = kvp.Key;
                ImperialToMetric[kvp.Value.Replace("\"", "")] = kvp.Key;
            }
        }

        public static string NormalizeSize(string size)
        {
            if (string.IsNullOrWhiteSpace(size)) return "";
            size = size.Trim();
            
            if (MetricToImperial.ContainsKey(size)) return size;

            if (ImperialToMetric.TryGetValue(size, out string? metric))
                return metric;

            if (!size.EndsWith("\"") && float.TryParse(size, out _))
            {
                if (ImperialToMetric.TryGetValue(size, out string? metric2))
                    return metric2;
            }

            return size; 
        }

        public static double GetMaxSpan(string pipeSize, bool isInsulated, IEnumerable<SpanStandard> standards, out bool isFound)
        {
            string normalizedPipe = NormalizeSize(pipeSize);
            foreach (var std in standards)
            {
                if (NormalizeSize(std.Size) == normalizedPipe)
                {
                    isFound = true;
                    return isInsulated ? std.MaxSpanInsulated : std.MaxSpan;
                }
            }
            isFound = false;
            return 1000.0;
        }
    }
}
