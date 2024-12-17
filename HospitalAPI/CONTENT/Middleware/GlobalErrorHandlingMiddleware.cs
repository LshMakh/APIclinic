using HospitalAPI.CONTENT.Models;
using Oracle.ManagedDataAccess.Client;
using System.Net;

namespace HospitalAPI.CONTENT.Middleware
{
    public class GlobalErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;

        public GlobalErrorHandlingMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                var response = context.Response;
                response.ContentType = "application/json";

                var errorResponse = new ApiErrorResponse();

                switch (error)
                {
                    case BusinessException e:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        errorResponse.ErrorCode = e.ErrorCode;
                        errorResponse.Message = e.Message;
                        break;

                    case KeyNotFoundException:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        errorResponse.ErrorCode = ErrorCodes.NotFound;
                        errorResponse.Message = "Requested resource not found";
                        break;

                    case UnauthorizedAccessException:
                        response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        errorResponse.ErrorCode = ErrorCodes.Unauthorized;
                        errorResponse.Message = "Unauthorized access";
                        break;

                    case OracleException e:
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        errorResponse.ErrorCode = ErrorCodes.DatabaseError;
                        errorResponse.Message = "Database operation failed";
                        _logger.LogError(e, "Database error occurred");
                        break;

                    default:
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        errorResponse.ErrorCode = ErrorCodes.GeneralError;
                        errorResponse.Message = "An internal error occurred";
                        _logger.LogError(error, "An unhandled exception occurred");
                        break;
                }

                await response.WriteAsJsonAsync(errorResponse);
            }
        }
    }

    
    public static class GlobalErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalErrorHandling(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalErrorHandlingMiddleware>();
        }
    }
}
