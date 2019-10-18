using RestSharp;
using System;
using System.Collections.Generic;
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
            var lines = File.ReadAllLines("Bank of England  Database.csv");
            var cleansed = new List<(string, double[])>();

            foreach (var item in lines.Skip(1))
            {
                var split = item.Split(',').Select(s => s == "\"n/a\"" || s == "\"..\"" ? "0" : s.Trim('"'));
                var date = split.First().Trim('"').Split(" ");
                var parsed = (GetQuaterlyMonth(date[1]) + date[2], split.Skip(1).Select(s => double.Parse(s)));
                cleansed.Add((parsed.Item1, parsed.Item2.ToArray()));
            }


            var aggregate = cleansed.GroupBy(t => t.Item1).Select(g => GetAverage(g)).ToList();

            return aggregate.ToDictionary(k => k.Key, v => v.Value);
        }


        public string GetQuaterlyMonth(string month)
        {
            if (month == "Nov") { return "Dec"; }
            else if (month == "Oct") { return "Dec"; }
            else if (month == "Aug") { return "Sep"; }
            else if (month == "Jul") { return "Sep"; }
            else if (month == "May") { return "Jun"; }
            else if (month == "Apr") { return "Jun"; }
            else if (month == "Feb") { return "Mar"; }
            else if (month == "Jan") { return "Mar"; }
            else { return month; }
        }

        public string GetQuaterlyMonth(int month)
        {
            var parsed = new DateTime(1900, month, 1).ToString("MMM");
            return GetQuaterlyMonth(parsed);
        }

        public string GetKey(DateTime date)
        {
            var parsed = date.ToString("MMM");
            return  GetQuaterlyMonth(parsed) + date.ToString("yy");
        }

        private KeyValuePair<string, double[]> GetAverage(IGrouping<string, (string, double[])> g)
        {
            var average
                = new KeyValuePair<string, double[]>(
                g.Key,
                Enumerable.Range(0, g.First().Item2.Length - 1).Select(i => g.Select(g => g.Item2[i]).Average()).ToArray()
                );

            return average;
        }


    }
}
