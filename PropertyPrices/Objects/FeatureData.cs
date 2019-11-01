using System;
using System.Collections.Generic;
using System.Text;

namespace PropertyPrices
{
    public class FeatureData
    {

        public string RegionName { get; set; }
        public int Year { get; set; }
        public IEnumerable<double> FeatureValue { get; set; }

    }
}
