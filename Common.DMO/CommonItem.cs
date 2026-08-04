using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Common.DMO
{
    public class CommonItem
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string OriginalId { get; set; } = string.Empty;
        public string SourceService { get; set; } = "Common";
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public double Price { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
