using Markdig;
using Markdig.Extensions.Emoji;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdigTable = Markdig.Extensions.Tables.Table;
using MarkdigTableRow = Markdig.Extensions.Tables.TableRow;
using MarkdigTableCell = Markdig.Extensions.Tables.TableCell;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PocketMC.Desktop.Infrastructure
{
    /// <summary>
    /// Converts Markdig AST into a WPF FlowDocument.
    /// Used for native WPF rendering of markdown without WebView2.
    /// </summary>
    public static class MarkdownFlowDocumentConverter
    {
        // Shared colors matching the existing MarkdownConfig theme
        private static Color Emerald => Color.FromRgb(0x34, 0xD3, 0x99);
        private static Color DarkText => Color.FromRgb(0xD1, 0xD5, 0xDB);
        private static Color DarkTitle => Color.FromRgb(0xF9, 0xFA, 0xFB);
        private static Color DarkCodeBg => Color.FromRgb(0x0D, 0x0D, 0x0D);
        private static Color DarkCodeText => Color.FromRgb(0xA7, 0xF3, 0xD0);
        private static Color DarkQuoteBg => Color.FromRgb(0x18, 0x18, 0x18);
        private static Color DarkQuoteText => Color.FromRgb(0x9C, 0xA3, 0xAF);
        private static Color DarkTableHeaderBg => Color.FromRgb(0x1E, 0x1E, 0x1E);
        private static Color DarkTableBorder => Color.FromRgb(0x2D, 0x2D, 0x2D);
        private static Color DarkTableEvenBg => Color.FromRgb(0x12, 0x12, 0x12);
        private static Color DarkHr => Color.FromRgb(0x2D, 0x2D, 0x2D);

        private static Color LightText => Color.FromRgb(0x1F, 0x29, 0x37);
        private static Color LightTitle => Color.FromRgb(0x11, 0x18, 0x27);
        private static Color LightCodeBg => Color.FromRgb(0xF9, 0xFA, 0xFB);
        private static Color LightCodeText => Color.FromRgb(0x06, 0x5F, 0x46);
        private static Color LightQuoteBg => Color.FromRgb(0xF9, 0xFA, 0xFB);
        private static Color LightQuoteText => Color.FromRgb(0x4B, 0x55, 0x63);
        private static Color LightTableHeaderBg => Color.FromRgb(0xF3, 0xF4, 0xF6);
        private static Color LightTableBorder => Color.FromRgb(0xE5, 0xE7, 0xEB);
        private static Color LightTableEvenBg => Color.FromRgb(0xF9, 0xFA, 0xFB);
        private static Color LightHr => Color.FromRgb(0xE5, 0xE7, 0xEB);
        public static FlowDocument Convert(string markdown, bool isDarkMode)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI, Segoe UI Emoji, sans-serif"),
                FontSize = 13,
                Foreground = new SolidColorBrush(isDarkMode ? DarkText : LightText),
                PagePadding = new Thickness(4, 0, 4, 0),
                TextAlignment = TextAlignment.Left
            };

            if (string.IsNullOrWhiteSpace(markdown))
                return doc;

            var parsed = Markdig.Markdown.Parse(markdown, MarkdownConfig.Pipeline);

            foreach (var block in parsed)
            {
                var wpfBlock = ConvertBlock(block, isDarkMode);
                if (wpfBlock != null)
                    doc.Blocks.Add(wpfBlock);
            }

            return doc;
        }

        private static System.Windows.Documents.Block? ConvertBlock(Markdig.Syntax.Block block, bool isDark)
        {
            return block switch
            {
                HeadingBlock heading => ConvertHeading(heading, isDark),
                ParagraphBlock paragraph => ConvertParagraph(paragraph, isDark),
                ListBlock list => ConvertList(list, isDark),
                ThematicBreakBlock => ConvertThematicBreak(isDark),
                FencedCodeBlock fenced => ConvertCodeBlock(fenced, isDark),
                CodeBlock code => ConvertCodeBlock(code, isDark),
                QuoteBlock quote => ConvertQuote(quote, isDark),
                MarkdigTable table => ConvertTable(table, isDark),
                _ => TryConvertLeafBlock(block, isDark)
            };
        }

        private static System.Windows.Documents.Block? TryConvertLeafBlock(Markdig.Syntax.Block block, bool isDark)
        {
            if (block is LeafBlock leaf && leaf.Inline != null)
            {
                var para = new Paragraph();
                AddInlines(para.Inlines, leaf.Inline, isDark);
                return para;
            }
            return null;
        }

        // ── Headings ────────────────────────────────────────────────────
        private static Paragraph ConvertHeading(HeadingBlock heading, bool isDark)
        {
            double fontSize = heading.Level switch
            {
                1 => 20,
                2 => 17,
                3 => 15,
                _ => 14
            };

            var para = new Paragraph
            {
                FontWeight = FontWeights.SemiBold,
                FontSize = fontSize,
                Foreground = new SolidColorBrush(isDark ? DarkTitle : LightTitle),
                Margin = new Thickness(0, heading.Level <= 2 ? 14 : 8, 0, 6),
                LineHeight = fontSize * 1.35
            };

            // Bottom border for h1/h2 (matching the HTML theme)
            if (heading.Level <= 2)
            {
                para.BorderBrush = new SolidColorBrush(isDark ? DarkHr : LightHr);
                para.BorderThickness = new Thickness(0, 0, 0, 1);
                para.Padding = new Thickness(0, 0, 0, 4);
            }

            if (heading.Inline != null)
                AddInlines(para.Inlines, heading.Inline, isDark);

            return para;
        }

        // ── Paragraphs ──────────────────────────────────────────────────
        private static Paragraph ConvertParagraph(ParagraphBlock paragraph, bool isDark)
        {
            var para = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 8),
                LineHeight = 21
            };

            if (paragraph.Inline != null)
                AddInlines(para.Inlines, paragraph.Inline, isDark);

            return para;
        }

        // ── Lists ───────────────────────────────────────────────────────
        private static List ConvertList(ListBlock list, bool isDark)
        {
            var wpfList = new List
            {
                MarkerStyle = list.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
                Margin = new Thickness(0, 4, 0, 8),
                Padding = new Thickness(16, 0, 0, 0),
                MarkerOffset = 4
            };

            foreach (var item in list)
            {
                if (item is ListItemBlock listItem)
                {
                    var li = new ListItem();
                    foreach (var child in listItem)
                    {
                        var converted = ConvertBlock(child, isDark);
                        if (converted != null)
                            li.Blocks.Add(converted);
                    }
                    wpfList.ListItems.Add(li);
                }
            }

            return wpfList;
        }

        // ── Thematic Break (HR) ─────────────────────────────────────────
        private static BlockUIContainer ConvertThematicBreak(bool isDark)
        {
            var separator = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(isDark ? DarkHr : LightHr),
                Margin = new Thickness(0, 12, 0, 12)
            };
            return new BlockUIContainer(separator);
        }

        // ── Code Blocks ─────────────────────────────────────────────────
        private static Paragraph ConvertCodeBlock(LeafBlock codeBlock, bool isDark)
        {
            var text = codeBlock.Lines.ToString().TrimEnd();
            return new Paragraph(new Run(text))
            {
                FontFamily = new FontFamily("JetBrains Mono, Consolas, 'Courier New', Segoe UI Emoji, monospace"),
                FontSize = 12,
                Foreground = new SolidColorBrush(isDark ? DarkCodeText : LightCodeText),
                Background = new SolidColorBrush(isDark ? DarkCodeBg : LightCodeBg),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 4, 0, 10),
                LineHeight = 18
            };
        }

        // ── Blockquotes ─────────────────────────────────────────────────
        private static Section ConvertQuote(QuoteBlock quoteBlock, bool isDark)
        {
            var section = new Section
            {
                BorderBrush = new SolidColorBrush(Emerald),
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(12, 4, 0, 4),
                Margin = new Thickness(0, 4, 0, 8),
                Background = new SolidColorBrush(isDark ? DarkQuoteBg : LightQuoteBg),
                Foreground = new SolidColorBrush(isDark ? DarkQuoteText : LightQuoteText)
            };

            foreach (var child in quoteBlock)
            {
                var converted = ConvertBlock(child, isDark);
                if (converted != null)
                    section.Blocks.Add(converted);
            }

            return section;
        }

        // ── Tables ──────────────────────────────────────────────────────
        private static System.Windows.Documents.Table ConvertTable(MarkdigTable markdigTable, bool isDark)
        {
            var wpfTable = new System.Windows.Documents.Table
            {
                BorderBrush = new SolidColorBrush(isDark ? DarkTableBorder : LightTableBorder),
                BorderThickness = new Thickness(1),
                CellSpacing = 0,
                Margin = new Thickness(0, 8, 0, 12),
                FontSize = 12
            };

            // Determine column count from first row
            int colCount = 0;
            foreach (var row in markdigTable.OfType<MarkdigTableRow>())
            {
                if (row.Count > colCount) colCount = row.Count;
            }

            for (int i = 0; i < colCount; i++)
            {
                wpfTable.Columns.Add(new TableColumn());
            }

            var rowGroup = new TableRowGroup();
            int rowIndex = 0;

            foreach (var row in markdigTable.OfType<MarkdigTableRow>())
            {
                var wpfRow = new System.Windows.Documents.TableRow();
                bool isHeader = row.IsHeader;

                if (isHeader)
                {
                    wpfRow.Background = new SolidColorBrush(isDark ? DarkTableHeaderBg : LightTableHeaderBg);
                }
                else if (rowIndex % 2 == 0)
                {
                    wpfRow.Background = new SolidColorBrush(isDark ? DarkTableEvenBg : LightTableEvenBg);
                }

                foreach (var cell in row.OfType<MarkdigTableCell>())
                {
                    var wpfCell = new System.Windows.Documents.TableCell
                    {
                        BorderBrush = new SolidColorBrush(isDark ? DarkTableBorder : LightTableBorder),
                        BorderThickness = new Thickness(0, 0, 1, 1),
                        Padding = new Thickness(10, 6, 10, 6)
                    };

                    if (isHeader)
                    {
                        wpfCell.FontWeight = FontWeights.SemiBold;
                        wpfCell.Foreground = new SolidColorBrush(isDark ? DarkTitle : LightTitle);
                    }

                    foreach (var child in cell)
                    {
                        var converted = ConvertBlock(child, isDark);
                        if (converted != null)
                        {
                            // Remove margin from paragraphs inside table cells
                            if (converted is Paragraph tablePara)
                                tablePara.Margin = new Thickness(0);
                            wpfCell.Blocks.Add(converted);
                        }
                    }

                    if (wpfCell.Blocks.Count == 0)
                        wpfCell.Blocks.Add(new Paragraph());

                    wpfRow.Cells.Add(wpfCell);
                }

                // Pad missing cells
                while (wpfRow.Cells.Count < colCount)
                {
                    wpfRow.Cells.Add(new System.Windows.Documents.TableCell(new Paragraph()));
                }

                rowGroup.Rows.Add(wpfRow);
                if (!isHeader) rowIndex++;
            }

            wpfTable.RowGroups.Add(rowGroup);
            return wpfTable;
        }

        // ── Inline Rendering ────────────────────────────────────────────
        private static void AddInlines(InlineCollection inlines, ContainerInline container, bool isDark)
        {
            foreach (var inline in container)
            {
                switch (inline)
                {
                    case EmojiInline emoji:
                        AddEmojiAwareRuns(inlines, emoji.Content.ToString(), forceEmojiFont: true);
                        break;

                    case HtmlEntityInline entity:
                        AddEmojiAwareRuns(inlines, entity.Transcoded.ToString());
                        break;

                    case LiteralInline literal:
                        AddEmojiAwareRuns(inlines, literal.Content.ToString());
                        break;

                    case EmphasisInline emphasis:
                        var span = new Span();
                        if (emphasis.DelimiterCount >= 2)
                        {
                            span.FontWeight = FontWeights.SemiBold;
                            span.Foreground = new SolidColorBrush(isDark ? DarkTitle : LightTitle);
                        }
                        else
                        {
                            span.FontStyle = FontStyles.Italic;
                        }
                        AddInlines(span.Inlines, emphasis, isDark);
                        inlines.Add(span);
                        break;

                    case CodeInline code:
                        inlines.Add(new Run(code.Content)
                        {
                            FontFamily = new FontFamily("JetBrains Mono, Consolas, Segoe UI Emoji, monospace"),
                            FontSize = 11.5,
                            Background = new SolidColorBrush(isDark
                                ? Color.FromArgb(30, 0x34, 0xD3, 0x99)
                                : Color.FromArgb(20, 0x10, 0xB9, 0x81)),
                            Foreground = new SolidColorBrush(Emerald)
                        });
                        break;

                    case LineBreakInline lineBreak:
                        if (lineBreak.IsHard)
                            inlines.Add(new LineBreak());
                        else
                            inlines.Add(new Run(" "));
                        break;

                    case LinkInline link:
                        if (link.IsImage)
                        {
                            // Skip images in FlowDocument — can't render inline
                            inlines.Add(new Run($"[{link.FirstChild}]")
                            {
                                Foreground = new SolidColorBrush(isDark ? DarkQuoteText : LightQuoteText),
                                FontStyle = FontStyles.Italic
                            });
                        }
                        else
                        {
                            var hyperlink = new Hyperlink
                            {
                                Foreground = new SolidColorBrush(Emerald),
                                TextDecorations = null
                            };
                            if (Uri.TryCreate(link.Url, UriKind.Absolute, out var uri))
                                hyperlink.NavigateUri = uri;
                            AddInlines(hyperlink.Inlines, link, isDark);
                            inlines.Add(hyperlink);
                        }
                        break;

                    case ContainerInline otherContainer:
                        AddInlines(inlines, otherContainer, isDark);
                        break;
                }
            }
        }

        private static void AddEmojiAwareRuns(InlineCollection inlines, string text, bool forceEmojiFont = false)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (forceEmojiFont)
            {
                inlines.Add(CreateEmojiInline(text));
                return;
            }

            var textElements = StringInfo.GetTextElementEnumerator(text);
            var segment = new StringBuilder();
            bool? segmentUsesEmojiFont = null;

            while (textElements.MoveNext())
            {
                string textElement = textElements.GetTextElement();
                bool useEmojiFont = ContainsEmojiScalar(textElement);

                if (segmentUsesEmojiFont != useEmojiFont && segment.Length > 0)
                {
                    inlines.Add(segmentUsesEmojiFont == true
                        ? CreateEmojiInline(segment.ToString())
                        : CreateRun(segment.ToString(), useEmojiFont: false));
                    segment.Clear();
                }

                segmentUsesEmojiFont = useEmojiFont;
                segment.Append(textElement);
            }

            if (segment.Length > 0)
            {
                inlines.Add(segmentUsesEmojiFont == true
                    ? CreateEmojiInline(segment.ToString())
                    : CreateRun(segment.ToString(), useEmojiFont: false));
            }
        }

        private static Run CreateRun(string text, bool useEmojiFont)
        {
            return new Run(text);
        }

        private static System.Windows.Documents.Inline CreateEmojiInline(string text)
        {
            var textBlock = new Emoji.Wpf.TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            return new InlineUIContainer(textBlock)
            {
                BaselineAlignment = BaselineAlignment.Center
            };
        }

        private static bool ContainsEmojiScalar(string textElement)
        {
            foreach (var rune in textElement.EnumerateRunes())
            {
                if (IsEmojiRune(rune.Value))
                    return true;
            }

            return false;
        }

        private static bool IsEmojiRune(int value)
        {
            return (value >= 0x1F000 && value <= 0x1FAFF)
                   || (value >= 0x2600 && value <= 0x27BF);
        }
    }
}
