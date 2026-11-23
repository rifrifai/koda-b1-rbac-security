using KodaStore.Application.DTOs.Auth;
using KodaStore.Application.Security;
using KodaStore.Application.Services;
using KodaStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KodaStore.Infrastructure.Identity;

public sealed class IdentityAuthService(UserManager<AppUser> _user, AppDbContext db) : IAuthService
{

	public async Task<LoginResults> LoginAsync(LoginRequest request, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();
		var email = request.Email.Trim();
		var getUser = await _user.FindByEmailAsync(email);

		if (getUser is null) return new LoginResults(false, null);
		var passwordValid = await _user.CheckPasswordAsync(getUser, request.Password);

		if (!passwordValid) return new LoginResults(false, null);
		var roles = await _user.GetRolesAsync(getUser);

		var response = new AuthUserResponse(getUser.Id, getUser.Email!, getUser.DisplayName, [.. roles]);

		return new LoginResults(true, response);
	}

	public async Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();
		var email = request.Email.Trim();
		var existingUser = await _user.FindByEmailAsync(email);
		if (existingUser is not null) return new RegisterResult(RegisterStatus.DuplicateEmail, null, ["Email sudah terdaftar"]);

		var user = new AppUser
		{
			Id = Guid.NewGuid(),
			UserName = email,
			Email = email,
			DisplayName = request.DisplayName.Trim(),
			CreatedAtUtc = DateTimeOffset.UtcNow
		};

		// IdentityResult identityResult; Tambahan
		await using var transaction = await db.Database.BeginTransactionAsync(ct);

		try
		{
			// identityResult = await _user.CreateAsync(user, request.Password);
			var createResult = await _user.CreateAsync(user, request.Password);

			if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(ct);

                var errors = createResult.Errors.ToArray();

                var duplicate = errors.Any(error =>
                    error.Code is "DuplicateEmail" or "DuplicateUserName");

                if (duplicate)
                {
                    return new RegisterResult(
                        RegisterStatus.DuplicateEmail,
                        null,
                        ["Email sudah terdaftar."]);
                }

                return new RegisterResult(
                    RegisterStatus.ValidationError,
                    null,
                    [.. errors.Select(error => error.Description)]);
            }

            var roleResult = await _user.AddToRoleAsync(user, AppRoles.Customer);

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    roleResult.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Gagal memberikan role CUSTOMER: {errors}");
            }

            await transaction.CommitAsync(ct);
		}
		catch (DbUpdateException e) when (e.InnerException is SqlException sqlError && sqlError.Number is 2601 or 2627)
		{
			return new RegisterResult(RegisterStatus.DuplicateEmail, null, [
				"Email sudah terdaftar"
			]);
		}

		// if (!identityResult.Succeeded)
		// {
		// 	var error = identityResult.Errors.ToArray();

		// 	var isDuplicate = error.Any(error => error.Code is "DuplicateEmail" or "DuplicateUsername");
		// 	if (isDuplicate)
		// 	{
		// 		return new RegisterResult(RegisterStatus.DuplicateEmail, null, ["Email sudah terdaftar"]);
		// 	}
		// 	return new RegisterResult(RegisterStatus.ValidationError,
		// 	null,
		// 	error.Select(error => error.Description).ToArray());
		// }

		var roles = await _user.GetRolesAsync(user);
		
		var response = new AuthUserResponse(
			user.Id,
			user.Email!,
			user.DisplayName,
			[.. roles]
		);

		return new RegisterResult(RegisterStatus.Created, response, []);
	}
}