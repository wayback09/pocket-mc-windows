using Markdig;

namespace PocketMC.Desktop.Infrastructure
{
    public static class MarkdownConfig
    {
        public static MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();
    }
}
