using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PropertyAdmin.Api.Auth;
using PropertyAdmin.Api.Services;
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
builder.Services.AddScoped<VatService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddSingleton<InvoicePdfService>();
builder.Services.Configure<CompanyOptions>(builder.Configuration.GetSection("Company"));
builder.Services.Configure<PropertyAdmin.Logic.V1.Invoices.GetPdf.CompanyOptions>(builder.Configuration.GetSection("Company"));

builder.Services.AddControllers();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
