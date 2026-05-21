CREATE TABLE InventoryItems (
    ItemId INT PRIMARY KEY IDENTITY(1,1),
    ItemName NVARCHAR(100) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    Quantity INT NOT NULL DEFAULT 0,
    Unit NVARCHAR(20) NOT NULL,
    ReorderLevel INT NOT NULL DEFAULT 10,
    SupplierName NVARCHAR(100),
    LastRestocked DATETIME,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active'
);