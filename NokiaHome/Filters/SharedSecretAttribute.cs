using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using NokiaHome.Settings;

namespace NokiaHome.Filters
{
    public enum SharedSecretKind { Device, Sender }

    /// <summary>Requires a header matching a configured secret (constant-time compare); else 401.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class SharedSecretAttribute : Attribute, IAsyncActionFilter
    {
        private readonly SharedSecretKind _kind;

        public SharedSecretAttribute(SharedSecretKind kind) => _kind = kind;

        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var settings = context.HttpContext.RequestServices.GetRequiredService<IOptions<PrinterSettings>>().Value;
            var (header, expected) = _kind == SharedSecretKind.Device
                ? ("X-Device-Key", settings.DeviceKey)
                : ("X-Api-Key", settings.ApiKey);

            var provided = context.HttpContext.Request.Headers[header].ToString();
            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(provided) || !FixedEquals(provided, expected))
            {
                context.Result = new UnauthorizedResult();
                return Task.CompletedTask;
            }
            return next();
        }

        public static bool FixedEquals(string a, string b)
        {
            // Hash first so lengths are equal and don't leak.
            var ha = SHA256.HashData(Encoding.UTF8.GetBytes(a));
            var hb = SHA256.HashData(Encoding.UTF8.GetBytes(b));
            return CryptographicOperations.FixedTimeEquals(ha, hb);
        }
    }
}
