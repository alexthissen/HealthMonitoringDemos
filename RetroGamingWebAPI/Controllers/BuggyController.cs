using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RetroGamingWebAPI.HealthChecks;

namespace RetroGamingWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuggyController : ControllerBase
    {
        //private readonly IReadOnlyPolicyRegistry<string> registry;
        private readonly TripwireHealthCheck healthCheck;
        private readonly ForcedHealthCheck forcedHealthCheck;
        private readonly ILogger<BuggyController> logger;

        public BuggyController(
            //IReadOnlyPolicyRegistry<string> registry,
            TripwireHealthCheck tripWireHealthCheck,
            ForcedHealthCheck forcedHealthCheck,
            ILogger<BuggyController> logger)
        {
            //this.registry = registry;
            this.healthCheck = tripWireHealthCheck;
            this.forcedHealthCheck = forcedHealthCheck;
            this.logger = logger;
        }

        // GET api/buggy
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/buggy/5
        [HttpGet("{id}")]
        public ActionResult<int> Get(int id)
        {
            // For demo purposes, a GET will trip circuit breaker
            // CircuitBreakerPolicy breaker = registry.Get<CircuitBreakerPolicy>("DefaultBreaker");
            // breaker.Isolate();

            // And trip another health check. Very buggy GET method :)
            return healthCheck.Trip();
        }

        // POST api/buggy
        [HttpPost]
        public void Post([FromQuery] string status)
        {
            forcedHealthCheck.Force(status, Environment.MachineName);
        }

        // PUT api/buggy/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
            // if (registry.TryGet<CircuitBreakerPolicy>("DefaultBreaker", out var circuitBreaker))
            // {
            //     try
            //     {
            //         circuitBreaker.Execute(() => throw new HttpRequestException());
            //     }
            //     catch (BrokenCircuitException)
            //     {
            //         logger?.LogInformation("Circuit breaker is open. Immediate failure");
            //     }
            //     catch (HttpRequestException)
            //     {
            //         logger?.LogInformation("Normal HTTP Request failure being caught");
            //     }
            // }
        }

        // DELETE api/buggy/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
