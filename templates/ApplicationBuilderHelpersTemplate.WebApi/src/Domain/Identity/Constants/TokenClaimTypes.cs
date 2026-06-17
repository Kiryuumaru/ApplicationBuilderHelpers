namespace Domain.Identity.Constants;

public static class TokenClaimTypes
{
    #region Identity Claims

    public const string Subject = "sub";

    public const string Name = "name";

    public const string SessionId = "sid";

    #endregion

    #region Authorization Claims

    public const string Roles = "roles";

    public const string Scope = "scope";

    #endregion

    #region Token Metadata Claims

    public const string TokenId = "jti";

    public const string IssuedAt = "iat";

    public const string ExpiresAt = "exp";

    public const string TokenType = "typ";

    #endregion

    public static class TokenTypeValues
    {
        public const string AccessToken = "at+jwt";

        public const string RefreshToken = "rt+jwt";

        public const string ApiKey = "ak+jwt";
    }
}
