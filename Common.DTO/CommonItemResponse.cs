namespace Common.DTO
{
    // Restricted view for normal users
    public class CommonItemResponse
    {
        public string ItemId { get; set; } = string.Empty;
        public string OriginalId { get; set; } = string.Empty;
        public string SourceService { get; set; } = "Common";
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Price { get; set; }
    }

    // Full view for Admins (includes stock quantities)
    public class CommonItemAdminResponse
    {
        public string ItemId { get; set; } = string.Empty;
        public string OriginalId { get; set; } = string.Empty;
        public string SourceService { get; set; } = "Common";
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockQuantity { get; set; }
    }

    // Request DTO for creating/updating items
    public class CommonItemRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockQuantity { get; set; }
        public string SourceService { get; set; } = "Common";
    }
}
