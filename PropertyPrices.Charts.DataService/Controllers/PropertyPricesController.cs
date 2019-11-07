using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace PropertyPrices.Charts.DataService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyPricesController : ControllerBase
    {


        private readonly ILogger<PropertyPricesController> _logger;

        public PropertyPricesController(ILogger<PropertyPricesController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("{id}")]
        public ContentResult Get(string id)
        {
            var json = System.IO.File.ReadAllText($"data\\{id}.json");
            return Content(json, "application/json");
        }

    }
}
