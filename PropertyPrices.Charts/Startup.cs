using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PropertyPrices.Charts
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string> { { "baseUrl", "http://localhost:63125" } })
#if RELEASE
                    .AddInMemoryCollection(new Dictionary<string, string> { { "baseUrl", "https://" } })
#endif
      .Build();
                services.AddSingleton<IConfigurationRoot>(config);

            }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
