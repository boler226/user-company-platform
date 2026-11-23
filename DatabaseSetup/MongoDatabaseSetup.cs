using MongoDB.Bson;
using MongoDB.Driver;

namespace DatabaseSetup;

internal sealed class MongoDatabaseSetup(string connectionString, string password)
{
    public async Task InitializeAsync()
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("lab01_notifications");

        await ConfigureCollectionsAsync(database);
        await SeedAsync(database);
        await CreateIndexesAsync(database);
        await ConfigureLoginAsync(database);

        Console.WriteLine("Ready: lab01_notifications");
    }

    private static async Task ConfigureCollectionsAsync(IMongoDatabase database)
    {
        var schemas = await ReadJsonAsync("01-collections.json");
        using var cursor = await database.ListCollectionNamesAsync();
        var existingCollections = (await cursor.ToListAsync()).ToHashSet();

        foreach (var schema in schemas)
        {
            var operation = existingCollections.Contains(schema.Name) ? "collMod" : "create";
            var command = new BsonDocument(operation, schema.Name)
            {
                { "validator", new BsonDocument("$jsonSchema", schema.Value) },
                { "validationLevel", "strict" },
                { "validationAction", "error" }
            };

            await database.RunCommandAsync<BsonDocument>(command);
        }
    }

    private static async Task SeedAsync(IMongoDatabase database)
    {
        var fixtures = await ReadJsonAsync("03-seed.json");

        foreach (var fixture in fixtures)
        {
            var collection = database.GetCollection<BsonDocument>(fixture.Name);

            foreach (var value in fixture.Value.AsBsonArray)
            {
                var document = value.AsBsonDocument;

                await collection.UpdateOneAsync(
                    new BsonDocument("_id", document["_id"]),
                    new BsonDocument("$setOnInsert", document),
                    new UpdateOptions { IsUpsert = true });
            }
        }
    }

    private static async Task CreateIndexesAsync(IMongoDatabase database)
    {
        var templateKeys = new BsonDocument
        {
            { "code", 1 },
            { "channel", 1 }
        };
        await CreateIndexAsync(database, "templates", templateKeys, unique: true);

        var inboxKeys = new BsonDocument
        {
            { "recipient.userId", 1 },
            { "createdAt", -1 },
            { "_id", 1 }
        };
        await CreateIndexAsync(database, "notifications", inboxKeys);

        var attemptKeys = new BsonDocument
        {
            { "notificationId", 1 },
            { "number", 1 }
        };
        await CreateIndexAsync(database, "deliveryAttempts", attemptKeys, unique: true);

        var pendingKeys = new BsonDocument
        {
            { "status", 1 },
            { "scheduledAt", 1 }
        };
        var pendingOptions = new CreateIndexOptions<BsonDocument>
        {
            PartialFilterExpression = new BsonDocument("status", "Pending")
        };
        var pendingIndex = new CreateIndexModel<BsonDocument>(pendingKeys, pendingOptions);

        await database.GetCollection<BsonDocument>("notifications")
            .Indexes.CreateOneAsync(pendingIndex);
    }

    private async Task ConfigureLoginAsync(IMongoDatabase database)
    {
        const string username = "lab01_notifications_app";
        var userInfo = await database.RunCommandAsync<BsonDocument>(
            new BsonDocument("usersInfo", username));

        var operation = userInfo["users"].AsBsonArray.Count == 0 ? "createUser" : "updateUser";
        var role = new BsonDocument
        {
            { "role", "readWrite" },
            { "db", "lab01_notifications" }
        };
        var command = new BsonDocument(operation, username)
        {
            { "pwd", password },
            { "roles", new BsonArray
                {
                    role,
                    new BsonDocument { { "role", "readWrite" }, { "db", "lab04_notifications" } }
                }
            }
        };

        await database.RunCommandAsync<BsonDocument>(command);
    }

    private static async Task CreateIndexAsync(
        IMongoDatabase database, string collection, BsonDocument keys, bool unique = false)
    {
        var options = new CreateIndexOptions { Unique = unique };
        var index = new CreateIndexModel<BsonDocument>(keys, options);

        await database.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(index);
    }

    private static async Task<BsonDocument> ReadJsonAsync(string filename)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "scripts", "notifications", filename);
        return BsonDocument.Parse(await File.ReadAllTextAsync(path));
    }
}
