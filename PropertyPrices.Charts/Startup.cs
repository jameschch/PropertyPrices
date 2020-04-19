using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace PropertyPrices.Charts
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddSingleton(new Configuration());

        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
