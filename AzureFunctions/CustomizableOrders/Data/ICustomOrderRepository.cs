using CustomizableOrders.Models;

namespace CustomizableOrders.Data
{
    public interface ICustomOrderRepository : IImageOrderRepository, ILabelRepository
    {
        Task<IList<ProductionUrlCheckModel>> GetItemsForProductionUrlCheckAsync();
        Task UpdateProductionUrlStatusAsync(IList<long> orderCustomIds);
        Task AssignBatchesAsync();
        Task PopulateCustomOrdersAsync();
        Task<int> HealthCheckAsync();
    }

    public interface IImageOrderRepository
    {
        Task<IList<ImageOrder>> GetImageBatchesAsync();
        Task UpdateBatchFileUrlAsync(IList<CreateImagePdfResponse> items);
        Task UpdateImageOrderCustomAsync(IList<ImageOrder> orderCustoms);
        Task NewBatchHistoryRecordsAsync(List<OrderCustomBatchHistory> batchHistories);
    }

    public interface ILabelRepository
    {
        Task<IList<OrderCustom>> GetLabelsForPrintAsync();
        Task UpdateLabelOrderStatusAsync(List<long> successfulIds, OrderCustomStatus orderCustomStatus);
        Task<IList<OrderCustom>> GetAllLabelsForSheetAsync(Guid batchGuid);
        Task AddStationHistoryRecordAsync(List<long> successfulIds);
        Task<OrderCustom> GetOrderCustomByIdAsync(long orderCustomId);
    }
}
