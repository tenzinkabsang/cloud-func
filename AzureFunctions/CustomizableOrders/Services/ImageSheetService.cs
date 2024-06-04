using System.Net.Http.Headers;
using System.Text.Json;
using CustomizableOrders.Data;
using CustomizableOrders.Models;
using Microsoft.Extensions.Logging;

namespace CustomizableOrders.Services
{
    public class ImageSheetService(ICustomOrderRepository orderRepository,
        GoogleService googleService,
        IHttpClientFactory httpClientFactory,
        ILogger<ImageSheetService> logger)
    {
        private readonly ICustomOrderRepository _orderRepository = orderRepository;
        private readonly GoogleService _googleService = googleService;
        private readonly HttpClient _apiTemplateClient = httpClientFactory.CreateClient(Config.APITEMPLATE_CLIENT);
        private readonly ILogger<ImageSheetService> _logger = logger;
        private const int CHUNK_SIZE = 2;

        public async Task GenerateImageSheetsAsync(bool onlyFullBatches)
        {
            var allItems = await _orderRepository.GetImageBatchesAsync();
            if (allItems.Count == 0 || (onlyFullBatches && allItems.Count < Config.BatchSize))
                return;

            _logger.LogInformation("Generating image sheets.");

            var sheets = GroupOrdersIntoBatches(allItems, onlyFullBatches);

            if (sheets.Count > 0)
            {
                // Printing in small batches and updating the status in order to reduce the
                // chances of reprinting sheets if the app crashes in the middle of processing.
                foreach (var chunk in sheets.Chunk(CHUNK_SIZE))
                {
                    var response = await CreateImagePdfsAsync(chunk);
                    await UpdateSheetRecordsAsync(allItems, response);
                }
            }
        }

        public async Task ReprintSheetAsync(ReprintSheetRequest sheetInfo)
        {
            string printer = sheetInfo.PrinterNo == 1 ? Config.Printer1 : Config.Printer2;
            await _googleService.SendPdfToFolderAsync(sheetInfo.FileUrl, sheetInfo.BatchGuid, printer, sheetInfo.BatchId);
        }

        private async Task<List<CreateImagePdfResponse>> CreateImagePdfsAsync(IList<ImageApiTemplatePdf> sheets)
        {
            var result = new List<CreateImagePdfResponse>();
            string path = $"create?template_id={Config.SheetTemplateId}";
            foreach (var sheet in sheets)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, path)
                    {
                        Content = new StringContent(sheet.ToJson(), new MediaTypeHeaderValue("application/json"))
                    };

                    var httpResponse = await _apiTemplateClient.SendAsync(request);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        // Successfuly generated pdf template.
                        using var stream = httpResponse.Content.ReadAsStream();
                        var response = await JsonSerializer.DeserializeAsync<ApiTemplateResponse>(stream);

                        // Send pdf to google drive
                        bool success = await _googleService.SendPdfToFolderAsync(response.DownloadUrl, sheet.BatchGuid, sheet.Printer, sheet.SheetNo);

                        // Pdf template sent to google drive for printing
                        if (success)
                            result.Add(new CreateImagePdfResponse(sheet.SheetNo, response.DownloadUrl, sheet.OrderCustomIds));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate image sheet for {BatchGuid}", sheet.BatchGuid);
                }
            }
            _logger.LogInformation("{TotalCount} batches successfully printed.", result.Count);

            return result;
        }

        private static List<ImageApiTemplatePdf> GroupOrdersIntoBatches(IList<ImageOrder> orders, bool onlyFullBatches)
        {
            return orders.GroupBy(x => new { x.BatchId, x.PrinterBatchGuid, x.PrinterNo }).ToDictionary(x => x.Key, x => x.ToList())
                .Where(x => !onlyFullBatches || x.Value.Count == Config.BatchSize)
                     .Select(x => new ImageApiTemplatePdf
                     {
                         SheetNo = x.Key.BatchId ?? 0,
                         BatchGuid = x.Key.PrinterBatchGuid,
                         Printer = x.Key.PrinterNo == 1 ? Config.Printer1 : Config.Printer2,
                         Items = x.Value.Select(oc =>
                             new ApiTemplateModel
                             {
                                 ItemNumber = $"[{oc.BatchId}] {oc.Guid}",
                                 DocumentId = oc.Barcode,
                                 ItemDescription = oc.ItemDescription,
                                 Image = oc.ImageUrl,
                                 Remake = oc.Remake
                             }).ToList(),
                         OrderCustomIds = x.Value.Select(x => x.Id).ToList()
                     }).ToList();
        }

        private async Task UpdateSheetRecordsAsync(IList<ImageOrder> items, List<CreateImagePdfResponse> response)
        {
            if (response.Count <= 0)
                return;

            var ids = response.SelectMany(r => r.OrderCustomIds).ToList();
            var printedItems = items.Where(oc => ids.Contains(oc.Id));

            var updateImageOrderTask = UpdateImageOrderCustomRecordAsync(printedItems);
            var insertBatchHistoryTask = InsertBatchHistoryRecordAsync(printedItems);
            var updateBatchFileUrlTask = _orderRepository.UpdateBatchFileUrlAsync(response);

            await Task.WhenAll(updateImageOrderTask, insertBatchHistoryTask, updateBatchFileUrlTask);
        }

        private async Task UpdateImageOrderCustomRecordAsync(IEnumerable<ImageOrder> printedItems)
        {
            var imageOrders = printedItems.Select(oc =>
                                        new ImageOrder
                                        {
                                            Id = oc.Id,
                                            CustomStatusId = OrderCustomStatus.Printed,
                                            PrintCount = oc.PrintCount + 1,
                                            ItemHeader = $"[{oc.BatchId}]{oc.Guid}"
                                        }).ToList();

            await _orderRepository.UpdateImageOrderCustomAsync(imageOrders);
        }

        private async Task InsertBatchHistoryRecordAsync(IEnumerable<ImageOrder> printedItems)
        {
            var batchHistories = printedItems.Select(oc =>
                                            new OrderCustomBatchHistory
                                            {
                                                CustomStatusId = OrderCustomStatus.Printed,
                                                OrderBatchId = oc.BatchId,
                                                OrderBatchGuid = oc.PrinterBatchGuid,
                                                ImageUrl = oc.ImageUrl,
                                                Barcode = oc.Barcode,
                                                ItemHeader = $"[{oc.BatchId}]{oc.Guid}",
                                                ItemDescription = oc.ItemDescription,
                                                CreatedDateUtc = DateTime.UtcNow,
                                                OrderCustomId = oc.Id,
                                                OrderCustomGuid = oc.Guid
                                            }).ToList();

            await _orderRepository.NewBatchHistoryRecordsAsync(batchHistories);
        }

    }
}
