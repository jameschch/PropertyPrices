using SharpLearning.InputOutput.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PropertyPrices
{
    public class LondonDensityDataExtractor
    {

        private int _minimumYear = DateTime.UtcNow.Year;

        public Dictionary<string, IEnumerable<FeatureData>> Extract()
        {
            var header = File.ReadLines("housing-density-borough.csv").First();
            var parser = new CsvParser(() => new StringReader(File.ReadAllText("housing-density-borough.csv")), ',', true, true);
            var featureRows = parser.EnumerateRows(new[] { "Code", "Name", "Year", "Population", "Inland_Area_Hectares" }).ToArray();

            var data = new Dictionary<string, List<FeatureData>>();

            foreach (var row in featureRows)
            {
                var key = row.GetValue("Code");
                var name = row.GetValue("Name");

                if (!data.ContainsKey(key))
                {
                    data.Add(key, new List<FeatureData>());
                }
                data[key].Add(new FeatureData { Year = int.Parse(row.GetValue("Year")), FeatureValue = row.Values.Skip(3).Select(s => double.Parse(s)) });
            }

            _minimumYear = data.Values.Min(d => d.Min(m => m.Year));

            return data.ToDictionary(k => k.Key, v => v.Value.AsEnumerable());
        }

        public IEnumerable<double> Get(Dictionary<string, IEnumerable<FeatureData>> data, ModelData modelData)
        {

            if (!PricePredictionRanker.London.Contains(modelData.Name))
            {
                return new[] { -1d, -1d };
            }

            var year = modelData.Date.Year - 1;

            if (year < _minimumYear)
            {
                return new[] { -1d, -1d };
            }

            if (data.ContainsKey(modelData.Code) && data[modelData.Code].Any(m => m.Year == year))
            {
                return data[modelData.Code].Single(m => m.Year == year).FeatureValue;
            }
            else if (data.SelectMany(d => d.Value.Where(v => v.RegionName == modelData.Name && v.Year == year)).Any())
            {
                return data.SelectMany(d => d.Value.Where(v => v.RegionName == modelData.Name && v.Year == year)).Single().FeatureValue;
            }

            Program.StatusLogger.Info($"Density data not found: {modelData.Name} {modelData.Date}");

            return new[] { -1d, -1d };
        }

    }
}
