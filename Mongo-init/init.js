db = db.getSiblingDB('CommanDB');

db.createCollection('commanItems');

db.commanItems.insertMany([
  {
    Name: "General Packing Box",
    Category: "Packaging",
    StockQuantity: 150,
    Price: 12.5,
    CreatedAt: new Date()
  },
  {
    Name: "Cleaning Utility Set",
    Category: "Maintenance",
    StockQuantity: 80,
    Price: 45.0,
    CreatedAt: new Date()
  },
  {
    Name: "Standard Storage Crate",
    Category: "Storage",
    StockQuantity: 200,
    Price: 25.0,
    CreatedAt: new Date()
  }
]);
