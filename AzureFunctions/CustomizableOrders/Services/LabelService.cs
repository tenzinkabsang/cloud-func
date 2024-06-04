using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using CustomizableOrders.Data;
using CustomizableOrders.Models;
using Microsoft.Extensions.Logging;

namespace CustomizableOrders.Services
{
    public sealed class LabelService(ICustomOrderRepository orderRepository,
        GoogleService googleService,
        IHttpClientFactory httpClientFactory,
        ILogger<LabelService> logger)
    {
        private readonly ICustomOrderRepository _orderRepository = orderRepository;
        private readonly GoogleService _googleService = googleService;
        private readonly HttpClient _apiTemplateClient = httpClientFactory.CreateClient(Config.APITEMPLATE_CLIENT);
        private readonly ILogger<LabelService> _logger = logger;
        private const int CHUNK_SIZE = 2;

        public async Task GenerateLabelsAsync()
        {
            var items = await _orderRepository.GetLabelsForPrintAsync();
            if (items.Count == 0)
                return;

            _logger.LogInformation("Generating labels for {LabelCount} items", items.Count);

            var labels = BuildTemplateData(items);

            // Printing in small batches and updating the status in order to reduce the
            // chances of reprinting labels if the app crashes in the middle of processing.
            foreach (var chunk in labels.Chunk(CHUNK_SIZE))
            {
                var successfulIds = await CreateLabelPdfsAsync(chunk);
                if (successfulIds.Count > 0)
                {
                    var updateStatusTask = _orderRepository.UpdateLabelOrderStatusAsync(successfulIds, OrderCustomStatus.LabelPrinted);
                    var addStationHistoryTask = _orderRepository.AddStationHistoryRecordAsync(successfulIds);
                    await Task.WhenAll(updateStatusTask, addStationHistoryTask);
                }
            }
        }

        public async Task ReprintAllLabelsForSheetAsync(ReprintSheetLabelRequest request)
        {
            var items = await _orderRepository.GetAllLabelsForSheetAsync(request.BatchGuid);
            await ReprintLabelsAsync(items);
        }

        public async Task ReprintSingleLabelAsync(ReprintSingleLabelRequest request)
        {
            var item = await _orderRepository.GetOrderCustomByIdAsync(request.OrderCustomId);
            await ReprintLabelsAsync(new List<OrderCustom> { item });
        }

        private async Task ReprintLabelsAsync(IList<OrderCustom> items)
        {
            if (!items.Any())
                return;

            _logger.LogInformation("Reprinting labels for {LabelCount} item(s)", items.Count);

            var labels = BuildTemplateData(items);
            var successfulIds = await CreateLabelPdfsAsync(labels);
            if (successfulIds.Count > 0)
            {
                await _orderRepository.UpdateLabelOrderStatusAsync(successfulIds, OrderCustomStatus.Reprint);
            }
        }

        private async Task<List<long>> CreateLabelPdfsAsync(IList<LabelApiTemplatePdf> labels)
        {
            var result = new List<long>();
            foreach (var label in labels)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"create?template_id={label.TemplateId}")
                    {
                        Content = new StringContent(label.ToJson(), new MediaTypeHeaderValue("application/json"))
                    };

                    var httpResponse = await _apiTemplateClient.SendAsync(request);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        // Successfuly generated pdf template.
                        using var stream = httpResponse.Content.ReadAsStream();
                        var response = await JsonSerializer.DeserializeAsync<ApiTemplateResponse>(stream);

                        // Send pdf to google drive
                        bool success = await _googleService.SendPdfToFolderAsync(response.DownloadUrl, label.OrderCustomGuid, label.Printer);

                        // Pdf template sent to google drive for printing
                        if (success)
                            result.Add(label.OrderCustomId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate label for {OrderCustomId}", label.OrderCustomId);
                }
            }
            _logger.LogInformation("{Count} labels successfully printed. [{OrderCustomId}]", result.Count, string.Join(", ", result.Select(r => r)));
            return result;
        }

        private IList<LabelApiTemplatePdf> BuildTemplateData(IList<OrderCustom> items)
        {
            return items
                .Where(oc => !string.IsNullOrEmpty(oc.Attributes) && !string.IsNullOrEmpty(oc.ItemDescription))
                .Select(oc =>
                    {
                        var attributes = TryParseAttributes(oc.Id, oc.Attributes);
                        var description = oc.ItemDescription.Split(" ");

                        return new LabelApiTemplatePdf
                        {
                            ProductionUrl = oc.ImageUrl ?? attributes.ProductionUrl,
                            Options = GetOptions(attributes.Options, ["dropdown", "swatch"]),
                            TextOptions = GetOptions(attributes.Options, ["text input"]),
                            OrderId = oc.OrderId,
                            CustomerId = oc.CustomerId,
                            Barcode = oc.Barcode,
                            BillingInfo = oc.BillingInfo,
                            ShippingInfo = oc.ShippingInfo,
                            Sku = description.FirstOrDefault(),
                            ItemDescription = string.Join(" ", description.Skip(1)),
                            ShippingNumber = oc.ShippingNumber,
                            SheetId = oc.BatchId,
                            Printer = oc.CustomTypeId == OrderCustomType.Engrave ? Config.EngravableLabelPrinter : Config.ImageLabelPrinter,
                            TemplateId = oc.CustomTypeId == OrderCustomType.Engrave ? Config.LabelTemplateId : Config.ImageLabelTemplateId,
                            OrderCustomGuid = oc.Guid,
                            OrderCustomId = oc.Id,
                            OrderDate = oc.OrderDate?.ToString("MM/dd/yy"),
                            ShippingCountryCode = oc.ShippingCountryCode,
                        };
                    }).ToList();
        }

        private static List<Option> GetOptions(List<Option> allOptions, List<string> optionTypes)
        {
            return allOptions
                    .Where(op => optionTypes.Contains(op.Type.Trim().ToLower()) && !string.IsNullOrWhiteSpace(op.Value))
                    .Select(op =>
                        new Option
                        {
                            Name = op.Name,
                            Type = op.Type,
                            Value = op.Value,
                            EncodedValue = HttpUtility.UrlEncode(op.Value)
                        })
                    .ToList();
        }

        private OrderAttributes TryParseAttributes(long orderCustomId, string attributes)
        {
            try
            {
                return JsonSerializer.Deserialize<OrderAttributes>(attributes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to parse attributes for {OrderCustomId}", orderCustomId);
            }
            return new OrderAttributes();
        }

    }

}
