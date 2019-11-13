using System;
using System.IO;

namespace PropertyPrices.Batcher
{
    class Program
    {

        static string[] TargetNames = new[] { "AveragePrice", "Index", "IndexSA", "AveragePriceSA", "SalesVolume", "DetachedPrice", "DetachedIndex", "SemiDetachedPrice", "SemiDetachedIndex", "TerracedPrice", "TerracedIndex", "FlatPrice", "FlatIndex", "Flat1m%Change", "Flat12m%Change", "CashPrice", "CashIndex", "CashSalesVolume", "MortgagePrice", "MortgageIndex", "MortgageSalesVolume", "FTBPrice", "FTBIndex", "FOOPrice", "FOOIndex", "NewPrice", "NewIndex", "NewSalesVolume", "OldPrice", "OldIndex", "OldSalesVolume" };

        static void Main(string[] args)
        {
            foreach (var target in TargetNames)
            {
                foreach (var offset in new[] { 1, 3, 5, 10 })
                {
                    Console.WriteLine($"Commencing: {target} {offset}");
                    new PropertyPrices.PricePredictionRanker().Predict(1600, offset, target);
                    //File.Move("log.txt", $"..\\..\\..\\..\\{target.Replace("%", ".")}-{offset}.csv");
                    Console.WriteLine($"Prediction complete: {target} {offset}");
                }
            }

        }
    }
}
