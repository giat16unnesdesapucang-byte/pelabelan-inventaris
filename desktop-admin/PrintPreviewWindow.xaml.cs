using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QRCoder;
using Brushes = System.Windows.Media.Brushes;
using Size = System.Windows.Size;
using Pen = System.Windows.Media.Pen;

namespace DesktopAdmin
{
    public partial class PrintPreviewWindow : Window
    {
        private List<AssetModel> _assetsToPrint;

        public PrintPreviewWindow(List<AssetModel> assetsToPrint)
        {
            InitializeComponent();
            _assetsToPrint = assetsToPrint;
            GenerateDocument();
        }

        private void GenerateDocument()
        {
            FixedDocument document = new FixedDocument();
            document.DocumentPaginator.PageSize = new Size(793.92, 1122.24); // A4 at 96 DPI

            int columns = 3;
            int rows = 5;
            int itemsPerPage = columns * rows;
            
            double marginX = 40;
            double marginY = 40;
            
            double cellWidth = (document.DocumentPaginator.PageSize.Width - (2 * marginX)) / columns;
            double cellHeight = (document.DocumentPaginator.PageSize.Height - (2 * marginY)) / rows;

            for (int i = 0; i < _assetsToPrint.Count; i += itemsPerPage)
            {
                FixedPage page = new FixedPage();
                page.Width = document.DocumentPaginator.PageSize.Width;
                page.Height = document.DocumentPaginator.PageSize.Height;
                page.Background = Brushes.White;

                for (int j = 0; j < itemsPerPage; j++)
                {
                    int assetIndex = i + j;
                    if (assetIndex >= _assetsToPrint.Count) break;

                    AssetModel asset = _assetsToPrint[assetIndex];

                    int col = j % columns;
                    int row = j / columns;

                    Border cell = CreateSticker(asset, cellWidth, cellHeight);
                    
                    FixedPage.SetLeft(cell, marginX + (col * cellWidth));
                    FixedPage.SetTop(cell, marginY + (row * cellHeight));
                    
                    page.Children.Add(cell);
                }

                PageContent pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(page);
                document.Pages.Add(pageContent);
            }

            DocViewer.Document = document;
        }

        private Border CreateSticker(AssetModel asset, double width, double height)
        {
            // The sticker layout
            Border border = new Border
            {
                Width = width,
                Height = height,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(10)
            };

            StackPanel container = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Title
            TextBlock title = new TextBlock
            {
                Text = "BALAI DESA PUCANG",
                FontWeight = FontWeights.Bold,
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            // QR Code
            System.Windows.Controls.Image qrImage = new System.Windows.Controls.Image
            {
                Width = 100,
                Height = 100,
                Margin = new Thickness(0, 5, 0, 5)
            };
            
            if (!string.IsNullOrEmpty(asset.AssetCode))
            {
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(asset.AssetCode, QRCodeGenerator.ECCLevel.Q);
                    using (QRCode qrCode = new QRCode(qrCodeData))
                    {
                        using (Bitmap qrBitmap = qrCode.GetGraphic(20))
                        {
                            qrImage.Source = BitmapToImageSource(qrBitmap);
                        }
                    }
                }
            }

            // Asset ID
            TextBlock idText = new TextBlock
            {
                Text = asset.AssetCode,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                TextAlignment = TextAlignment.Center
            };

            // Asset Name
            TextBlock nameText = new TextBlock
            {
                Text = asset.Name,
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 30
            };

            container.Children.Add(title);
            container.Children.Add(qrImage);
            container.Children.Add(idText);
            container.Children.Add(nameText);

            border.Child = container;
            return border;
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }

        private void BtnPrint_Click(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (DocViewer.Document == null) return;

            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintDocument(((IDocumentPaginatorSource)DocViewer.Document).DocumentPaginator, "Cetak Label QR Aset");
            }
        }
    }
}
