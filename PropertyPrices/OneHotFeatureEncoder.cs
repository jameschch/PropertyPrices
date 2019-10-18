using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PropertyPrices
{
    public class OneHotFeatureEncoder
    {

        public Dictionary<string, double[]> Encode(IEnumerable<string> keys)
        {
            var filtered = keys.Distinct().OrderBy(o => o).Select((v, i) => (value: v, index: i));

            return filtered.ToDictionary(k => k.value, v => Transform(v.index, filtered.Count()));
        }

        public string Decode(IEnumerable<string> keys, double[] features)
        {
            var oneHot = features.TakeLast(keys.Count());
            return keys.ElementAt(Array.IndexOf(oneHot.ToArray(), 1));
        }

        private double[] Transform(int index, int length)
        {
            var zeroes = Enumerable.Repeat(0d, length).ToArray();
            zeroes[index] = 1d;

            return zeroes;
        }

    }
}
