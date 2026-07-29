// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Net;

namespace Gallery.Api.Infrastructure.Exceptions
{
    public class ConflictException : Exception, IApiException
    {
        public ConflictException()
            : base("Conflict")
        {
        }

        public ConflictException(string message)
            : base(message)
        {
        }

        public HttpStatusCode GetStatusCode()
        {
            return HttpStatusCode.Conflict;
        }
    }
}
