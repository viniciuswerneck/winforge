namespace WinForge.Services;

public interface IUserConfigurationService
{
    bool IsOnboardingCompleted();
    void SetOnboardingCompleted();
}
