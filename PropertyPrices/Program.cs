using NLog;
using System;
using System.Linq;

namespace PropertyPrices
{


    class Program
    {

        public static Logger Logger = LogManager.GetLogger("log");
        public static Logger StatusLogger = LogManager.GetLogger("status");

        static void Main(string[] args)
        {
            new PricePredictionRanker().Predict(pauseAtEnd: true);
            //new PricePredictionUniversalRanker().Predict(args.Any() ? int.Parse(args[0]) : PricePredictionUniversalRanker.DefaultIterations);
        }
    }
}
