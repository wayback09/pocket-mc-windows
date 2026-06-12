namespace PocketMC.Desktop.Features.Shell.Interfaces
{
    public interface ISupportsKeyboardBackNavigation
    {
        /// <summary>
        /// Handles back/cancel keyboard navigation (e.g., Escape, Alt+Left, BrowserBack).
        /// </summary>
        /// <returns>True if the page handled the navigation; False to let default navigation proceed.</returns>
        bool HandleBackNavigation();
    }
}
