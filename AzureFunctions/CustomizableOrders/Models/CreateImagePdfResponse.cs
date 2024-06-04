namespace CustomizableOrders.Models
{
    public record class CreateImagePdfResponse(long BatchId, string DownloadUrl, List<long> OrderCustomIds);
}
