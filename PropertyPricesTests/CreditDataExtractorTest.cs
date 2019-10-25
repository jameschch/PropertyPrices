using PropertyPrices;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace PropertyPricesTests
{
    public class CreditDataExtractorTest
    {

        CreditDataExtractor _unit = new CreditDataExtractor();

        [Theory]
        [InlineData(1, "Dec00")]
        [InlineData(2, "Dec00")]
        [InlineData(3, "Dec00")]
        [InlineData(4, "Mar01")]
        [InlineData(5, "Mar01")]
        [InlineData(6, "Mar01")]
        [InlineData(7, "Jun01")]
        [InlineData(8, "Jun01")]
        [InlineData(9, "Jun01")]
        [InlineData(10, "Sep01")]
        [InlineData(11, "Sep01")]
        [InlineData(12, "Sep01")]
        public void GetMonthOfPreviousQuarterTest(int month, string expected)
        {
            var date = new DateTime(2001, month, 1);

            var actual = _unit.GetMonthOfPreviousQuarter(date);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(1, "Mar01")]
        [InlineData(2, "Mar01")]
        [InlineData(3, "Mar01")]
        [InlineData(4, "Jun01")]
        [InlineData(5, "Jun01")]
        [InlineData(6, "Jun01")]
        [InlineData(7, "Sep01")]
        [InlineData(8, "Sep01")]
        [InlineData(9, "Sep01")]
        [InlineData(10, "Dec01")]
        [InlineData(11, "Dec01")]
        [InlineData(12, "Dec01")]
        public void GetMonthOfNextQuarterTest(int month, string expected)
        {
            var date = new DateTime(2001, month, 1);

            var actual = _unit.GetMonthOfNextQuarter(date);
            Assert.Equal(expected, actual);
        }

    }
}
