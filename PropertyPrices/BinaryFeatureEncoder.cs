using DeepEqual.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PropertyPrices
{
    public class BinaryFeatureEncoder
    {

        private Dictionary<string, double[]> _codeBook;

        public Dictionary<string, double[]> Encode(IEnumerable<string> keys)
        {
            var filtered = keys.Distinct().OrderBy(o => o).Select((v, i) => (value: v, index: i));

            _codeBook = filtered.ToDictionary(k => k.value, v => Transform(v.index, filtered.Count()).ToArray());

            return _codeBook;
        }

        private IEnumerable<double> Transform(int index, int length)
        {
            var zeroes = Enumerable.Repeat(default(bool), length).ToList();
            zeroes[index] = true;

            var encoded = new List<double>();

            foreach (var chunk in SplitList(zeroes, 64))
            {
                var bitArray = new BitArray(chunk.ToArray());
                var len = Math.Min(64, bitArray.Count);
                ulong n = 0;
                for (int i = 0; i < len; i++)
                {
                    if (bitArray.Get(i))
                        n |= 1UL << i;
                }

                encoded.Add(n);
            }

            return encoded;
        }

        public IEnumerable<IEnumerable<T>> SplitList<T>(List<T> splitting, int size = 64)
        {
            for (int i = 0; i < splitting.Count(); i += size)
            {
                yield return splitting.GetRange(i, Math.Min(size, splitting.Count() - i));
            }
        }


        public string Decode(IEnumerable<double> features)
        {

            if (_codeBook == null)
            {
                throw new Exception();
            }

            var numberOfChunks = (int)Math.Ceiling(_codeBook.Count() / 64d);
            var encoded = features.TakeLast(numberOfChunks).ToArray();

            var decoded = _codeBook.Where(s => Enumerable.SequenceEqual(s.Value, encoded));

            if (decoded.Count() > 1)
            {
                System.Diagnostics.Debugger.Break();
            }

            return decoded.First().Key;
        }


        //private double Transform(int index, int length)
        //{
        //    var zeroes = new Enumerable.Repeat(default(byte), length).ToArray();
        //    zeroes[index] = 1;

        //    var current = zeroes[0];
        //    for (int i = 1; i < zeroes.Count(); i++)
        //    {
        //        current = current | zeroes[i] << i;
        //    }

        //    return current;
        //}

    }
}
