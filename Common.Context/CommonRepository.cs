using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using Common.DMO;
using Common.Library.DTOs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Common.Context
{
    public class CommonRepository
    {
        private readonly IMongoCollection<CommonItem> _commonItems;
        private readonly string _dairyBaseUrl;
        private readonly string _groceryBaseUrl;
        private readonly string _stationaryBaseUrl;

        // Relative API Endpoint Paths
        private const string DairyProductsEndpoint = "/api/dairy/products";
        private const string GroceryProductsEndpoint = "/api/Grocery/products";
        private const string StationaryProductsEndpoint = "/api/stationary/products";

        private static MongoClient CreateClient(string connStr)
        {
            try
            {
                var settings = MongoClientSettings.FromConnectionString(connStr);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(1);
                settings.ConnectTimeout = TimeSpan.FromSeconds(1);
                settings.SocketTimeout = TimeSpan.FromSeconds(1);
                settings.SslSettings = new SslSettings
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                };
                return new MongoClient(settings);
            }
            catch
            {
                return new MongoClient(connStr);
            }
        }

        
        public CommonRepository(string connectionString, string databaseName, Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
        {
            var dairyUrl = configuration?["ServiceUrls:DairyService"];
            _dairyBaseUrl = !string.IsNullOrWhiteSpace(dairyUrl) ? dairyUrl : "http://localhost:8088";

            var groceryUrl = configuration?["ServiceUrls:GroceryService"];
            _groceryBaseUrl = !string.IsNullOrWhiteSpace(groceryUrl) ? groceryUrl : "http://localhost:8089";

            var stationaryUrl = configuration?["ServiceUrls:StationaryService"];
            _stationaryBaseUrl = !string.IsNullOrWhiteSpace(stationaryUrl) ? stationaryUrl : "http://localhost:8090";

            try
            {
                var client = CreateClient(connectionString);
                var database = client.GetDatabase(databaseName);
                _commonItems = database.GetCollection<CommonItem>("commonItems");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[COMMON REPO INIT WARNING] {ex.Message}");
            }
        }

        private static readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(3) };

        private static bool TryGetProp(System.Text.Json.JsonElement elem, string propName, out System.Text.Json.JsonElement val)
        {
            if (elem.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var prop in elem.EnumerateObject())
                {
                    if (prop.Name.Equals(propName, StringComparison.OrdinalIgnoreCase))
                    {
                        val = prop.Value;
                        return true;
                    }
                }
            }
            val = default;
            return false;
        }

        private static string GetString(System.Text.Json.JsonElement elem, string propName, string defaultVal = "")
        {
            if (TryGetProp(elem, propName, out var v))
            {
                if (v.ValueKind == System.Text.Json.JsonValueKind.String)
                    return v.GetString() ?? defaultVal;
                return v.ToString();
            }
            return defaultVal;
        }

        private static double GetDouble(System.Text.Json.JsonElement elem, string propName, double defaultVal = 0.0)
        {
            if (TryGetProp(elem, propName, out var v))
            {
                if (v.ValueKind == System.Text.Json.JsonValueKind.Number && v.TryGetDouble(out var d))
                    return d;
                if (v.ValueKind == System.Text.Json.JsonValueKind.String && double.TryParse(v.GetString(), out var parsed))
                    return parsed;
            }
            return defaultVal;
        }

        private static int GetInt(System.Text.Json.JsonElement elem, string propName, int defaultVal = 0)
        {
            if (TryGetProp(elem, propName, out var v))
            {
                if (v.ValueKind == System.Text.Json.JsonValueKind.Number && v.TryGetInt32(out var i))
                    return i;
                if (v.ValueKind == System.Text.Json.JsonValueKind.String && int.TryParse(v.GetString(), out var parsed))
                    return parsed;
            }
            return defaultVal;
        }

        public async Task<List<CommonItem>> GetAllItemsAsync()
        {
            var result = new List<CommonItem>();

            // 1. Fetch all items stored in _commonItems (CommonDB)
            if (_commonItems != null)
            {
                try
                {
                    using var cts = new System.Threading.CancellationTokenSource(1000);
                    var commonList = await _commonItems.Find(_ => true).ToListAsync(cts.Token);
                    if (commonList != null && commonList.Count > 0)
                    {
                        foreach (var item in commonList)
                        {
                            if (string.IsNullOrEmpty(item.SourceService)) item.SourceService = "Common";
                            result.Add(item);
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[COMMON FETCH ERR] {ex.Message}"); }
            }

            // 2. Fetch live products from sub-services via REST HTTP APIs
            await FetchSubServiceProductsOverHttpAsync(result);

            return result;
        }

        private async Task FetchSubServiceProductsOverHttpAsync(List<CommonItem> result)
        {
            // Dairy REST HTTP API Endpoint
            if (!string.IsNullOrEmpty(_dairyBaseUrl))
            {
                try
                {
                    var url = $"{_dairyBaseUrl.TrimEnd('/')}{DairyProductsEndpoint}";
                    var res = await _httpClient.GetAsync(url);
                    if (res.IsSuccessStatusCode)
                    {
                        var json = await res.Content.ReadAsStringAsync();
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            string name = GetString(elem, "name");
                            if (string.IsNullOrEmpty(name)) name = GetString(elem, "productName");
                            if (string.IsNullOrEmpty(name)) continue;
                            if (!result.Exists(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                            {
                                double fat = GetDouble(elem, "fatContent", 3.5);
                                if (fat == 0.0) fat = GetDouble(elem, "fatContentPercentage", 3.5);
                                int stock = GetInt(elem, "stockQuantity", 50);
                                if (stock == 0) stock = GetInt(elem, "quantity", 50);
                                double price = GetDouble(elem, "price", Math.Round(fat * 2.5, 2));
                                if (price == 0.0) price = Math.Round(fat * 2.5, 2);

                                result.Add(new CommonItem
                                {
                                    Id = ObjectId.GenerateNewId(),
                                    Name = name,
                                    Category = "Dairy",
                                    Price = price,
                                    StockQuantity = stock,
                                    SourceService = "Dairy"
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[COMMON REPO DAIRY FETCH NOTICE] Could not fetch live Dairy items: {ex.Message}");
                }
            }

            // Grocery REST HTTP API Endpoint
            if (!string.IsNullOrEmpty(_groceryBaseUrl))
            {
                try
                {
                    var url = $"{_groceryBaseUrl.TrimEnd('/')}{GroceryProductsEndpoint}";
                    var res = await _httpClient.GetAsync(url);
                    if (res.IsSuccessStatusCode)
                    {
                        var json = await res.Content.ReadAsStringAsync();
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            string name = GetString(elem, "name");
                            if (string.IsNullOrEmpty(name)) name = GetString(elem, "productName");
                            if (string.IsNullOrEmpty(name)) continue;
                            if (!result.Exists(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                            {
                                double price = GetDouble(elem, "price", 4.99);
                                int stock = GetInt(elem, "stockQuantity", 50);
                                if (stock == 0) stock = GetInt(elem, "quantity", 50);

                                result.Add(new CommonItem
                                {
                                    Id = ObjectId.GenerateNewId(),
                                    Name = name,
                                    Category = "Grocery",
                                    Price = price,
                                    StockQuantity = stock,
                                    SourceService = "Grocery"
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[COMMON REPO GROCERY FETCH NOTICE] Could not fetch live Grocery items: {ex.Message}");
                }
            }

            // Stationary REST HTTP API Endpoint
            if (!string.IsNullOrEmpty(_stationaryBaseUrl))
            {
                try
                {
                    var url = $"{_stationaryBaseUrl.TrimEnd('/')}{StationaryProductsEndpoint}";
                    var res = await _httpClient.GetAsync(url);
                    if (res.IsSuccessStatusCode)
                    {
                        var json = await res.Content.ReadAsStringAsync();
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            string name = GetString(elem, "name");
                            if (string.IsNullOrEmpty(name)) name = GetString(elem, "productName");
                            if (string.IsNullOrEmpty(name)) continue;
                            if (!result.Exists(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                            {
                                string cat = GetString(elem, "category", "Stationary");
                                if (string.IsNullOrEmpty(cat)) cat = "Stationary";
                                double price = GetDouble(elem, "price", 5.00);
                                int stock = GetInt(elem, "stockQuantity", 50);
                                if (stock == 0) stock = GetInt(elem, "quantity", 50);

                                result.Add(new CommonItem
                                {
                                    Id = ObjectId.GenerateNewId(),
                                    Name = name,
                                    Category = cat,
                                    Price = price,
                                    StockQuantity = stock,
                                    SourceService = "Stationary"
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[COMMON REPO STATIONARY FETCH NOTICE] Could not fetch live Stationary items: {ex.Message}");
                }
            }
        }

        public async Task<CommonItem> GetItemAsync(string id)
        {
            return await _commonItems.Find(p => p.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
        }

        public async Task AddItemAsync(CommonItem item)
        {
            if (string.IsNullOrEmpty(item.SourceService)) item.SourceService = "Common";
            await _commonItems.InsertOneAsync(item);
        }

        public async Task UpdateItemAsync(string id, CommonItem item)
        {
            item.Id = ObjectId.Parse(id);
            await _commonItems.ReplaceOneAsync(p => p.Id == item.Id, item);
        }

        public async Task DeleteItemAsync(string id)
        {
            await _commonItems.DeleteOneAsync(p => p.Id == ObjectId.Parse(id));
        }

        public async Task SyncProductAsync(ProductSyncPayload payload)
        {
            if (payload.ActionType == "Add")
            {
                var existing = await _commonItems.Find(x => (x.OriginalId == payload.OriginalId || x.Name.ToLower() == payload.Name.ToLower()) && x.SourceService == payload.SourceService).FirstOrDefaultAsync();
                if (existing == null)
                {
                    var item = new CommonItem
                    {
                        OriginalId = payload.OriginalId,
                        SourceService = payload.SourceService,
                        Name = payload.Name,
                        Category = payload.Category,
                        Price = (double)payload.Price,
                        StockQuantity = payload.StockQuantity
                    };
                    await _commonItems.InsertOneAsync(item);
                }
            }
            else if (payload.ActionType == "Update")
            {
                var existing = await _commonItems.Find(x => (x.OriginalId == payload.OriginalId || x.Name.ToLower() == payload.Name.ToLower()) && x.SourceService == payload.SourceService).FirstOrDefaultAsync();
                if (existing != null)
                {
                    existing.Name = payload.Name;
                    existing.Category = payload.Category;
                    existing.Price = (double)payload.Price;
                    existing.StockQuantity = payload.StockQuantity;
                    await _commonItems.ReplaceOneAsync(x => x.Id == existing.Id, existing);
                }
                else
                {
                    var item = new CommonItem
                    {
                        OriginalId = payload.OriginalId,
                        SourceService = payload.SourceService,
                        Name = payload.Name,
                        Category = payload.Category,
                        Price = (double)payload.Price,
                        StockQuantity = payload.StockQuantity
                    };
                    await _commonItems.InsertOneAsync(item);
                }
            }
            else if (payload.ActionType == "Delete")
            {
                await _commonItems.DeleteManyAsync(x => (x.OriginalId == payload.OriginalId || x.Name.ToLower() == payload.Name.ToLower()) && x.SourceService == payload.SourceService);
            }
        }
    }
}
