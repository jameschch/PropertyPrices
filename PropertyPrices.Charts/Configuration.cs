using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PropertyPrices.Charts
{
    public class Configuration : Dictionary<string, string>
    {
        public Configuration()
        {
#if DEBUG
            this.Add("BaseUrl", "http://localhost:63125");
#else
            this.Add("BaseUrl", "https://");
#endif
        }

    }
}
