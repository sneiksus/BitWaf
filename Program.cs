using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.AddMvc(options =>
{
    options.Filters.Remove(new AutoValidateAntiforgeryTokenAttribute());
});
var app = builder.Build();

app.UseStaticFiles();
app.UseDefaultFiles();

app.MapGet("/", () => 
    Results.Content(
        "<html><body>Hello! <a href='/login.html'>Go to Login</a></body></html>", 
        "text/html"
    )
);

app.MapGet("/listing", (HttpContext context, string folder) =>
{
    if (!context.Request.Cookies.TryGetValue("AuthCookie", out var authCookie) || authCookie != "authenticated")
    {
        return Results.Unauthorized();
    }

    var files = Directory.GetFiles($"./{folder}");
    return Results.Ok(string.Join(", ", files));
});

app.MapPost("/login", async (HttpContext context, [FromBody] LoginRequest login, IConfiguration config) =>
{
    var client = new MongoClient(config.GetConnectionString("CS"));
    var database = client.GetDatabase("Bit");
    var collection = database.GetCollection<BsonDocument>("Users");

    var filterJson = $"{{ 'login': '{login.Username}', 'password': '{login.Password}' }}";
    var filter = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(filterJson);

    var result = await collection.Find(filter).FirstOrDefaultAsync();

    if (result != null)
    {
        context.Response.Cookies.Append("AuthCookie", "authenticated", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return Results.Ok("Login successful!");
    }

    return Results.Unauthorized();
});

app.Run();

