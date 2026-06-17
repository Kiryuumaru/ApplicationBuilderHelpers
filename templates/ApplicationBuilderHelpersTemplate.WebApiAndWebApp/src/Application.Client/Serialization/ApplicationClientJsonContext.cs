using System.Text.Json.Serialization;
using Application.Client.Identity.Models;
using Application.Client.Shared.Models;
using Application.Client.Authorization.Models;

namespace Application.Client.Serialization;

/// <summary>
/// JSON serialization context for NativeAOT-compatible serialization.
/// Uses source generators for trimming-safe JSON operations.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Metadata)]

// Authentication models
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(TokenUserInfo))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(StoredCredentials))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(RefreshTokenRequest))]
[JsonSerializable(typeof(ForgotPasswordRequest))]
[JsonSerializable(typeof(ResetPasswordRequest))]
[JsonSerializable(typeof(TwoFactorVerifyRequest))]
[JsonSerializable(typeof(RevokeAllResponse))]
[JsonSerializable(typeof(SessionInfo))]
[JsonSerializable(typeof(ApiKeyInfo))]
[JsonSerializable(typeof(CreateApiKeyRequest))]
[JsonSerializable(typeof(CreateApiKeyResult))]
[JsonSerializable(typeof(TwoFactorSetupInfo))]
[JsonSerializable(typeof(EnableTwoFactorRequest))]
[JsonSerializable(typeof(EnableTwoFactorResponse))]
[JsonSerializable(typeof(DisableTwoFactorRequest))]
[JsonSerializable(typeof(RecoveryCodesResponse))]
[JsonSerializable(typeof(UserProfile))]
[JsonSerializable(typeof(ChangePasswordRequest))]
[JsonSerializable(typeof(ChangeUsernameRequest))]
[JsonSerializable(typeof(ChangeEmailRequest))]
[JsonSerializable(typeof(PasskeyInfo))]
[JsonSerializable(typeof(PasskeyRegistrationOptions))]
[JsonSerializable(typeof(PasskeyRegistrationResult))]
[JsonSerializable(typeof(PasskeyRegistrationOptionsRequest))]
[JsonSerializable(typeof(PasskeyRegistrationRequest))]
[JsonSerializable(typeof(PasskeyRenameRequest))]
[JsonSerializable(typeof(ResetUserPasswordRequest))]

// IAM models
[JsonSerializable(typeof(IamUser))]
[JsonSerializable(typeof(UpdateUserRequest))]
[JsonSerializable(typeof(UserPermissions))]
[JsonSerializable(typeof(IamRole))]
[JsonSerializable(typeof(CreateRoleRequest))]
[JsonSerializable(typeof(UpdateRoleRequest))]
[JsonSerializable(typeof(AssignRoleRequest))]
[JsonSerializable(typeof(UnassignRoleRequest))]
[JsonSerializable(typeof(IamPermission))]
[JsonSerializable(typeof(GrantPermissionRequest))]
[JsonSerializable(typeof(RevokePermissionRequest))]
[JsonSerializable(typeof(ScopeTemplateInfo))]
[JsonSerializable(typeof(ScopeTemplateRequest))]

// Generic collection types
[JsonSerializable(typeof(ListResponse<SessionInfo>), TypeInfoPropertyName = "ListResponseSessionInfo")]
[JsonSerializable(typeof(ListResponse<ApiKeyInfo>), TypeInfoPropertyName = "ListResponseApiKeyInfo")]
[JsonSerializable(typeof(ListResponse<IamRole>), TypeInfoPropertyName = "ListResponseIamRole")]
[JsonSerializable(typeof(ListResponse<IamPermission>), TypeInfoPropertyName = "ListResponseIamPermission")]
[JsonSerializable(typeof(ListResponse<PasskeyInfo>), TypeInfoPropertyName = "ListResponsePasskeyInfo")]
[JsonSerializable(typeof(PagedResponse<IamUser>), TypeInfoPropertyName = "PagedResponseIamUser")]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<Guid>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, System.Text.Json.JsonElement>), TypeInfoPropertyName = "DictionaryStringJsonElement")]
public partial class ApplicationClientJsonContext : JsonSerializerContext
{
}
