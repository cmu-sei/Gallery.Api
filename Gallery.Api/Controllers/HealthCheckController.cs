// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using Microsoft.AspNetCore.Authorization;

namespace Gallery.Api.Controllers
{
    [AllowAnonymous]
    public class HealthController : BaseController
    {
        private readonly HealthCheckService healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            this.healthCheckService = healthCheckService;
        }

        /// <summary>
        /// Checks the liveliness health endpoint
        /// </summary>
        /// <remarks>
        /// Returns a HealthReport of the liveliness health check
        /// </remarks>
        /// <returns></returns>
        [HttpGet("health/live")]
        [ProducesResponseType(typeof(HealthReport), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "Health_GetLiveliness")]
        public async Task<IActionResult> GetLiveliness(CancellationToken ct)
        {
            HealthReport report = await this.healthCheckService.CheckHealthAsync((check) => check.Tags.Contains("live"));
            var result = new
            {
                status = report.Status.ToString()
            };
            return report.Status == HealthStatus.Healthy ? this.Ok(result) : this.StatusCode((int)HttpStatusCode.ServiceUnavailable, result);
        }

        /// <summary>
        /// Checks the readiness health endpoint
        /// </summary>
        /// <remarks>
        /// Returns a HealthReport of the readiness health check
        /// </remarks>
        /// <returns></returns>
        [HttpGet("health/ready")]
        [ProducesResponseType(typeof(HealthReport), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "Health_GetReadiness")]
        public async Task<IActionResult> GetReadiness(CancellationToken ct)
        {
            HealthReport report = await this.healthCheckService.CheckHealthAsync((check) => check.Tags.Contains("ready"));
            var result = new
            {
                status = report.Status.ToString()
            };
            return report.Status == HealthStatus.Healthy ? this.Ok(result) : this.StatusCode((int)HttpStatusCode.ServiceUnavailable, result);
        }
    }
}
