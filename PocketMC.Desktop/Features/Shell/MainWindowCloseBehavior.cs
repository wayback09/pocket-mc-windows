namespace PocketMC.Desktop.Features.Shell;

public enum MainWindowCloseAction
{
    Exit,
    HideToTray
}

public static class MainWindowCloseBehavior
{
    public static MainWindowCloseAction Decide(
        bool explicitExitRequested,
        bool hasRunningServers,
        bool minimizeToTrayOnClose,
        bool isRemoteControlRunning)
    {
        if (explicitExitRequested)
        {
            return MainWindowCloseAction.Exit;
        }

        return hasRunningServers || minimizeToTrayOnClose || isRemoteControlRunning
            ? MainWindowCloseAction.HideToTray
            : MainWindowCloseAction.Exit;
    }
}
