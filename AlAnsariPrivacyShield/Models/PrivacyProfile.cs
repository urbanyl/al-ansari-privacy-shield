namespace AlAnsariPrivacyShield.Models;

public sealed record PrivacyProfile(string Label, string Description, string[] EnabledIds)
{
    public override string ToString() => Label;
}
