using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using WebstrarPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Allow larger ZIP uploads (adjust if needed)
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 1024L * 1024L * 1024L; // 1 GB
});

// IIS in-process hosting limit
builder.Services.Configure<IISServerOptions>(o =>
{
    o.MaxRequestBodySize = 1024L * 1024L * 1024L; // 1 GB
});

// Kestrel limit (covers out-of-process)
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 1024L * 1024L * 1024L; // 1 GB
});

// ── CAS SSO (disabled for now — uncomment when CAS app is registered) ──
// builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//     .AddCookie(options =>
//     {
//         options.LoginPath = "/account/login";
//         options.LogoutPath = "/account/logout";
//         options.AccessDeniedPath = "/account/access-denied";
//         options.Cookie.Name = "WebstrarAuth";
//         options.Cookie.HttpOnly = true;
//         options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
//         options.Cookie.SameSite = SameSiteMode.Lax;
//         options.ExpireTimeSpan = TimeSpan.FromHours(8);
//         options.SlidingExpiration = true;
//     });
// builder.Services.AddHttpClient<CasTicketValidator>();

// AWS region from appsettings.json ("AWS": { "Region": "us-west-2" })
var awsOptions = builder.Configuration.GetAWSOptions();
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonDynamoDB>();

// Services
builder.Services.AddSingleton(sp =>
    new DynamoDbService(
        sp.GetRequiredService<IAmazonDynamoDB>(),
        "WebstrarUsers"
    )
);

builder.Services.AddSingleton<ZipDeployService>();
builder.Services.AddSingleton<PageStatusService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
