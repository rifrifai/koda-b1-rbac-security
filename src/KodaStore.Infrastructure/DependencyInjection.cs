using KodaStore.Application.Repositories;
using KodaStore.Application.Services;
using KodaStore.Infrastructure.Identity;
using KodaStore.Infrastructure.Persistence;
using KodaStore.Infrastructure.Seeding;
using KodaStore.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KodaStore.Infrastructure;
public static class DependencyInjection
{
	public static IServiceCollection AddInfrastucture(this IServiceCollection services, IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString("ecommercedb") ?? throw new InvalidOperationException("ConnectionString : Belum dikonfigurasi");

		services.AddDbContext<AppDbContext>(options =>
		{
			options.UseSqlServer(connectionString)
				.UseSeeding((context, _) =>
				{
					ProductSeedData.Seed(context);
					CategorySeedData.Seed(context);
					OrderSeedData.Seed(context);
				})
				.UseAsyncSeeding(async (context, _, ct) =>
				//    SeedAllAsync(context, ct)
				{
					await ProductSeedData.SeedAsync(context, ct);
					await CategorySeedData.SeedAsync(context, ct);
					await OrderSeedData.SeedAsync(context, ct);
				});
		});

		services.AddScoped<IProductRepository, EfProductRepository>();
		services.AddScoped<IOrderRepository, EfOrderRepository>();
		services.AddScoped<IProductQueryRepository, EfProductQueryRepository>();
		services.AddScoped<ICategoryRepository, EfCategoryRepository>();
		services.AddIdentityCore<AppUser>(options =>
		{
			options.User.RequireUniqueEmail = true;
			options.Password.RequiredLength = 8;
			options.Password.RequireDigit = true;
			options.Password.RequireLowercase = true;
			options.Password.RequireUppercase = true;
			options.Password.RequireNonAlphanumeric = true;
			options.Password.RequiredUniqueChars = 4;

			options.SignIn.RequireConfirmedEmail = false;
		}).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AppDbContext>();

		services.AddScoped<IAuthService, IdentityAuthService>();
		services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
		services.AddScoped<IJwtTokenService, JwtTokenService>();
		services.AddScoped<IRefreshTokenService, RefreshTokenService>();
		
		return services;
	}
}