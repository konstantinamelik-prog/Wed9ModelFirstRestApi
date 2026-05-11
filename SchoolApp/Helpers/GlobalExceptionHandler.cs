using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SchoolApp.Exceptions;

namespace SchoolApp.Helpers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Είναι ένα defensive check: αν για κάποιο λόγο το response έχει ήδη αρχίσει να στέλνεται στον
            // client, δεν στέλνεται 2ο response με το error, απλά log-άρεται το πρόβλημα.
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response has already started; cannot write error response.");
                return false;
            }

            var (statusCode, title, isExpected) = MapException(exception);

            // κατηγοριοποίηση των exceptions σε δύο τύπους με βάση την προέλευσή τους
            // business rule violations που έχουμε προβλέψει
            // Και bugs ή system failures που δεν είχαμε προβλέψει
            // Εμπίπτουν στο false _ του MapException
            if (isExpected)
            {
                _logger.LogWarning(exception,
                    "Handled {ExceptionType} at {Method} {Endpoint} by {User} | Trace={TraceId}",
                    exception.GetType().Name,
                    context.Request.Method,
                    context.Request.Path,
                    context.User.Identity?.Name ?? "Anonymous",
                    context.TraceIdentifier);
            }
            else
            {
                _logger.LogError(exception,
                    "Unhandled exception at {Method} {Endpoint} by {User} | Trace={TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.User.Identity?.Name ?? "Anonymous",
                    context.TraceIdentifier);
            }

            // RFC 7807 (Problem Details) -- είναι το standard για REST API error responses στο .NET
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = isExpected
                    ? exception.Message
                    : "An unexpected error occurred. Please contact support.",
                Instance = context.Request.Path,
                Type = $"https://httpstatuses.io/{statusCode}"
            };

            // Προσθέτει ένα custom field στο JSON response
            // είναι ένα unique ID per request που παράγει αυτόματα το framework
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(
                problemDetails, cancellationToken);

            return true;
        }

        private static (int StatusCode, string Title, bool IsExpected) MapException(Exception ex) => ex switch
        {
            EntityAlreadyExistsException => (StatusCodes.Status409Conflict, "Resource already exists", true),
            EntityNotAuthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized", true),
            EntityForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", true),
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found", true),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error", false)
        };
    }
}