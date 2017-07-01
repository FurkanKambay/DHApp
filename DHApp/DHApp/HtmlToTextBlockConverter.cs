using HtmlAgilityPack;
using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace DHApp
{
    public class HtmlToTextBlockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml((string)value);

            var formattedTextBlock = new TextBlock { TextWrapping = System.Windows.TextWrapping.Wrap };
            foreach (var node in htmlDoc.DocumentNode.ChildNodes)
            {
                Inline inline = new Run(node.InnerText);

                if (node.Name == "strong")
                    inline = new Bold(inline);
                else if (node.Name == "i")
                    inline = new Italic(inline);

                formattedTextBlock.Inlines.Add(inline);
            }
            return formattedTextBlock;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new InvalidOperationException();
    }
}
