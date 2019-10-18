using PropertyPrices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace PropertyPricesTests
{
    public class BinaryFeatureEncoderTest
    {


        [Fact]
        public void EncodeAndDecode()
        {
            var keys = Enumerable.Range(0, 500).Select(s => s.ToString());
            var unit = new BinaryFeatureEncoder();

            var actual = unit.Encode(keys);

            foreach (var item in actual)
            {
                Assert.Equal(item.Key, unit.Decode(item.Value));
            }
        }


    }
}
