using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertyPrices
{
    public class TargetCalculator
    {

        public void Calculate(ConcurrentDictionary<int, ModelData> data, int offset)
        {

            Program.StatusLogger.Info("Calculating Targets");
            Parallel.For(0, data.Count(), new ParallelOptions { MaxDegreeOfParallelism = -1 }, i =>
            {
                var item = data[i];
                if (item.OriginalTarget.HasValue)
                {
                    var forward = data.SingleOrDefault(d => d.Value.Name == item.Name && d.Value.Date == item.Date.AddYears(offset) && d.Value.OriginalTarget.HasValue);
                    if (forward.Value != null)
                    {
                        //relative difference
                        var change = forward.Value.OriginalTarget.Value - item.OriginalTarget.Value;
                        var percent = change / Math.Max(item.OriginalTarget.Value, forward.Value.OriginalTarget.Value);
                        if (Math.Abs(percent) > 0.7 && offset == 1)
                        {
                            Program.StatusLogger.Info($"Suspect target: {item.Name} {item.Date} {percent}");
                        }

                        item.Target = percent;
                    }
                    else
                    {
                        item.Target = -1;
                    }
                }
                else
                {
                    item.Target = -1;
                }

            });


        }

    }
}
