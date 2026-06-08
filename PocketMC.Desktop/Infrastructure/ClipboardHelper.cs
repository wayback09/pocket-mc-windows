using System;
using System.Threading.Tasks;

namespace PocketMC.Desktop.Infrastructure;

/// <summary>
/// Production-grade clipboard helper that prevents crashes from
/// <c>System.Windows.Clipboard.SetText</c>.
///
/// <b>Why this exists:</b> The Windows clipboard uses a global OS mutex.
/// <c>SetText</c> throws <see cref="System.Runtime.InteropServices.COMException"/>
/// (CLIPBRD_E_CANT_OPEN / 0x800401D0) whenever another process holds the
/// clipboard open — antivirus clipboard monitors, password managers, RDP,
/// or simply the previous <c>SetText</c> call that hasn't released yet.
/// Rapid clicking makes this near-certain.
///
/// This helper provides:
/// <list type="bullet">
///   <item>Retry with short delays so transient locks resolve.</item>
///   <item>A reentrancy guard so spam-clicking can't queue competing calls.</item>
///   <item>Swallows all clipboard exceptions on the final attempt to guarantee no crash.</item>
/// </list>
/// </summary>
public static class ClipboardHelper
{
    private const int DefaultMaxRetries = 3;
    private const int DefaultRetryDelayMs = 50;

    private static volatile bool _busy;

    /// <summary>
    /// Safely copies <paramref name="text"/> to the clipboard with retry logic.
    /// Returns <c>true</c> if the text was successfully placed on the clipboard.
    /// Never throws.
    /// </summary>
    public static async Task<bool> TrySetTextAsync(string text, int maxRetries = DefaultMaxRetries, int retryDelayMs = DefaultRetryDelayMs)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // Reentrancy guard — drop concurrent calls from spam-clicking
        if (_busy)
            return false;

        _busy = true;
        try
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    System.Windows.Clipboard.SetText(text);
                    return true;
                }
                catch (System.Runtime.InteropServices.COMException) when (attempt < maxRetries - 1)
                {
                    await Task.Delay(retryDelayMs);
                }
                catch (System.Runtime.InteropServices.ExternalException) when (attempt < maxRetries - 1)
                {
                    await Task.Delay(retryDelayMs);
                }
                catch (Exception)
                {
                    // Final attempt or unexpected exception — swallow to prevent crash
                    return false;
                }
            }

            return false;
        }
        finally
        {
            _busy = false;
        }
    }
}
