using SharpLearning.InputOutput.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PropertyPrices
{
    public class GvaDataExtractor
    {
        private readonly string _path;
        private int _minimumYear = DateTime.UtcNow.Year;

        public GvaDataExtractor(string path = "series-311019-1.csv")
        {
            _path = path;
        }

        public Dictionary<string, IEnumerable<FeatureData>> Extract()
        {

            var parser = new CsvParser(() => new StringReader(File.ReadAllText(_path)), ',', true, false);

            var featureRows = parser.EnumerateRows().ToArray();

            var data = new Dictionary<string, IEnumerable<FeatureData>>();

            foreach (var row in featureRows)
            {

                if (row.Values[0].Length == 4 && int.TryParse(row.Values[0], out var parsed))
                {
                    data.Add(parsed.ToString(), new[] { new FeatureData { FeatureValue = new[] { double.Parse(row.Values[1]) }, Year = parsed } });
                }
            }

            _minimumYear = data.Values.Min(d => d.Min(m => m.Year));

            return data;
        }

        public IEnumerable<double> Get(Dictionary<string, IEnumerable<FeatureData>> data, ModelData modelData)
        {
            var year = modelData.Date.Year - 1;

            if (year < _minimumYear)
            {
                return new[] { -1d };
            }

            if (data.ContainsKey(year.ToString()))
            {
                return data[year.ToString()].Single().FeatureValue;
            }

            Program.StatusLogger.Info($"Population data not found: {modelData.Name} {modelData.Date}");

            return new[] { -1d };
        }

    }
}
