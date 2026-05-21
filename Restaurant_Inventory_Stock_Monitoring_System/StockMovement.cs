using System;

public class StockMovement
{
    public int MovementId { get; set; }
    public int ItemId { get; set; }
    public string MovementType { get; set; }
    public int Quantity { get; set; }
    public DateTime MovementDate { get; set; }
    public string Remarks { get; set; }
    public string StaffName { get; set; }
    public InventoryItem Item { get; set; }
}