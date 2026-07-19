
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WalletService.Api
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
              //.AddJsonOptions(o =>
              //{
              //    //o.AllowInputFormatterExceptionMessages = false;
              //});


            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

                    var problem = factory.CreateValidationProblemDetails(
                        context.HttpContext,
                        context.ModelState,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Solicitud inválida",
                        detail: "Solicitud inválida",
                        instance: context.HttpContext.Request.Path);

                    // Sanitiza errores (quita detalles técnicos)
                    var sanitized = new Dictionary<string, string[]>();
                    foreach (var kv in problem.Errors)
                    {
                        var key = kv.Key.StartsWith("$.") ? kv.Key[2..] : kv.Key; // "$.documentType" -> "documentType"

                        var messages = kv.Value
                            .Select(m => m.Contains("could not be converted", StringComparison.OrdinalIgnoreCase)
                                ? "Formato inválido."
                                : m)
                            .ToArray();

                        sanitized[key] = messages;
                    }

                    problem.Errors.Clear();
                    foreach (var kv in sanitized)
                        problem.Errors.Add(kv.Key, kv.Value);

                    return new BadRequestObjectResult(problem);
                };

                //Evita que el framework mande automáticamente un 400 Bad Request con detalles de validación

                //options.InvalidModelStateResponseFactory = context =>
                //{
                //    var problemDetails = new ValidationProblemDetails(context.ModelState)
                //    {
                //        Status = StatusCodes.Status422UnprocessableEntity,
                //        Type = "https://httpstatuses.com/422",
                //        Title = "One or more validation errors occurred.",
                //        Detail = "See the errors property for details.",
                //        Instance = context.HttpContext.Request.Path
                //    };
                //    return new UnprocessableEntityObjectResult(problemDetails)
                //    {
                //        ContentTypes = { "application/problem+json", "application/problem+xml" }
                //    };
                //};
            });


          
            // 1. Permite que el sistema encuentre los endpoints (especialmente Minimal APIs)
            services.AddEndpointsApiExplorer();

            // 2. Genera el documento OpenAPI formal (reemplaza a AddSwaggerGen) - openapi/v1.json
            services.AddOpenApi();


            // ProblemDetails (incluye IProblemDetailsService)
            services.AddProblemDetails(options =>
            {
                // Nunca mandes detalles internos al cliente
                //options.IncludeExceptionDetails = (ctx, ex) => false;

                options.CustomizeProblemDetails = ctx =>
                {
                    ctx.ProblemDetails.Extensions["correlationId"] = ctx.HttpContext.TraceIdentifier;
                    ctx.ProblemDetails.Extensions.Remove("exception");
                    ctx.ProblemDetails.Extensions.Remove("headers");
                    ctx.ProblemDetails.Extensions.Remove("endpoint");
                    ctx.ProblemDetails.Extensions.Remove("routeValues");
                    ctx.ProblemDetails.Extensions.Remove("path");
                };
            });

            // Excepciones inesperadas => ProblemDetails consistente
            services.AddExceptionHandler<GlobalExceptionHandler>();

            // ── JWT Authentication (Entra ID) ──────────────────────────────────────
            var tenantId = configuration["AzureAd:TenantId"]
                ?? throw new InvalidOperationException("AzureAd:TenantId no está configurado.");
            var clientId = configuration["AzureAd:ClientId"]
                ?? throw new InvalidOperationException("AzureAd:ClientId no está configurado.");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Authority           = $"https://login.microsoftonline.com/{tenantId}/v2.0";
                        options.Audience            = clientId;
                        options.MapInboundClaims    = false;
                        options.TokenValidationParameters.RoleClaimType = "roles";
                    });

            services.AddAuthorization();

            return services;

        }
    }
}
