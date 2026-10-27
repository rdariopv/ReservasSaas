namespace ReservasSaaS.Infrastructure.Authentication;

public class FirebaseAuthenticationOptions
{
    public const string SectionName = "FirebaseAuthentication";

    public string ProjectId { get; set; } = string.Empty;
    public bool AllowSimulatedTokensInDevelopment { get; set; } = true;
    public string Issuer => $"https://securetoken.google.com/{ProjectId}";
    public string Audience => ProjectId;
}
