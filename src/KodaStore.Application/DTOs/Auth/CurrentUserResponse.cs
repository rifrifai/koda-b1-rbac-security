namespace KodaStore.Application.DTOs.Auth;

public record class CurrentUserResponse(    
    Guid Id,
    string Email,
    IReadOnlyList<string> Roles);
