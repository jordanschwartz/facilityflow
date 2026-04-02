namespace FacilityFlow.Application.DTOs.Users;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);
