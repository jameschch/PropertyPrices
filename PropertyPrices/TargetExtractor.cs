using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertyPrices
{
    class TargetExtractor
    {

        public void Extract(ConcurrentDictionary<int, ModelData> data, int offset)
        {

            Program.StatusLogger.Info("Calculating Targets");
            Parallel.For(0, data.Count(), new ParallelOptions { MaxDegreeOfParallelism = -1 }, i =>
            {
                 var item = data[i];
                 if (item.OriginalTarget > 0)
                 {

                     var forward = data.SingleOrDefault(d => d.Value.Name == item.Name && d.Value.Date == item.Date.AddYears(offset) && d.Value.OriginalTarget > 0);
                     if (forward.Value != null)
                     {
                         var change = forward.Value.OriginalTarget - item.OriginalTarget;
                         var percent = change / item.OriginalTarget;
                         if (Math.Abs(percent) > 0.7 && offset == 1)
                         {
                            Program.StatusLogger.Info($"Suspect target: {item.Name} {item.Date} {percent}");
                         }

                         item.Target = percent;
                     }
                 }

             });


        }

    }
}
