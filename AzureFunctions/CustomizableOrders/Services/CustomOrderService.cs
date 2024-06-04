using CustomizableOrders.Data;
using CustomizableOrders.Models;
using Microsoft.Extensions.Logging;

namespace CustomizableOrders.Services
{
    public class CustomOrderService(ICustomOrderRepository orderRepository,
        ImageSheetService imageSheetService,
        LabelService labelService,
        IHttpClientFactory httpClientFactory,
        ILogger<CustomOrderService> logger)
    {
        private readonly ICustomOrderRepository _orderRepository = orderRepository;
        private readonly ImageSheetService _imageSheetService = imageSheetService;
        private readonly LabelService _labelService = labelService;
        private readonly ILogger<CustomOrderService> _logger = logger;
        private readonly HttpClient _http = httpClientFactory.CreateClient();
        private const int CHUNK_SIZE = 30;

        public async Task PrintSheetsAndLabelsAsync()
        {
            await _orderRepository.PopulateCustomOrdersAsync();
            await PerformUrlCheckAsync();
            await _orderRepository.AssignBatchesAsync();
            await _imageSheetService.GenerateImageSheetsAsync(onlyFullBatches: true);
            await _labelService.GenerateLabelsAsync();
        }

        private async Task PerformUrlCheckAsync()
        {
            var items = await _orderRepository.GetItemsForProductionUrlCheckAsync();
            if (items.Count == 0)
                return;

            _logger.LogInformation("Performing production url status check.");

            foreach (var chunk in items.Chunk(CHUNK_SIZE))
            {
                var readyImages = await IsImageReadyAsync(chunk);
                if (readyImages.Count > 0)
                {
                    await _orderRepository.UpdateProductionUrlStatusAsync(readyImages);
                }
            }
        }

        private async Task<List<long>> IsImageReadyAsync(IList<ProductionUrlCheckModel> items)
        {
            List<long> readyImages = [];
            foreach (var item in items)
            {
                try
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Get, item.ImageUrl);
                    var result = await _http.SendAsync(requestMsg);
                    if (result.IsSuccessStatusCode)
                        readyImages.Add(item.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Production url check failed for {OrderCustomId}", item.Id);
                }
            }
            return readyImages;
        }
    }
}
