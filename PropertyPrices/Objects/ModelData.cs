using System;

namespace PropertyPrices
{
    public class ModelData
    {
        public string Name;
        public string Code;
        public DateTime Date;
        public double[] Observations;
        public double OriginalTarget;
        public double Target = -1;
    }


}
