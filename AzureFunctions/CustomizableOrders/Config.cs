
namespace CustomizableOrders
{
    public record class Config
    {
        // HTTP Client Names
        public const string APITEMPLATE_CLIENT = "ApiTemplateHttp";
        public const string GOOGLE_API_CLIENT = "GoogleApiHttp";
        public const string GOOGLE_OAUTH_CLIENT = "GoogleOAuthHttp";

        // ApiTemplateIo
        public static readonly string ApiTemplateToken;
        public static readonly string SheetTemplateId;
        public static readonly string LabelTemplateId;
        public static readonly string ImageLabelTemplateId;

        // Google Secrets
        public static readonly string GoogleClientId;
        public static readonly string GoogleClientSecret;
        public static readonly string GoogleRefreshToken;

        // Printer Folder
        public static readonly string Printer1;
        public static readonly string Printer2;
        public static readonly string ImageLabelPrinter;
        public static readonly string EngravableLabelPrinter;

        public static readonly int BatchSize;
        public static readonly string ConnectionString;

        static Config()
        {
            // ApiTemplateIo
            ApiTemplateToken = GetValue("ApiTemplateToken");
            SheetTemplateId = GetValue("SheetTemplateId");
            LabelTemplateId = GetValue("LabelTemplateId");
            ImageLabelTemplateId = GetValue("ImageLabelTemplateId");

            // Google Secrets
            GoogleClientId = GetValue("GoogleClientId");
            GoogleClientSecret = GetValue("GoogleClientSecret");
            GoogleRefreshToken = GetValue("GoogleRefreshToken");

            // Printer Folder
            Printer1 = GetValue("Printer1");
            Printer2 = GetValue("Printer2");
            ImageLabelPrinter = GetValue("ImageLabelPrinter");
            EngravableLabelPrinter = GetValue("EngravableLabelPrinter");

            BatchSize = int.TryParse(GetValue("BatchSize"), out var value) ? value : 15;
            ConnectionString = GetValue("SQLCONNSTR_ConnectionString") ?? GetValue("ConnectionStrings:ConnectionString");
        }

        private static string GetValue(string key) => Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
    }
}
