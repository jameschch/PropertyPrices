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
            this.Add("BaseUrl", "https://propertyprices.org.uk");
#if DEBUG
            this.Add("ApiUrl", "http://localhost:63125");
#else
            this.Add("ApiUrl", "https://api.propertyprices.org.uk");
#endif
        }

    }
}
