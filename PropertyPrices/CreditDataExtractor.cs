using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PropertyPrices
{
    public class CreditDataExtractor
    {

        public Dictionary<string, double[]> Extract()
        {
            //https://www.bankofengland.co.uk/boeapps/database/fromshowcolumns.asp?Travel=NIxIRxSUx&FromSeries=1&ToSeries=50&DAT=ALL&FNY=&CSVF=TT&html.x=124&html.y=43&C=EOF&C=IW&C=KG&C=NB2&C=NB1&C=EOT&C=EOW&C=EP0&C=EP3&C=EP6&C=VS&C=PB&C=KI&C=RG&C=RO&C=U1P&C=U1R&C=U1Q&C=UKP&C=UKU&C=UKS&C=UKQ&C=UKT&C=UKV&C=OBW&C=UKR&C=UKW&C=OBZ&C=E32&Filter=N
            var lines = File.ReadAllLines("Bank of England Database.csv");
            var cleansed = new List<(string, double[])>();

            foreach (var item in lines.Skip(1))
            {
                var split = item.Split(',').Select(s => s == "\"n/a\"" || s == "\"..\"" ? "0" : s.Trim('"'));
                var date = DateTime.ParseExact(split.First().Trim('"'), "dd MMM yy", new CultureInfo("en-GB"));
                var parsed = (GetMonthOfNextQuarter(date), split.Skip(1).Select(s => double.Parse(s)));
                cleansed.Add((parsed.Item1, parsed.Item2.ToArray()));
            }

            var aggregate = cleansed.GroupBy(t => t.Item1).Select(g => GetAverage(g)).ToList();

            return aggregate.ToDictionary(k => k.Key, v => v.Value);
        }

        public string GetMonthOfNextQuarter(DateTime date)
        {
            while (date.Month % 3 > 0)
            {
                date = date.AddMonths(1);
            }

            return date.ToString("MMMyy");
        }

        public string GetMonthOfPreviousQuarter(DateTime date)
        {
            //there is look ahead bias for rate this month
            date = date.AddMonths(-1);

            while (date.Month % 3 > 0)
            {
                date = date.AddMonths(-1);
            }

            return date.ToString("MMMyy");
        }

        private KeyValuePair<string, double[]> GetAverage(IGrouping<string, (string, double[])> g)
        {
            var average = new KeyValuePair<string, double[]>
            (
                g.Key,
                Enumerable.Range(0, g.First().Item2.Length - 1).Select(i => g.Select(g => g.Item2[i]).Average()).ToArray()
            );

            return average;
        }

    }
}
