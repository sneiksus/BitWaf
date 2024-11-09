using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/listing", (string folder) =>
{
    var files = Directory.GetFiles($"./{folder}");
    return string.Join(", ", files);
});

app.MapPost("/login", (HttpContext context, string username, string password) =>
{
    if (username == "admin" && password == "admin")
    {
        context.Response.Cookies.Append("AuthCookie", "authenticated", new CookieOptions
        {
            HttpOnly = true, 
            Secure = true,   
            SameSite = SameSiteMode.Strict, 
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return "Login successful!";
    }

    return "Invalid credentials!";
});

app.MapGet("/find", async (IConfiguration config, string login) =>
{
    var client = new MongoDB.Driver.MongoClient(config.GetConnectionString("CS"));
    var database = client.GetDatabase("Bit");
    var collection = database.GetCollection<BsonDocument>("Users");

    var filter = Builders<BsonDocument>.Filter.Eq("login", login);
    var result = await collection.Find(filter).FirstOrDefaultAsync();

    if (result == null)
    {
        return Results.NotFound();
    }

    var response = new Dictionary<string, object>
    {
        ["_id"] = result["_id"].AsObjectId.ToString()
    };

    foreach (var element in result.Elements)
    {
        if (element.Name != "_id")
        {
            response[element.Name] = element.Value.ToString();
        }
    }

    return Results.Ok(response);
});

app.Run();
