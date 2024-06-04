namespace CustomizableOrders.Models
{
    public class OrderCustom
    {
        public long Id { get; set; }
        public long? BatchId { get; set; }
        public Guid Guid { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string ImageUrl { get; set; }
        public OrderCustomStatus CustomStatusId { get; set; }
        public OrderCustomType CustomTypeId { get; set; }
        public Station CurrentStationId { get; set; }
        public Guid PrinterBatchGuid { get; set; }
        public string Barcode { get; set; }
        public string ItemHeader { get; set; }
        public string ItemDescription { get; set; }
        public string Attributes { get; set; }
        public int PrintCount { get; set; }
        public int PrinterNo { get; set; }
        public string BillingInfo { get; set; }
        public string ShippingInfo { get; set; }
        public string ShippingNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public string ShippingCountryCode { get; set; }
    }

    public record class ImageOrder
    {
        public long Id { get; set; }
        public Guid Guid { get; set; }
        public int PrinterNo { get; set; }
        public int PrintCount { get; set; }
        public string Barcode { get; set; }
        public string ItemDescription { get; set; }
        public string ImageUrl { get; set; }
        public OrderCustomStatus CustomStatusId { get; set; }
        public Guid PrinterBatchGuid { get; set; }
        public long? BatchId { get; set; }
        public bool Remake { get; set; }
        public string ItemHeader { get; set; }
    }

    public enum OrderCustomType
    {
        Image = 1,
        Tag = 2,
        Engrave = 3,
        Ring = 4
    }

    public enum OrderCustomStatus
    {
        New = 1,
        ForcePrint = 2,
        Reprint = 3,
        CopyRejected = 4,
        CopyApproved = 5,
        Printed = 6,
        LabelPrinted = 7
    }

    public enum Station
    {
        Station1 = 1,
        Station2 = 2,
        Station3 = 3,
        Station4 = 4,
        Station5 = 5
    }
}
