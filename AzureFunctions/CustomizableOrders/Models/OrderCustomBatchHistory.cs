namespace CustomizableOrders.Models
{
    public class OrderCustomBatchHistory
    {
        public long Id { get; set; }
        public OrderCustomStatus CustomStatusId { get; set; }
        public long OrderCustomId { get; set; }
        public Guid OrderCustomGuid { get; set; }
        public long? OrderBatchId { get; set; }
        public Guid OrderBatchGuid { get; set; }
        public string ImageUrl { get; set; }
        public string Barcode { get; set; }
        public string ItemHeader { get; set; }
        public string ItemDescription { get; set; }
        public DateTime CreatedDateUtc { get; set; }
    }
}
