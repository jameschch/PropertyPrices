using NLog;
using System;

namespace PropertyPrices
{


    class Program
    {

           public static Logger Logger = LogManager.GetLogger("log");

        static void Main(string[] args)
        {
            new PricePredictionRanker().Predict();
        }
    }
}
