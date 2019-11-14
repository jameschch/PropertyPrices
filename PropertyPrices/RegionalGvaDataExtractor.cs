using SharpLearning.InputOutput.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PropertyPrices
{
    //todo:
    public class RegionalGvaDataExtractor
    {
        private readonly string _path;
        private int _minimumYear = DateTime.UtcNow.Year;

        public RegionalGvaDataExtractor(string path = "regionalgvaibylainuk.csv")
        {
            _path = path;
        }

        //public Dictionary<int, IEnumerable<FeatureData>> Extract()
        //{

        //    var lines = File.ReadAllLines(_path);

        //    var data = new Dictionary<int, IEnumerable<FeatureData>>();

        //    foreach (var row in lines.Skip(8))
        //    {
        //        var values = row.Split(',').Select(s => s.Trim('"')).ToArray();

        //        if (values[0].Length == 4 && int.TryParse(values[0], out var parsed))
        //        {
        //            data.Add(parsed, new[] { new FeatureData { FeatureValue = new[] { double.Parse(values[1]) }, Year = parsed } });
        //        }
        //    }

        //    _minimumYear = data.Values.Min(d => d.Min(m => m.Year));

        //    return data;

        //}

        //public IEnumerable<double> Get(Dictionary<int, IEnumerable<FeatureData>> data, ModelData modelData)
        //{
        //    var year = modelData.Date.Year - 1;

        //    if (year < _minimumYear)
        //    {
        //        return new[] { -1d };
        //    }

        //    if (data.ContainsKey(year))
        //    {
        //        return data[year].Single().FeatureValue;
        //    }

        //    Program.StatusLogger.Info($"GVA data not found: {modelData.Name} {modelData.Date}");

        //    return new[] { -1d };
        //}

    }
}
