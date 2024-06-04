using System.Text.Json.Serialization;

namespace CustomizableOrders.Models
{
    public record class ImageApiTemplatePdf
    {
        public long SheetNo { get; set; }
        public Guid BatchGuid { get; set; }
        public List<ApiTemplateModel> Items { get; set; }

        [JsonIgnore]
        public string Printer { get; set; }
        [JsonIgnore]
        public List<long> OrderCustomIds { get; set; }
    }

    public record class ApiTemplateModel
    {
        [JsonPropertyName("item_no")]
        public string ItemNumber { get; set; }

        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; }

        public string ItemDescription { get; set; }

        public string Image { get; set; }

        public bool Remake { get; set; }
    }


    public record class LabelApiTemplatePdf
    {
        [JsonPropertyName("production_Url")]
        public string ProductionUrl { get; set; }
        
        public List<Option> Options { get; set; }

        [JsonPropertyName("textoptions")]
        public List<Option> TextOptions { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string Barcode { get; set; }
        public string BillingInfo { get; set; }
        public string ShippingInfo { get; set; }
        public string ItemDescription { get; set; }
        public string ShippingNumber { get; set; }
        public long? SheetId { get; set; }

        public string OrderDate { get; set; }
        public string ShippingCountryCode { get; set; }
        public string Sku { get; set; }

        [JsonIgnore]
        public string Printer { get; set; }
        [JsonIgnore]
        public Guid OrderCustomGuid { get; set; }
        [JsonIgnore]
        public long OrderCustomId { get; set; }
        [JsonIgnore]
        public string TemplateId { get; set; }

    }

    public record class Option
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("encoded_value")]
        public string EncodedValue { get; set; }
    }

    public record class ApiTemplateResponse
    {
        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; }

        [JsonPropertyName("template_id")]
        public string TemplateId { get; set; }

        [JsonPropertyName("transaction_ref")]
        public string TransactionRef { get; set; }

        public string Status { get; set; }
    }

    public record class OAuthResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
    }
}
