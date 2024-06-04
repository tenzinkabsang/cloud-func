using System.Net;
using CustomizableOrders.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CustomizableOrders.Services;
using CustomizableOrders.Data;

namespace CustomizableOrders.Functions
{
    public class ReprintApi(LabelService labelService, 
        ImageSheetService imageSheetService,
        ICustomOrderRepository customOrderRepository,
        ILoggerFactory loggerFactory)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<ReprintApi>();
        private readonly LabelService _labelService = labelService;
        private readonly ImageSheetService _imageSheetService = imageSheetService;
        private readonly ICustomOrderRepository _customOrderRepository = customOrderRepository;

        [Function("HealthCheck")]
        public async Task<HttpResponseData> HealthCheckAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "health")] HttpRequestData req)
        {
            int count = await _customOrderRepository.HealthCheckAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString($"Count: {count}");
            return response;
        }

        [Function("ReprintSingleLabel")]
        public async Task<HttpResponseData> ReprintSingleLabelAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "label")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var singleLabelRequest = body.FromJson<ReprintSingleLabelRequest>();
            
            _logger.LogInformation("Attempting to reprint single label for OrderCustomId: {OrderCustomId}", singleLabelRequest.OrderCustomId);

            await _labelService.ReprintSingleLabelAsync(singleLabelRequest);
            
            return req.CreateResponse(HttpStatusCode.Accepted);
        }


        [Function("ReprintAllLabelsForSheet")]
        public async Task<HttpResponseData> ReprintAllLabelsForSheetAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "sheet/labels")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var batch = body.FromJson<ReprintSheetLabelRequest>();
            
            _logger.LogInformation("Attempting to reprint all labels for Batch: {BatchGuid}", batch.BatchGuid);

            _ = Task.Run(async () => await _labelService.ReprintAllLabelsForSheetAsync(batch));
            
            return req.CreateResponse(HttpStatusCode.Accepted);
        }

        [Function("ReprintSheet")]
        public async Task<HttpResponseData> ReprintSheetAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "sheet")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var sheetInfo = body.FromJson<ReprintSheetRequest>();

            _logger.LogInformation("Attempting to reprint Sheet # {SheetId}", sheetInfo.BatchId);

            await _imageSheetService.ReprintSheetAsync(sheetInfo);

            return req.CreateResponse(HttpStatusCode.Accepted);
        }

        [Function("ForcePrintRemainingItems")]
        public Task<HttpResponseData> ForcePrintRemainingItems([HttpTrigger(AuthorizationLevel.Function, "post", Route = "sheet/all")] HttpRequestData req)
        {
            _logger.LogInformation("Attempting to reprint all sheets and labels.");

            _ = Task.Run(async () =>
            {
                await _customOrderRepository.AssignBatchesAsync();
                await _imageSheetService.GenerateImageSheetsAsync(onlyFullBatches: false);
                await _labelService.GenerateLabelsAsync();
            });

            return Task.FromResult(req.CreateResponse(HttpStatusCode.Accepted));
        }
    }
}
