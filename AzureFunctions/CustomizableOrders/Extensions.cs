using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace CustomizableOrders
{
    public static class Extensions
    {
        public static string ToJson<T>(this T log) => JsonSerializer.Serialize(log, _options);

        public static T FromJson<T>(this string value) => JsonSerializer.Deserialize<T>(value, _options);

        private static readonly JsonSerializerOptions _options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static void AddApiTemplateIoHttpClient(this IServiceCollection service)
        {
            service.AddHttpClient(Config.APITEMPLATE_CLIENT, c =>
            {
                c.BaseAddress = new Uri("https://api.apitemplate.io/v1/");
                c.DefaultRequestHeaders.Add("X-API-KEY", $"{Config.ApiTemplateToken}");
            });
        }

        public static void AddGoogleOAuthHttpClient(this IServiceCollection service)
        {
            service.AddHttpClient(Config.GOOGLE_OAUTH_CLIENT, c =>
            {
                c.BaseAddress = new Uri("https://www.googleapis.com/oauth2/v4/");
            });
        }

        public static void AddGoogleApiHttpClient(this IServiceCollection service)
        {
            service.AddHttpClient(Config.GOOGLE_API_CLIENT, c =>
            {
                c.BaseAddress = new Uri("https://script.google.com/macros/s/AKfycbzcQcG5xb0qQ_HwfoPs57rIbZeyI85O2-LnoJeO8CLZ5mfaDuA9vEciemP/");
            });
        }
    }
}
