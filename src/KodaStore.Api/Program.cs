using KodaStore.Infrastructure.Seeding;
using KodaStore.Application.Services;
using Microsoft.AspNetCore.RateLimiting;
using KodaStore.Api.Middlewares;
using KodaStore.Infrastructure;
using KodaStore.Infrastructure.Identity;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastucture(builder.Configuration);

var jwtOption = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("Konfigurasi JWT belum tersedia");

if (string.IsNullOrWhiteSpace(jwtOption.Issuer)) throw new InvalidOperationException("Jwt: Issuer wajib diisi");
if (string.IsNullOrWhiteSpace(jwtOption.Audience)) throw new InvalidOperationException("Jwt: Audience wajib diisi");
if (jwtOption.AccessTokenMinutes is < 1 or > 60) throw new InvalidOperationException("Jwt: ACCESS Token Minutes antara 1 - 60");

byte[] signingKeyBytes;
try
{
	signingKeyBytes = Convert.FromBase64String(jwtOption.SigningKeyBase64);
}
catch (FormatException e)
{
	throw new InvalidOperationException("Jwt: SigingKeyBase64 tidak menggunakan Base64 yg valid", e);
}

// HS256
if (signingKeyBytes.Length < 32) throw new InvalidOperationException("JWT signing key minimal harus 32 byte");

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductQueryService, ProductQueryService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
	options.AddPolicy("ReactClient", policy =>
	{
		policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
	});
});

builder.Services.AddRateLimiter(option =>
{
	option.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
	option.AddConcurrencyLimiter("category-writes", limiter =>
	{
		limiter.PermitLimit = 8;
		limiter.QueueLimit = 0;
	});

	// Untuk Login
	option.AddPolicy("auth-login", context =>
	{
		var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Nihil";

		return RateLimitPartition.GetFixedWindowLimiter(
			partitionKey: $"login:{ip}",
			factory: _ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = 5,
				Window = TimeSpan.FromMinutes(3),
				QueueLimit = 0,
				AutoReplenishment = true
			}
		);
	});

	option.AddPolicy("auth-register", context =>
	{
		var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Nihil";

		return RateLimitPartition.GetFixedWindowLimiter(
			partitionKey: $"register:{ip}",
			factory: _ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = 5,
				Window = TimeSpan.FromMinutes(10),
				QueueLimit = 0,
				AutoReplenishment = true
			}
		);
	});

	option.AddPolicy("auth-refresh", context =>
	{
		var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Nihil";

		return RateLimitPartition.GetFixedWindowLimiter(
			partitionKey: $"refresh:{ip}",
			factory: _ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = 10,
				Window = TimeSpan.FromMinutes(2),
				QueueLimit = 0,
				AutoReplenishment = true
			}
		);
	});

	// option.AddFixedWindowLimiter("LoginLimiter", limiter =>
	// {
	//     limiter.PermitLimit = 3;
	//     limiter.Window = TimeSpan.FromMinutes(3);
	//     limiter.QueueLimit = 0;
	// });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(option =>
	{
		option.MapInboundClaims = false;
		option.IncludeErrorDetails = false;
		option.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = jwtOption.Issuer,
			ValidateAudience = true,
			ValidAudience = jwtOption.Audience,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
			RequireSignedTokens = true,
			ValidAlgorithms = [
				SecurityAlgorithms.HmacSha256
			],
			ValidateLifetime = true,
			RequireExpirationTime = true,
			ClockSkew = TimeSpan.Zero,
			NameClaimType = JwtRegisteredClaimNames.Sub,
			RoleClaimType = "role"
		};
	});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();

	var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
	var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
	var bootstrapAdminEmail = app.Configuration["RoleBootstrap:AdminEmail"];

	await RoleSeedData.SeedAsync(roleManager, userManager, bootstrapAdminEmail);
	app.MapOpenApi();
}
else
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseMiddleware<RequestTimingMiddleware>();
app.UseCors("ReactClient");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();