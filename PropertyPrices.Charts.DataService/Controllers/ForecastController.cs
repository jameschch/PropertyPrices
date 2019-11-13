using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace PropertyPrices.Charts.DataService
{

    [ApiController]
    [Route("api/[controller]")]
    public class ForecastController : ControllerBase
    {
        private readonly ILogger<ForecastController> _logger;

        public ForecastController(ILogger<ForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("{id}")]
        public ContentResult Get(string id)
        {
            var json = System.IO.File.ReadAllText($"forecastData\\{id}.json");
            return Content(json, "application/json");
        }
    }

}