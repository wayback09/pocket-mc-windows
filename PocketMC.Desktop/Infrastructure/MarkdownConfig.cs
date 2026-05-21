using Markdig;

namespace PocketMC.Desktop.Infrastructure
{
    public static class MarkdownConfig
    {
        public static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // Handles tables, task lists, grid tables
            .UseEmojiAndSmiley()     // Standard emoji mapping
            .Build();

        public static string RenderToHtml(string markdownText, bool isDarkMode = true, double baseFontSize = 13.0)
        {
            if (string.IsNullOrWhiteSpace(markdownText))
                return "<html><body style='background:transparent;'></body></html>";

            string bodyContent = Markdown.ToHtml(markdownText, Pipeline);

            // Determine premium PocketMC colors based on dark/light mode
            string bgColor = "transparent"; // Overlay beautifully on card backgrounds
            string textColor = isDarkMode ? "#D1D5DB" : "#1F2937";
            string titleColor = isDarkMode ? "#F9FAFB" : "#111827";
            string codeColor = "#34D399";
            string codeBg = isDarkMode ? "rgba(52, 211, 153, 0.12)" : "rgba(16, 185, 129, 0.08)";
            string codeBlockBg = isDarkMode ? "#0D0D0D" : "#F9FAFB";
            string codeBlockColor = isDarkMode ? "#A7F3D0" : "#065F46";
            string codeBlockBorder = isDarkMode ? "#2D2D2D" : "#E5E7EB";
            string tableBorder = isDarkMode ? "#2D2D2D" : "#E5E7EB";
            string tableThBg = isDarkMode ? "#1E1E1E" : "#F3F4F6";
            string tableTrEvenBg = isDarkMode ? "#121212" : "#F9FAFB";
            string linkColor = isDarkMode ? "#34D399" : "#059669";
            string hrColor = isDarkMode ? "#2D2D2D" : "#E5E7EB";
            string quoteBorder = "#10B981";
            string quoteBg = isDarkMode ? "#181818" : "#F9FAFB";
            string quoteColor = isDarkMode ? "#9CA3AF" : "#4B5563";

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1'>
                <style>
                    body {{
                        background-color: {bgColor};
                        color: {textColor};
                        padding: 8px 12px;
                        margin: 0;
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji';
                        font-size: {baseFontSize}px;
                        line-height: 1.625;
                        letter-spacing: 0.01em;
                        overflow-x: hidden;
                    }}

                    /* Headings */
                    h1, h2, h3, h4, h5, h6 {{
                        color: {titleColor};
                        font-weight: 600;
                        margin-top: 20px;
                        margin-bottom: 10px;
                        line-height: 1.3;
                    }}
                    h1 {{ font-size: 1.5em; border-bottom: 1px solid {hrColor}; padding-bottom: 6px; }}
                    h2 {{ font-size: 1.3em; border-bottom: 1px solid {hrColor}; padding-bottom: 4px; }}
                    h3 {{ font-size: 1.15em; }}
                    h4 {{ font-size: 1.05em; }}

                    p {{
                        margin-top: 0;
                        margin-bottom: 10px;
                    }}

                    /* Lists & Bullets */
                    ul, ol {{
                        padding-left: 24px;
                        margin-top: 0;
                        margin-bottom: 10px;
                    }}
                    li {{
                        margin-bottom: 6px;
                    }}
                    li::marker {{
                        color: {linkColor};
                        font-weight: bold;
                    }}

                    /* Task Lists / Checkboxes */
                    li.task-list-item {{
                        list-style-type: none;
                        margin-left: -20px;
                    }}
                    input[type=""checkbox""] {{
                        margin-right: 8px;
                        accent-color: #34D399; /* Premium emerald check */
                        vertical-align: middle;
                        width: 15px;
                        height: 15px;
                        cursor: pointer;
                    }}

                    /* Inline Code */
                    code {{
                        font-family: JetBrains Mono, Fira Code, Consolas, Monaco, monospace;
                        color: {codeColor};
                        background-color: {codeBg};
                        padding: 3px 6px;
                        border-radius: 6px;
                        font-size: 0.9em;
                    }}

                    /* Code Blocks */
                    pre {{
                        background-color: {codeBlockBg};
                        border: 1px solid {codeBlockBorder};
                        border-radius: 8px;
                        padding: 12px 16px;
                        overflow-x: auto;
                        margin-top: 6px;
                        margin-bottom: 12px;
                    }}
                    pre code {{
                        background-color: transparent !important;
                        color: {codeBlockColor};
                        padding: 0;
                        border-radius: 0;
                        font-size: 0.88em;
                    }}

                    /* Hyperlinks */
                    a {{
                        color: {linkColor};
                        text-decoration: none;
                    }}
                    a:hover {{
                        text-decoration: underline;
                    }}

                    /* Premium Rounded-Corner Tables */
                    table {{
                        width: 100%;
                        border-collapse: separate;
                        border-spacing: 0;
                        margin-top: 12px;
                        margin-bottom: 16px;
                        font-size: 0.92em;
                        border: 1px solid {tableBorder};
                        border-radius: 8px;
                        overflow: hidden;
                    }}
                    th, td {{
                        padding: 10px 14px;
                        border-bottom: 1px solid {tableBorder};
                        border-right: 1px solid {tableBorder};
                        text-align: left;
                    }}
                    th:last-child, td:last-child {{
                        border-right: none;
                    }}
                    tr:last-child td {{
                        border-bottom: none;
                    }}
                    th {{
                        background-color: {tableThBg};
                        color: {titleColor};
                        font-weight: 600;
                    }}
                    tr:nth-child(even) {{
                        background-color: {tableTrEvenBg};
                    }}
                    tr:hover {{
                        background-color: {(isDarkMode ? "rgba(255, 255, 255, 0.02)" : "rgba(0, 0, 0, 0.015)")};
                    }}

                    /* Blockquotes */
                    blockquote {{
                        margin: 0 0 12px 0;
                        padding: 10px 16px;
                        background-color: {quoteBg};
                        border-left: 4px solid {quoteBorder};
                        color: {quoteColor};
                        border-radius: 0 6px 6px 0;
                        font-style: italic;
                    }}
                    blockquote p:last-child {{
                        margin-bottom: 0;
                    }}

                    /* Horizontal Rule */
                    hr {{
                        height: 1px;
                        border: none;
                        background-color: {hrColor};
                        margin: 18px 0;
                    }}

                    /* Strong/Bold text formatting */
                    strong, b {{
                        font-weight: 600;
                        color: {titleColor};
                    }}

                    /* Scrollbar customization */
                    ::-webkit-scrollbar {{
                        width: 8px;
                        height: 8px;
                    }}
                    ::-webkit-scrollbar-track {{
                        background: transparent;
                    }}
                    ::-webkit-scrollbar-thumb {{
                        background: {(isDarkMode ? "#3e3e3e" : "#ccc")};
                        border-radius: 4px;
                    }}
                    ::-webkit-scrollbar-thumb:hover {{
                        background: {(isDarkMode ? "#4f4f4f" : "#aaa")};
                    }}
                </style>
            </head>
            <body>
                {bodyContent}
            </body>
            </html>";
        }
    }
}
