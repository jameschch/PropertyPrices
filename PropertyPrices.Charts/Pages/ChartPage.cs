using Blazor.DynamicJavascriptRuntime.Evaluator;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PropertyPrices.Charts.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyPrices.Charts.Pages
{
    public class ChartPage : ComponentBase
    {

        private string _controller = "PropertyPrices";

        [Inject]
        public IJSRuntime JSRuntime { get; set; }
        [Inject]
        public HttpClient HttpClient { get; set; }
        [Inject]
        public Configuration Config { get; set; }
        protected Wait Wait { get; set; }
        protected Analysis Analysis { get; set; }

        public Dictionary<string, string> ColumnOptions { get; } = new Dictionary<string, string>{ {"X12m.Change","12m % Change"},{"X1m.Change","1m % Change"},{"AveragePrice","Average Price"},
            {"AveragePriceSA","Average Price SA"},{"Cash12m.Change","Cash 12m % Change"},{"Cash1m.Change","Cash 1m % Change"},{"CashIndex","Cash Index"},{"CashPrice","Cash Price"},
            {"CashSalesVolume","Cash Sales Volume"},{"Detached12m.Change","Detached 12m % Change"},{"Detached1m.Change","Detached 1m % Change"},{"DetachedIndex","Detached Index"},
            {"DetachedPrice","Detached Price"},{"Flat12m.Change","Flat 12m % Change"},{"Flat1m.Change","Flat 1m % Change"},{"FlatIndex","Flat Index"},{"FlatPrice","Flat Price"},
            {"FOO12m.Change","FOO 12m % Change"},{"FOO1m.Change","FOO 1m % Change"},{"FOOIndex","FOO Index"},{"FOOPrice","FOO Price"},{"FTB12m.Change","FTB 12m % Change"},
            {"FTB1m.Change","FTB 1m % Change"},{"FTBIndex","FTB Index"},{"FTBPrice","FTB Price"},{"Index","Index"},{"IndexSA","Index SA"},{"Mortgage12m.Change","Mortgage 12m % Change"},
            {"Mortgage1m.Change","Mortgage 1m % Change"},{"MortgageIndex","Mortgage Index"},{"MortgagePrice","Mortgage Price"},{"MortgageSalesVolume","Mortgage Sales Volume"},
            {"New12m.Change","New 12m % Change"},{"New1m.Change","New 1m % Change"},{"NewIndex","New Index"},{"NewPrice","New Price"},{"NewSalesVolume","New Sales Volume"},
            {"Old12m.Change","Old 12m % Change"},{"Old1m.Change","Old 1m % Change"},{"OldIndex","Old Index"},{"OldPrice","Old Price"},{"OldSalesVolume","Old Sales Volume"},
            {"SalesVolume","Sales Volume"},{"SemiDetached12m.Change","Semi-Detached 12m % Change"},{"SemiDetached1m.Change","Semi-Detached 1m % Change"},
            {"SemiDetachedIndex","Semi-Detached Index"},{"SemiDetachedPrice","Semi-Detached Price"},{"Terraced12m.Change","Terraced 12m % Change"},{"Terraced1m.Change","Terraced 1m % Change"},
            {"TerracedIndex","Terraced Index"},{"TerracedPrice","Terraced Price"}};

        public Dictionary<string, string> ForecastColumnOptions { get; set; }

        public string Column { get; set; } = "AveragePrice";
        public string SelectedLayout { get; set; } = "lightLayout";
        public string HistoricalClass = "align-middle";
        public string ForecastClass = "d-none";

        protected override async Task OnInitializedAsync()
        {
            ForecastColumnOptions = GetFilteredColumnOptions();
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                using (dynamic context = new EvalContext(JSRuntime))
                {
                    (context as EvalContext).Expression = () => context.jQuery("#historicalSelect").val(Column);
                }
                await NewPlot("AveragePrice");
            }
        }

        public async Task ColumnChange(ChangeEventArgs e)
        {
            await NewPlot(e.Value.ToString());
            Column = e.Value.ToString();
        }

        private async Task NewPlot(string id)
        {
            Wait.Show();
            try
            {
                using (var reader = new StreamReader((await HttpClient.GetStreamAsync($"{Config["ApiUrl"]}/api/{_controller}/" + id))))
                {
                    await JSRuntime.InvokeAsync<dynamic>("PlotlyInterop.newPlot", reader.ReadToEnd());
                }
                Column = id;
            }
            finally
            {
                Wait.Hide();
            }
        }

        public async Task LightClick()
        {
            if (SelectedLayout != "lightLayout")
            {
                SelectedLayout = "lightLayout";
                ToggleTheme();
            }
        }
        public async Task DarkClick()
        {
            if (SelectedLayout != "darkLayout")
            {
                SelectedLayout = "darkLayout";
                ToggleTheme();
            }
        }

        private void ToggleTheme()
        {
            var background = "#000";
            var foreground = "#fff";

            if (SelectedLayout == "lightLayout")
            {
                background = "#fff";
                foreground = "#000";
            }

            using (dynamic context = new EvalContext(JSRuntime))
            {
                (context as EvalContext).Expression = () => context.PlotlyInterop.toggleTheme(foreground, background);
            }
        }

        public async Task HistoricalClick()
        {

            if (_controller != "PropertyPrices")
            {
                using (dynamic context = new EvalContext(JSRuntime))
                {
                    (context as EvalContext).Expression = () => context.jQuery("#historicalSelect").val(Column);
                }

                HistoricalClass = "align-middle";
                ForecastClass = "d-none";
                _controller = "PropertyPrices";
                await NewPlot(Column);
            }
        }
        public async Task ForecastClick()
        {
            if (_controller != "Forecast")
            {
                if (ForecastColumnOptions.ContainsValue(Column))
                {
                    using (dynamic context = new EvalContext(JSRuntime))
                    {
                        (context as EvalContext).Expression = () => context.jQuery("#forecastSelect").val(Column);
                    }
                }

                HistoricalClass = "d-none";
                ForecastClass = "align-middle";
                _controller = "Forecast";
                await NewPlot(Column);
            }
        }

        private Dictionary<string, string> GetFilteredColumnOptions()
        {
            return ColumnOptions.Where(w => !w.Value.Contains("%")).ToDictionary(k => k.Key, v => v.Value);
        }

        public async Task AnalysisClick()
        {
            Analysis.Show();
        }


    }
}
