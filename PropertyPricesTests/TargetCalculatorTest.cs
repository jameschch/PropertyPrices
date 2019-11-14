using PropertyPrices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace PropertyPricesTests
{
    public class TargetCalculatorTest
    {

        TargetCalculator _unit = new TargetCalculator();

        [Theory]
        [InlineData(100, 150, 1, 0.33)]
        [InlineData(100, 50, 1, -0.5)]
        [InlineData(-10, 10, 1, 2)]
        [InlineData(10, -10, 1, -2)]
        [InlineData(-1, 1, 1, 2)]
        [InlineData(null, 1, 1, -1)]
        [InlineData(1, null, 1, -1)]
        [InlineData(100, 150, 2, 0.67)]
        public void GivenBeforeAndAfterThenShouldCalculateRelativeDifference(double? before, double? after, int offset, double expected)
        {

            var data = new ConcurrentDictionary<int, ModelData>(new Dictionary<int, ModelData>
            {
                { 0, new ModelData { OriginalTarget = before, Date = new DateTime(2001,1,1)  } },
                { 1, new ModelData { OriginalTarget = after, Date = new DateTime(2002,1,1)   } },
                { 2, new ModelData { OriginalTarget = after*2, Date = new DateTime(2003,1,1)  } }
            });

            _unit.Calculate(data, offset);
            var actual = data.Values.First().Target;

            Assert.Equal(expected, actual, 2);

        }

    }
}
