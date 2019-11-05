using Blazor.DynamicJavascriptRuntime.Evaluator;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PropertyPrices.Charts.Pages
{
    public class ChartPage : ComponentBase
    {
        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await new EvalContext(JSRuntime).InvokeAsync<dynamic>("showPlot();");

            await base.OnAfterRenderAsync(firstRender);
        }

    }
}
