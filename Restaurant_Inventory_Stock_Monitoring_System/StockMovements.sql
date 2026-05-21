CREATE TABLE StockMovements (
    MovementId INT PRIMARY KEY IDENTITY(1,1),
    ItemId INT NOT NULL,
    MovementType NVARCHAR(10) NOT NULL,
    Quantity INT NOT NULL,
    MovementDate DATETIME NOT NULL DEFAULT GETDATE(),
    Remarks NVARCHAR(200),
    StaffName NVARCHAR(100),
    FOREIGN KEY (ItemId) REFERENCES InventoryItems(ItemId)
);