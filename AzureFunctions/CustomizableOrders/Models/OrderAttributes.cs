using System.Text.Json.Serialization;

namespace CustomizableOrders.Models
{
    public record class OrderAttributes
    {
        [JsonPropertyName("productionUrl")]
        public string ProductionUrl { get; set; }

        [JsonPropertyName("options")]
        public List<Option> Options { get; set; } = [];
    }
}
