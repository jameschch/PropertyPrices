using PropertyPrices;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace PropertyPricesTests
{
    public class PopulationDataExtractorTest
    {

        PopulationDataExtractor _unit = new PopulationDataExtractor("MYEB3_summary_components_of_change_series_UK_(2018_geog19).csv", "ladcode19", "laname19");

        [Fact]
        public void ExtractTest()
        {
            _unit.Extract();

        }



    }
}
