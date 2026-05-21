namespace PocketMC.Desktop.Tests;

public sealed class DashboardPageXamlTests
{
    [Fact]
    public void InstanceCardMetadata_UsesLabeledFieldsInsteadOfDenseInlineText()
    {
        string xaml = File.ReadAllText(TestSourceFileResolver.Resolve(
            "PocketMC.Desktop",
            "Features",
            "Dashboard",
            "DashboardPage.xaml"));

        Assert.Contains("MetadataItemLabel", xaml);
        Assert.Contains("Text=\"Last played\"", xaml);
        Assert.Contains("{Binding LastPlayedValueText}", xaml);
        Assert.DoesNotContain("{Binding LastPlayedText}\" FontSize=\"11\"", xaml);
    }

    [Fact]
    public void InstanceCardMetadata_CentersRamLabelAndValueInSameColumn()
    {
        string xaml = File.ReadAllText(TestSourceFileResolver.Resolve(
            "PocketMC.Desktop",
            "Features",
            "Dashboard",
            "DashboardPage.xaml"));

        Assert.Contains("<TextBlock Style=\"{StaticResource MetadataItemLabel}\" Text=\"RAM\" TextAlignment=\"Center\"/>", xaml);
        Assert.Contains("<TextBlock Style=\"{StaticResource MetadataItemValue}\" Text=\"{Binding MemoryValueText}\" TextAlignment=\"Center\"/>", xaml);
    }
}
