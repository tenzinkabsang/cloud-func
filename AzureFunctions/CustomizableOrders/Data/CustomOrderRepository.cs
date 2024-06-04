using System.Data;
using CustomizableOrders.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CustomizableOrders.Data
{
    public class CustomOrderRepository(string connectionString) : ICustomOrderRepository
    {
        private readonly string _connectionString = connectionString;

        public async Task PopulateCustomOrdersAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync("usp_populate_custom_orders", commandType: CommandType.StoredProcedure);
        }

        public async Task AssignBatchesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync("usp_assign_batches_custom_orders", commandType: CommandType.StoredProcedure);
        }

        public async Task<IList<ImageOrder>> GetImageBatchesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return (await connection.QueryAsync<ImageOrder>("usp_get_image_custom_orders", commandType: CommandType.StoredProcedure)).ToList();
        }

        public async Task<IList<OrderCustom>> GetLabelsForPrintAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return (await connection.QueryAsync<OrderCustom>("usp_get_label_custom_orders", commandType: CommandType.StoredProcedure)).ToList();
        }

        public async Task<IList<ProductionUrlCheckModel>> GetItemsForProductionUrlCheckAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var items = await connection.QueryAsync<ProductionUrlCheckModel>(@"SELECT Id, ImageUrl FROM PL_OrderCustom WHERE CustomTypeId = 1 AND production_url_status = 0 AND CustomStatusId IN @CustomStatusIds ORDER BY Id",
                            new
                            {
                                CustomStatusIds = new List<OrderCustomStatus>
                                {
                                        OrderCustomStatus.New,
                                        OrderCustomStatus.CopyApproved
                                }
                            });

            return items.ToList();
        }

        public async Task UpdateProductionUrlStatusAsync(IList<long> customOrderIds)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(@"Update PL_OrderCustom SET production_url_status = 1 WHERE Id IN @Ids", new { Ids = customOrderIds });
        }

        public async Task UpdateBatchFileUrlAsync(IList<CreateImagePdfResponse> items)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(@"Update PL_OrderCustomBatch SET FileUrl = @FileUrl, PrintedDateUtc = @PrintedDateUtc where Id = @BatchId",
                items.Select(x =>
                new
                {
                    FileUrl = x.DownloadUrl,
                    PrintedDateUtc = DateTime.UtcNow,
                    BatchId = x.BatchId
                }).ToList());
        }

        public async Task UpdateImageOrderCustomAsync(IList<ImageOrder> imageOrders)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                    """
                    Update PL_OrderCustom SET CustomStatusId = @CustomStatusId,
                    PrintCount = @PrintCount, 
                    ItemHeader = @ItemHeader, 
                    UpdatedDateUtc = @UpdatedDateUtc 
                    Where Id = @Id
                    """,
                        imageOrders.Select(oc => new
                        {
                            Id = oc.Id,
                            CustomStatusId = oc.CustomStatusId,
                            PrintCount = oc.PrintCount,
                            ItemHeader = oc.ItemHeader,
                            UpdatedDateUtc = DateTime.UtcNow
                        }).ToList()
             );
        }

        public async Task UpdateLabelOrderStatusAsync(List<long> successfulIds, OrderCustomStatus orderCustomStatus)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                    """
                    Update PL_OrderCustom SET CustomStatusId = @OrderCustomStatusId, 
                    CurrentStationId = @StationId, 
                    LabelPrinted = 1,
                    UpdatedDateUtc = @UpdatedDateUtc 
                    WHERE Id IN @Ids
                    """,
                        new
                        {
                            Ids = successfulIds,
                            OrderCustomStatusId = orderCustomStatus,
                            StationId = Station.Station1,
                            UpdatedDateUtc = DateTime.UtcNow
                        });
        }

        public async Task AddStationHistoryRecordAsync(List<long> successfulIds)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                            """
                            Insert into PL_OrderCustomStationHistory (OrderCustomId, OrderStationId, CreatedDateUtc, StationUserName)
                            Values(@OrderCustomId, @OrderStationId, @CreatedDateUtc, @StationUserName)
                            """,
                            successfulIds.Select(id =>
                                   new
                                   {
                                       OrderCustomId = id,
                                       OrderStationId = Station.Station1,
                                       CreatedDateUtc = DateTime.UtcNow,
                                       StationUserName = "admin"
                                   }).ToList());
        }

        public async Task NewBatchHistoryRecordsAsync(List<OrderCustomBatchHistory> batchHistories)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                            """
                            INSERT INTO PL_OrderCustomBatchHistory (CustomStatusId, OrderCustomId, OrderCustomGuid, OrderBatchId, OrderBatchGuid, ImageUrl, Barcode, ItemHeader, ItemDescription, CreatedDateUtc)
                            VALUES (@CustomStatusId, @OrderCustomId, @OrderCustomGuid, @OrderBatchId, @OrderBatchGuid, @ImageUrl, @Barcode, @ItemHeader, @ItemDescription, @CreatedDateUtc)
                            """,
                        batchHistories);
        }

        public async Task<IList<OrderCustom>> GetAllLabelsForSheetAsync(Guid batchGuid)
        {
            using var connection = new SqlConnection(_connectionString);
            var orders = await connection.QueryAsync<OrderCustom>("usp_get_label_reprint_custom_orders", new { BatchGuid = batchGuid }, commandType: CommandType.StoredProcedure);
            return orders.ToList();
        }

        public async Task<OrderCustom> GetOrderCustomByIdAsync(long orderCustomId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstAsync<OrderCustom>("usp_get_label_reprint_custom_orders", new { Id = orderCustomId }, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> HealthCheckAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstAsync<int>("SELECT COUNT(1) FROM PL_OrderCustom");
        }
    }
}
