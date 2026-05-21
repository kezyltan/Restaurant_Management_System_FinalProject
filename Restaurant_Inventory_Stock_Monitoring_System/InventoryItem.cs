using System;

public class InventoryItem
{
    public int ItemId { get; set; }
    public string ItemName { get; set; }
    public string Category { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; }
    public int ReorderLevel { get; set; }
    public string SupplierName { get; set; }
    public DateTime LastRestocked { get; set; }
    public string Status { get; set; }
}