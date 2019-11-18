using Blazor.DynamicJavascriptRuntime.Evaluator;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PropertyPrices.Charts.Shared
{
    public class WaitBase : ComponentBase
    {

        protected virtual string Selector { get; set; } = "#wait";

        [Inject] public IJSRuntime JsRuntime { get; set; }

        public void Show()
        {
            using (dynamic context = new EvalContext(JsRuntime))
            {
                (context as EvalContext).Expression = () => context.jQuery(Selector).show();
            }

        }


        public void Hide()
        {
            using (dynamic context = new EvalContext(JsRuntime))
            {
                (context as EvalContext).Expression = () => context.jQuery(Selector).hide();
            }
        }

    }
}
