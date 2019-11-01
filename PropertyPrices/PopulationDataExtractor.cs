using SharpLearning.InputOutput.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PropertyPrices
{
    public class PopulationDataExtractor
    {
        private readonly string _path;
        private readonly string _codeField;
        private readonly string _nameField;
        private int _minimumYear = DateTime.UtcNow.Year;

        public PopulationDataExtractor(string path, string codeField, string nameField)
        {
            _path = path;
            _codeField = codeField;
            _nameField = nameField;
        }

        public Dictionary<string, IEnumerable<FeatureData>> Extract()
        {

            var header = File.ReadLines(_path).First();
            var years = header.Split(",").Where(h => h.StartsWith("population")).Select(s => s.Split('_').Last()).ToArray();

            var parser = new CsvParser(() => new StringReader(File.ReadAllText(_path)), ',', true, true);


            var featureRows = parser.EnumerateRows((r) => new[] { _codeField, _nameField }.Contains(r) || r.StartsWith("population")).ToArray();

            var data = new Dictionary<string, IEnumerable<FeatureData>>();

            foreach (var row in featureRows)
            {
                var key = row.GetValue(_codeField);
                var name = row.GetValue(_nameField);

                var features = row.Values.Skip(2).Select((s, i) => new FeatureData { RegionName = name, FeatureValue = new[] { double.Parse(s) }, Year = int.Parse(years[i]) });

                data.Add(key, features);
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

            if (data.ContainsKey(modelData.Code) && data[modelData.Code].Any(m => m.Year == year))
            {
                return data[modelData.Code].Single(m => m.Year == year).FeatureValue;
            }
            else if (data.SelectMany(d => d.Value.Where(v => v.RegionName == modelData.Name && v.Year == year)).Any())
            {
                return data.SelectMany(d => d.Value.Where(v => v.RegionName == modelData.Name && v.Year == year)).Single().FeatureValue;
            }

            Program.StatusLogger.Info($"Population data not found: {modelData.Name} {modelData.Date}");

            return new[] { -1d };
        }

    }
}
