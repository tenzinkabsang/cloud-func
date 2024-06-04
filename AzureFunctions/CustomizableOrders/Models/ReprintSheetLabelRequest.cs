namespace CustomizableOrders.Models
{
    public record class ReprintSheetLabelRequest(Guid BatchGuid);

    public record class ReprintSheetRequest(long BatchId, Guid BatchGuid, string FileUrl, int PrinterNo);

    public record class ReprintSingleLabelRequest(long OrderCustomId);
}
