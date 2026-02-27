// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gallery.Api.Infrastructure.Exceptions;
using Gallery.Api.ViewModels;
using System;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Data.SqlClient;

namespace Gallery.Api.Infrastructure.Filters
{
    public class JsonExceptionFilter : IExceptionFilter
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<JsonExceptionFilter> _logger;

        public JsonExceptionFilter(IWebHostEnvironment env, ILogger<JsonExceptionFilter> logger)
        {
            _env = env;
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            // Transform PostgreSQL errors into clear messages
            if (context.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx)
            {
                context.Exception = TransformPostgresException(pgEx);
            }
            else if (context.Exception is DbUpdateException dbEx2 && dbEx2.InnerException is SqlException sqlEx)
            {
                context.Exception = TransformSqlServerException(sqlEx);
            }

            var error = new ApiError();
            error.Status = GetStatusCodeFromException(context.Exception);

            if(error.Status == (int)HttpStatusCode.InternalServerError)
            {
                if (_env.IsDevelopment())
                {
                    error.Title = context.Exception.Message;
                    error.Detail = context.Exception.StackTrace;
                }
                else
                {
                    error.Title = "A server error occurred.";
                    error.Detail = context.Exception.Message;
                }
            }
            else
            {
                error.Title = context.Exception.Message;
            }

            context.Result = new JsonResult(error)
            {
                StatusCode = error.Status
            };
        }

        /// <summary>
        /// map all custom exceptions to proper http status code
        /// </summary>
        /// <returns></returns>
        private static int GetStatusCodeFromException(Exception exception)
        {
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

            if (exception is IApiException)
            {
                statusCode = (exception as IApiException).GetStatusCode();
            }

            return (int)statusCode;
        }

        /// <summary>
        /// Transform PostgreSQL exceptions into user-friendly messages.
        /// Logs detailed error information while returning generic messages to prevent
        /// exposing database internals to users.
        /// </summary>
        private Exception TransformPostgresException(PostgresException pgEx)
        {
            // Log detailed error for developers/ops
            _logger.LogError($"PostgreSQL {pgEx.SqlState}: Table={pgEx.TableName}, Constraint={pgEx.ConstraintName}, Message={pgEx.MessageText}");

            // Always return generic user-friendly messages
            return pgEx.SqlState switch
            {
                "23505" => // unique_violation
                    new InvalidOperationException("A record with this identifier already exists."),

                "23503" => // foreign_key_violation
                    new InvalidOperationException("Referenced entity does not exist. Please verify all referenced entities exist."),

                "23514" => // check_violation
                    new InvalidOperationException("Data validation failed."),

                _ => new InvalidOperationException("A database error occurred.")
            };
        }

        /// <summary>
        /// Transform SQL Server exceptions into user-friendly messages.
        /// Logs detailed error information while returning generic messages to prevent
        /// exposing database internals to users.
        /// </summary>
        private Exception TransformSqlServerException(SqlException sqlEx)
        {
            // Log detailed error for developers/ops
            _logger.LogError($"SQL Server Error {sqlEx.Number}: {sqlEx.Message}");

            // Always return generic user-friendly messages
            return sqlEx.Number switch
            {
                2601 or 2627 => // unique constraint violation
                    new InvalidOperationException("A record with this identifier already exists."),

                547 => // foreign key violation
                    new InvalidOperationException("Referenced entity does not exist. Please verify all referenced entities exist."),

                _ => new InvalidOperationException("A database error occurred.")
            };
        }
    }
}

