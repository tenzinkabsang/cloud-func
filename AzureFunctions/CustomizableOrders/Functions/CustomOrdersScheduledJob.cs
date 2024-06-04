using CustomizableOrders.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CustomizableOrders.Functions
{
    public class CustomOrdersScheduledJob(CustomOrderService customOrderService, ILoggerFactory loggerFactory)
    {
        private readonly CustomOrderService _customOrderService = customOrderService;
        private readonly ILogger _logger = loggerFactory.CreateLogger<CustomOrdersScheduledJob>();

        [Function(nameof(CustomOrdersScheduledJob))]
        public async Task RunAsync([TimerTrigger("%CustomOrdersSchedule%")] TimerInfo myTimer)
        {
            _logger.LogInformation("CustomOrdersScheduledJob Timer trigger function executed at: {Time}", DateTime.Now);

            await _customOrderService.PrintSheetsAndLabelsAsync();
            
            if (myTimer.ScheduleStatus is not null)
                _logger.LogInformation("Next timer schedule at: {Time}", myTimer.ScheduleStatus.Next);
        }
    }
}
