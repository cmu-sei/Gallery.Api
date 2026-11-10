// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Net;

namespace Gallery.Api.Infrastructure.Exceptions
{
    /// <summary>Another Crucible service a request depends on could not answer it.</summary>
    public class ServiceUnavailableException : Exception, IApiException
    {
        public ServiceUnavailableException()
            : base("Service Unavailable")
        {
        }

        public ServiceUnavailableException(string message)
            : base(message)
        {
        }

        public ServiceUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public HttpStatusCode GetStatusCode()
        {
            return HttpStatusCode.ServiceUnavailable;
        }
    }
}
