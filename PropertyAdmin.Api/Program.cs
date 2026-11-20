using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Data;
using PropertyAdmin.Logic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogic(builder.Configuration.GetConnectionString("Default"));

builder.Services.AddIdentityCore<ApplicationUser>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 10;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        o.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddDataStores()
    .AddDefaultTokenProviders();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>>((o, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = TokenService.CreateKey(jwt.Key),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var principal = ctx.Principal!;
                if (principal.FindFirstValue(AppClaims.Purpose) != AppClaims.AccessPurpose)
                {
                    ctx.Fail("Invalid token purpose.");
                    return;
                }
                var users = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await users.FindByIdAsync(principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "");
                if (user is null
                    || user.SecurityStamp != principal.FindFirstValue(AppClaims.SecurityStamp)
                    || await users.IsLockedOutAsync(user))
                {
                    ctx.Fail("Token revoked.");
                }
            },
        };
    });

builder.Services.AddAuthorization(Policies.Configure);
builder.Services.AddMemoryCache();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PropertyAdmin.Logic.V1.Auth.ITokenIssuer>(sp => sp.GetRequiredService<TokenService>());
builder.Services.Configure<PropertyAdmin.Logic.V1.Invoices.GetPdf.CompanyOptions>(builder.Configuration.GetSection("Company"));

// D-043 (BUG-7): an unreadable JSON body gives the normal 400 validation Problem Details with a
// generic message per JSON path, never a .NET type name or exception text (D-022).
builder.Services.AddControllers()
    .AddJsonOptions(o => o.AllowInputFormatterExceptionMessages = false)
    // T-127 (D-049, D-052, T-140): data annotation messages with an ErrorMessage key are localized through the
    // shared ValidationMessages resources. MVC passes the parameter's own type (string) for record constructor
    // parameters, not the request type, so the provider cannot pick per feature: ValidationMessages is the one
    // data annotation resource for all features. Requests without ErrorMessage keep the framework defaults.
    .AddDataAnnotationsLocalization(o => o.DataAnnotationLocalizerProvider = (_, factory) =>
        factory.Create(typeof(PropertyAdmin.Api.Validation.ValidationMessages)));
// REQ-011 (T-114, D-049): messages in the language of the query parameter `lang` (nl-NL default,
// en-US, case-insensitive; absent or another value gives Dutch). Accept-Language is not read.
// Only the UI culture changes, number and date formatting keep the process culture.
builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(o =>
{
    var culture = System.Globalization.CultureInfo.CurrentCulture;
    o.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culture, new System.Globalization.CultureInfo("nl-NL"));
    o.SupportedCultures = [culture];
    o.SupportedUICultures = [new System.Globalization.CultureInfo("nl-NL"), new System.Globalization.CultureInfo("en-US")];
    o.RequestCultureProviders = [new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider { QueryStringKey = "lang", UIQueryStringKey = "lang" }];
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<PropertyAdmin.Api.ApiExceptionHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsNpgsql()) await db.Database.MigrateAsync();
    else await db.Database.EnsureCreatedAsync();
    await Seeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "PropertyAdmin API v1"));
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
