using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using test_application_for_development_team;
using test_application_for_development_team.Services;

var builder = WebApplication.CreateBuilder(args);

var cultures = builder.Configuration.GetSection("Localization:SupportedCultures").Get<string[]>() ?? ["nl-NL", "en-US"];
var defaultCulture = builder.Configuration["Localization:DefaultCulture"] ?? cultures[0];

builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    var supported = cultures.Select(c => new CultureInfo(c)).ToList();
    o.DefaultRequestCulture = new RequestCulture(defaultCulture);
    o.SupportedCultures = supported;
    o.SupportedUICultures = supported;
    o.RequestCultureProviders = [new CookieRequestCultureProvider()];
});

builder.Services.AddRazorPages(o =>
    {
        o.Conventions.AllowAnonymousToFolder("/Account");
        o.Conventions.AuthorizeFolder("/Admin", PropertyAdmin.Contracts.Permissions.Users);
    })
    .AddMvcOptions(o => o.Filters.Add<ApiExceptionPageFilter>())
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(o => o.DataAnnotationLocalizerProvider = (_, f) => f.Create(typeof(SharedResource)));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/Account/AccessDenied";
        o.Cookie.Name = "PropertyAdmin.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        o.SlidingExpiration = false;
    });
builder.Services.AddAuthorization(FrontendPolicies.Configure);
builder.Services.AddAntiforgery(o =>
{
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SignInService>();
builder.Services.AddScoped<ApiExceptionPageFilter>();
builder.Services.AddTransient<AccessTokenHandler>();
builder.Services.AddHttpClient<ApiClient>(c =>
        c.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("Api:BaseUrl is not configured.")))
    .AddHttpMessageHandler<AccessTokenHandler>();

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        o.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
    }
    foreach (var network in builder.Configuration.GetSection("ForwardedHeaders:KnownIPNetworks").Get<string[]>() ?? [])
    {
        o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
    }
});

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();

public partial class Program;

