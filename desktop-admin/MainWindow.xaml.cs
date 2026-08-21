using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;
using Supabase;
using Postgrest.Attributes;
using Postgrest.Models;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ClosedXML.Excel;

namespace DesktopAdmin
{
    public partial class MainWindow : Window
    {
        private Supabase.Client _supabaseClient;
        public ObservableCollection<AssetModel> Assets { get; set; } = new ObservableCollection<AssetModel>();
        public ObservableCollection<LoanDisplayModel> Loans { get; set; } = new ObservableCollection<LoanDisplayModel>();
        public ObservableCollection<CategoryModel> Categories { get; set; } = new ObservableCollection<CategoryModel>();
        public ObservableCollection<LoanDisplayModel> RecentActivities { get; set; } = new ObservableCollection<LoanDisplayModel>();
        public ObservableCollection<LoanDisplayModel> OverdueLoans { get; set; } = new ObservableCollection<LoanDisplayModel>();
        private Supabase.Realtime.RealtimeChannel _realtimeChannel;

        public MainWindow()
        {
            InitializeComponent();
            AssetsGrid.ItemsSource = Assets;
            LoansGrid.ItemsSource = Loans;
            CategoriesGrid.ItemsSource = Categories;
            
            if (FindName("ListRecentActivities") != null) 
                ((System.Windows.Controls.ItemsControl)FindName("ListRecentActivities")).ItemsSource = RecentActivities;
            if (FindName("ListOverdueLoans") != null)
                ((System.Windows.Controls.ItemsControl)FindName("ListOverdueLoans")).ItemsSource = OverdueLoans;

            AssetsGrid.SelectionChanged += AssetsGrid_SelectionChanged;

            _ = InitializeSupabaseAsync();
        }

        private async Task InitializeSupabaseAsync()
        {
            var options = new SupabaseOptions
            {
                AutoConnectRealtime = true
            };
            
            _supabaseClient = new Supabase.Client(SupabaseConfig.Url, SupabaseConfig.Key, options);
            await _supabaseClient.InitializeAsync();
            await LoadCategoriesAsync();
            await LoadAssetsAsync();
            await LoadLoansAsync();
            
            await InitializeRealtimeAsync();
        }

        private async Task InitializeRealtimeAsync()
        {
            try
            {
                await _supabaseClient.Realtime.ConnectAsync();
                
                _realtimeChannel = await _supabaseClient.From<AssetModel>().On(Supabase.Realtime.PostgresChanges.PostgresChangesOptions.ListenType.All, (sender, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        _ = LoadAssetsAsync();
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Realtime setup failed: {ex.Message}");
            }
        }

        private async Task LoadAssetsAsync()
        {
            try
            {
                var response = await _supabaseClient.From<AssetModel>().Get();
                var models = response.Models;
                
                Dispatcher.Invoke(() =>
                {
                    Assets.Clear();
                    foreach (var asset in models)
                    {
                        Assets.Add(asset);
                    }
                    
                    if (Assets.Count == 0)
                    {
                        MessageBox.Show("Belum ada data aset di database Supabase Anda. Anda dapat menambahkan data baru.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    
                    UpdateDashboardStats();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Gagal memuat data dari Supabase: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var response = await _supabaseClient.From<CategoryModel>().Get();
                var models = response.Models;
                
                Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    foreach (var c in models)
                    {
                        Categories.Add(c);
                    }
                });
            }
            catch (Exception ex)
            {
                // Ignore silent errors for now
            }
        }

        private async Task LoadLoansAsync()
        {
            try
            {
                var response = await _supabaseClient.From<LoanTransactionModel>().Get();
                var models = response.Models;
                
                Dispatcher.Invoke(() =>
                {
                    Loans.Clear();
                    foreach (var loan in models)
                    {
                        var matchingAsset = Assets.FirstOrDefault(a => a.Id == loan.AssetId);
                        
                        Loans.Add(new LoanDisplayModel
                        {
                            BorrowerName = loan.BorrowerName,
                            BorrowerNik = loan.BorrowerNik,
                            AssetCodeProxy = matchingAsset != null ? matchingAsset.AssetCode : loan.AssetId,
                            BorrowDate = loan.BorrowDate,
                            ExpectedReturnDate = loan.ExpectedReturnDate,
                            Status = loan.Status
                        });
                    }

                    UpdateDashboardStats();
                });
            }
            catch (Exception ex)
            {
                // Ignore silent errors for now or log them
            }
        }
        private void AssetsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (AssetsGrid.SelectedItem is AssetModel selectedAsset)
            {
                GenerateQRCode(selectedAsset.AssetCode);
                QrPreviewText.Text = $"QR untuk {selectedAsset.Name}\nID: {selectedAsset.AssetCode}";

                // Load Photo Preview
                if (!string.IsNullOrWhiteSpace(selectedAsset.PhotoUrl))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(selectedAsset.PhotoUrl, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        PhotoPreviewImage.Source = bitmap;
                        PhotoPreviewImage.Visibility = Visibility.Visible;
                        PhotoPreviewPlaceholder.Visibility = Visibility.Collapsed;
                        PhotoPreviewText.Text = $"{selectedAsset.Name}\nSumber: {selectedAsset.FundingSourceDisplay} | Nilai: {selectedAsset.PriceDisplay}\nTgl Perolehan: {selectedAsset.PurchaseDateDisplay}";
                    }
                    catch
                    {
                        PhotoPreviewImage.Source = null;
                        PhotoPreviewImage.Visibility = Visibility.Collapsed;
                        PhotoPreviewPlaceholder.Visibility = Visibility.Visible;
                        PhotoPreviewText.Text = $"{selectedAsset.Name}\n(Gagal memuat gambar dari URL)\nSumber: {selectedAsset.FundingSourceDisplay} | Nilai: {selectedAsset.PriceDisplay}";
                    }
                }
                else
                {
                    PhotoPreviewImage.Source = null;
                    PhotoPreviewImage.Visibility = Visibility.Collapsed;
                    PhotoPreviewPlaceholder.Visibility = Visibility.Visible;
                    PhotoPreviewText.Text = $"{selectedAsset.Name}\n(Belum ada foto fisik)\nSumber: {selectedAsset.FundingSourceDisplay} | Nilai: {selectedAsset.PriceDisplay}";
                }
            }
            else
            {
                QrPreviewImage.Source = null;
                QrPreviewText.Text = "Pilih aset di tabel untuk melihat QR Code.";
                PhotoPreviewImage.Source = null;
                PhotoPreviewImage.Visibility = Visibility.Collapsed;
                PhotoPreviewPlaceholder.Visibility = Visibility.Visible;
                PhotoPreviewText.Text = "Pilih aset di tabel untuk melihat foto & detail.";
            }
        }

        private void TxtSearchAsset_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(Assets);
            if (view == null) return;

            string filterText = TxtSearchAsset.Text.ToLower();
            if (string.IsNullOrWhiteSpace(filterText))
            {
                view.Filter = null;
            }
            else
            {
                view.Filter = item =>
                {
                    var asset = item as AssetModel;
                    return asset != null && (
                        (asset.Name != null && asset.Name.ToLower().Contains(filterText)) ||
                        (asset.AssetCode != null && asset.AssetCode.ToLower().Contains(filterText)) ||
                        (asset.Location != null && asset.Location.ToLower().Contains(filterText))
                    );
                };
            }
        }

        private async void BtnEditAsset_Click(object sender, RoutedEventArgs e)
        {
            if (AssetsGrid.SelectedItem is AssetModel selectedAsset)
            {
                var form = new AddAssetWindow(_supabaseClient, selectedAsset);
                form.Owner = this;
                form.ShowDialog();
                if (form.IsSuccess)
                {
                    await LoadAssetsAsync();
                }
            }
            else
            {
                MessageBox.Show("Silakan pilih aset yang ingin diubah terlebih dahulu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnDeleteAsset_Click(object sender, RoutedEventArgs e)
        {
            if (AssetsGrid.SelectedItem is AssetModel selectedAsset)
            {
                var result = MessageBox.Show($"Apakah Anda yakin ingin menghapus aset '{selectedAsset.Name}'?", "Konfirmasi Hapus", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _supabaseClient.From<AssetModel>().Where(a => a.Id == selectedAsset.Id).Delete();
                        await LoadAssetsAsync();
                        UpdateDashboardStats();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Gagal menghapus aset: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Silakan pilih aset yang ingin dihapus terlebih dahulu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnViewHistory_Click(object sender, RoutedEventArgs e)
        {
            if (AssetsGrid.SelectedItem is AssetModel selectedAsset)
            {
                var historyWindow = new AssetHistoryWindow(_supabaseClient, selectedAsset);
                historyWindow.Owner = this;
                historyWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Silakan pilih aset yang ingin dilihat riwayatnya terlebih dahulu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void GenerateQRCode(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return;
            
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    using (Bitmap qrBitmap = qrCode.GetGraphic(20))
                    {
                        QrPreviewImage.Source = BitmapToImageSource(qrBitmap);
                    }
                }
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }

        private async void BtnAddAsset_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddAssetWindow(_supabaseClient);
            addWindow.Owner = this;
            addWindow.ShowDialog();

            if (addWindow.IsSuccess)
            {
                await LoadAssetsAsync();
            }
        }

        private void BtnPrintBatch_Click(object sender, RoutedEventArgs e)
        {
            var selectedAssets = new System.Collections.Generic.List<AssetModel>();
            
            // If items are selected, use them. Otherwise, use all items in the grid.
            if (AssetsGrid.SelectedItems.Count > 0)
            {
                foreach (var item in AssetsGrid.SelectedItems)
                {
                    if (item is AssetModel asset)
                        selectedAssets.Add(asset);
                }
            }
            else
            {
                selectedAssets = new System.Collections.Generic.List<AssetModel>(Assets);
            }

            if (selectedAssets.Count == 0)
            {
                MessageBox.Show("Tidak ada aset untuk dicetak.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var printWindow = new PrintPreviewWindow(selectedAssets);
            printWindow.Owner = this;
            printWindow.ShowDialog();
        }

        private void UpdateDashboardStats()
        {
            Dispatcher.Invoke(() =>
            {
                TxtTotalAset.Text = Assets.Count.ToString();
                TxtAsetTersedia.Text = Assets.Count(a => a.AvailabilityStatus == "Tersedia").ToString();
                TxtAsetDipinjam.Text = Assets.Count(a => a.AvailabilityStatus == "Dipinjam").ToString();
                TxtAsetRusak.Text = Assets.Count(a => a.Condition == "Rusak Ringan" || a.Condition == "Rusak Berat").ToString();

                RecentActivities.Clear();
                var recent = Loans.OrderByDescending(l => l.BorrowDate).Take(5);
                foreach (var r in recent) RecentActivities.Add(r);

                OverdueLoans.Clear();
                var overdue = Loans.Where(l => l.Status == "Aktif" && l.ExpectedReturnDate.HasValue && l.ExpectedReturnDate.Value < DateTime.Now).OrderBy(l => l.ExpectedReturnDate).Take(5);
                foreach (var o in overdue) OverdueLoans.Add(o);
            });
        }

        private async void BtnNavDashboard_Click(object sender, RoutedEventArgs e)
        {
            PageDashboard.Visibility = Visibility.Visible;
            PageAssets.Visibility = Visibility.Collapsed;
            PageLoans.Visibility = Visibility.Collapsed;
            PageCategories.Visibility = Visibility.Collapsed;
            
            BtnNavDashboard.Style = (Style)FindResource("SidebarButtonActive");
            BtnNavAssets.Style = (Style)FindResource("SidebarButton");
            BtnNavLoans.Style = (Style)FindResource("SidebarButton");
            BtnNavCategories.Style = (Style)FindResource("SidebarButton");

            await LoadAssetsAsync();
            UpdateDashboardStats();
        }

        private async void BtnNavAssets_Click(object sender, RoutedEventArgs e)
        {
            PageDashboard.Visibility = Visibility.Collapsed;
            PageAssets.Visibility = Visibility.Visible;
            PageLoans.Visibility = Visibility.Collapsed;
            PageCategories.Visibility = Visibility.Collapsed;
            
            BtnNavDashboard.Style = (Style)FindResource("SidebarButton");
            BtnNavAssets.Style = (Style)FindResource("SidebarButtonActive");
            BtnNavLoans.Style = (Style)FindResource("SidebarButton");
            BtnNavCategories.Style = (Style)FindResource("SidebarButton");

            await LoadAssetsAsync();
        }

        private async void BtnNavLoans_Click(object sender, RoutedEventArgs e)
        {
            PageDashboard.Visibility = Visibility.Collapsed;
            PageAssets.Visibility = Visibility.Collapsed;
            PageLoans.Visibility = Visibility.Visible;
            PageCategories.Visibility = Visibility.Collapsed;
            
            BtnNavDashboard.Style = (Style)FindResource("SidebarButton");
            BtnNavAssets.Style = (Style)FindResource("SidebarButton");
            BtnNavLoans.Style = (Style)FindResource("SidebarButtonActive");
            BtnNavCategories.Style = (Style)FindResource("SidebarButton");

            // Refresh loans every time we open the tab
            await LoadLoansAsync();
        }

        private void BtnNavCategories_Click(object sender, RoutedEventArgs e)
        {
            PageDashboard.Visibility = Visibility.Collapsed;
            PageAssets.Visibility = Visibility.Collapsed;
            PageLoans.Visibility = Visibility.Collapsed;
            PageCategories.Visibility = Visibility.Visible;
            
            BtnNavDashboard.Style = (Style)FindResource("SidebarButton");
            BtnNavAssets.Style = (Style)FindResource("SidebarButton");
            BtnNavLoans.Style = (Style)FindResource("SidebarButton");
            BtnNavCategories.Style = (Style)FindResource("SidebarButtonActive");
        }

        private async void BtnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            var form = new CategoryFormWindow(_supabaseClient, null);
            form.Owner = this;
            if (form.ShowDialog() == true)
            {
                // Refresh data if new category was saved
                await LoadCategoriesAsync();
            }
        }

        private async void BtnEditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesGrid.SelectedItem is CategoryModel selectedCategory)
            {
                var form = new CategoryFormWindow(_supabaseClient, selectedCategory);
                form.Owner = this;
                if (form.ShowDialog() == true)
                {
                    await LoadCategoriesAsync();
                }
            }
            else
            {
                MessageBox.Show("Silakan pilih kategori yang ingin diubah terlebih dahulu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesGrid.SelectedItem is CategoryModel selectedCategory)
            {
                var result = MessageBox.Show($"Apakah Anda yakin ingin menghapus kategori '{selectedCategory.Name}'?\nAset yang berada di kategori ini akan kehilangan relasinya.", "Konfirmasi Hapus", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _supabaseClient.From<CategoryModel>().Where(c => c.Id == selectedCategory.Id).Delete();
                        await LoadCategoriesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Gagal menghapus kategori: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Silakan pilih kategori yang ingin dihapus terlebih dahulu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnExportAssets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Assets.Count == 0)
                {
                    MessageBox.Show("Tidak ada data aset untuk diekspor.", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Laporan Inventaris Aset Desa (Excel)",
                    Filter = "File Microsoft Excel (*.xlsx)|*.xlsx|Semua File (*.*)|*.*",
                    FileName = $"Laporan_Buku_Inventaris_Desa_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Buku Inventaris Desa");
                        ws.ShowGridLines = true;

                        // 1. KOP SURAT LAPORAN RESMI DESA
                        ws.Range("A1:K1").Merge();
                        ws.Cell("A1").Value = "PEMERINTAH DESA PUCANG";
                        ws.Cell("A1").Style.Font.Bold = true;
                        ws.Cell("A1").Style.Font.FontSize = 14;
                        ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1E293B");
                        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Range("A2:K2").Merge();
                        ws.Cell("A2").Value = "LAPORAN BUKU INVENTARISASI DAN REKAPITULASI BARANG MILIK DESA (KIB)";
                        ws.Cell("A2").Style.Font.Bold = true;
                        ws.Cell("A2").Style.Font.FontSize = 12;
                        ws.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#0F766E");
                        ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Range("A3:K3").Merge();
                        ws.Cell("A3").Value = $"Status Data: Real-Time Supabase Cloud | Tanggal Cetak: {DateTime.Now.ToString("dd MMMM yyyy, HH:mm", new System.Globalization.CultureInfo("id-ID"))} WIB";
                        ws.Cell("A3").Style.Font.Italic = true;
                        ws.Cell("A3").Style.Font.FontSize = 10;
                        ws.Cell("A3").Style.Font.FontColor = XLColor.FromHtml("#64748B");
                        ws.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // 2. KOTAK RINGKASAN EKSEKUTIF (KPI CARDS)
                        int totalUnit = Assets.Count;
                        decimal totalNilai = Assets.Sum(a => a.Price ?? 0m);
                        int totalTersedia = Assets.Count(a => a.AvailabilityStatus == "Tersedia");
                        int totalDipinjam = Assets.Count(a => a.AvailabilityStatus == "Dipinjam");
                        int totalBaik = Assets.Count(a => a.Condition == "Baik");
                        int totalRusak = Assets.Count(a => a.Condition != "Baik");

                        // Card 1: Total Unit
                        ws.Range("A5:B5").Merge();
                        ws.Cell("A5").Value = "TOTAL ASET";
                        ws.Cell("A5").Style.Font.Bold = true;
                        ws.Cell("A5").Style.Font.FontSize = 9;
                        ws.Cell("A5").Style.Font.FontColor = XLColor.FromHtml("#475569");
                        ws.Cell("A5").Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                        ws.Cell("A5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Range("A6:B6").Merge();
                        ws.Cell("A6").Value = $"{totalUnit} Barang";
                        ws.Cell("A6").Style.Font.Bold = true;
                        ws.Cell("A6").Style.Font.FontSize = 12;
                        ws.Cell("A6").Style.Font.FontColor = XLColor.FromHtml("#0F172A");
                        ws.Cell("A6").Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                        ws.Cell("A6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range("A5:B6").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Range("A5:B6").Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");

                        // Card 2: Total Nilai
                        ws.Range("C5:E5").Merge();
                        ws.Cell("C5").Value = "TOTAL ESTIMASI NILAI ASET";
                        ws.Cell("C5").Style.Font.Bold = true;
                        ws.Cell("C5").Style.Font.FontSize = 9;
                        ws.Cell("C5").Style.Font.FontColor = XLColor.FromHtml("#065F46");
                        ws.Cell("C5").Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
                        ws.Cell("C5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Range("C6:E6").Merge();
                        ws.Cell("C6").Value = totalNilai;
                        ws.Cell("C6").Style.NumberFormat.Format = "_(\"Rp \"* #,##0_);_(\"Rp \"* (#,##0);_(\"Rp \"* \"-\"_);_(@_)";
                        ws.Cell("C6").Style.Font.Bold = true;
                        ws.Cell("C6").Style.Font.FontSize = 12;
                        ws.Cell("C6").Style.Font.FontColor = XLColor.FromHtml("#047857");
                        ws.Cell("C6").Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
                        ws.Cell("C6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range("C5:E6").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Range("C5:E6").Style.Border.OutsideBorderColor = XLColor.FromHtml("#A7F3D0");

                        // Card 3: Status Sirkulasi
                        ws.Range("F5:H5").Merge();
                        ws.Cell("F5").Value = "STATUS KETERSEDIAAN";
                        ws.Cell("F5").Style.Font.Bold = true;
                        ws.Cell("F5").Style.Font.FontSize = 9;
                        ws.Cell("F5").Style.Font.FontColor = XLColor.FromHtml("#1E40AF");
                        ws.Cell("F5").Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
                        ws.Cell("F5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Range("F6:H6").Merge();
                        ws.Cell("F6").Value = $"{totalTersedia} Tersedia | {totalDipinjam} Dipinjam";
                        ws.Cell("F6").Style.Font.Bold = true;
                        ws.Cell("F6").Style.Font.FontSize = 11;
                        ws.Cell("F6").Style.Font.FontColor = XLColor.FromHtml("#1D4ED8");
                        ws.Cell("F6").Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
                        ws.Cell("F6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range("F5:H6").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Range("F5:H6").Style.Border.OutsideBorderColor = XLColor.FromHtml("#BFDBFE");

                        // Card 4: Kondisi Fisik
                        ws.Range("I5:K5").Merge();
                        ws.Cell("I5").Value = "KONDISI KELAYAKAN";
                        ws.Cell("I5").Style.Font.Bold = true;
                        ws.Cell("I5").Style.Font.FontSize = 9;
                        ws.Cell("I5").Style.Font.FontColor = XLColor.FromHtml("#92400E");
                        ws.Cell("I5").Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                        ws.Cell("I5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Range("I6:K6").Merge();
                        ws.Cell("I6").Value = $"{totalBaik} Baik | {totalRusak} Perlu Perbaikan";
                        ws.Cell("I6").Style.Font.Bold = true;
                        ws.Cell("I6").Style.Font.FontSize = 11;
                        ws.Cell("I6").Style.Font.FontColor = XLColor.FromHtml("#B45309");
                        ws.Cell("I6").Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                        ws.Cell("I6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range("I5:K6").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Range("I5:K6").Style.Border.OutsideBorderColor = XLColor.FromHtml("#FDE68A");

                        // 3. TABLE HEADERS (BARIS 8)
                        int startRow = 8;
                        string[] headers = new string[] {
                            "NO", "KODE SKU / ID", "NAMA BARANG / ASET", "KATEGORI",
                            "LOKASI SIMPAN", "SUMBER DANA", "TGL PEROLEHAN",
                            "NILAI ASET (Rp)", "STATUS", "KONDISI", "KETERANGAN / SPESIFIKASI"
                        };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = ws.Cell(startRow, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.FontSize = 10;
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E"); // Emerald dark
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#134E4A");
                        }
                        ws.Row(startRow).Height = 26;

                        // 4. DATA ROWS
                        int currentRow = startRow + 1;
                        int no = 1;
                        foreach (var asset in Assets)
                        {
                            var categoryName = Categories.FirstOrDefault(c => c.Id == asset.CategoryId)?.Name ?? "-";
                            var row = ws.Row(currentRow);
                            row.Height = 20;

                            var rowBg = (no % 2 == 0) ? XLColor.FromHtml("#F8FAFC") : XLColor.White;

                            // 1. NO
                            ws.Cell(currentRow, 1).Value = no;
                            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            // 2. KODE SKU
                            ws.Cell(currentRow, 2).Value = asset.AssetCode;
                            ws.Cell(currentRow, 2).Style.Font.Bold = true;
                            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            // 3. NAMA
                            ws.Cell(currentRow, 3).Value = asset.Name;
                            ws.Cell(currentRow, 3).Style.Font.Bold = true;

                            // 4. KATEGORI
                            ws.Cell(currentRow, 4).Value = categoryName;

                            // 5. LOKASI
                            ws.Cell(currentRow, 5).Value = asset.Location;

                            // 6. SUMBER DANA
                            ws.Cell(currentRow, 6).Value = string.IsNullOrWhiteSpace(asset.FundingSource) ? "-" : asset.FundingSource;
                            ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            // 7. TGL PEROLEHAN
                            if (asset.PurchaseDate.HasValue)
                            {
                                ws.Cell(currentRow, 7).Value = asset.PurchaseDate.Value;
                                ws.Cell(currentRow, 7).Style.DateFormat.Format = "dd/MM/yyyy";
                            }
                            else
                            {
                                ws.Cell(currentRow, 7).Value = "-";
                            }
                            ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            // 8. NILAI ASET (Rp)
                            if (asset.Price.HasValue)
                            {
                                ws.Cell(currentRow, 8).Value = asset.Price.Value;
                                ws.Cell(currentRow, 8).Style.NumberFormat.Format = "_(\"Rp \"* #,##0_);_(\"Rp \"* (#,##0);_(\"Rp \"* \"-\"_);_(@_)";
                            }
                            else
                            {
                                ws.Cell(currentRow, 8).Value = 0;
                                ws.Cell(currentRow, 8).Style.NumberFormat.Format = "_(\"Rp \"* #,##0_);_(\"Rp \"* (#,##0);_(\"Rp \"* \"-\"_);_(@_)";
                            }

                            // 9. STATUS
                            var cellStatus = ws.Cell(currentRow, 9);
                            cellStatus.Value = asset.AvailabilityStatus;
                            cellStatus.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            if (asset.AvailabilityStatus == "Tersedia")
                            {
                                cellStatus.Style.Font.FontColor = XLColor.FromHtml("#047857");
                            }
                            else if (asset.AvailabilityStatus == "Dipinjam")
                            {
                                cellStatus.Style.Font.FontColor = XLColor.FromHtml("#B45309");
                            }

                            // 10. KONDISI
                            var cellKondisi = ws.Cell(currentRow, 10);
                            cellKondisi.Value = asset.Condition;
                            cellKondisi.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            if (asset.Condition != "Baik")
                            {
                                cellKondisi.Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                                cellKondisi.Style.Font.Bold = true;
                            }

                            // 11. KETERANGAN
                            ws.Cell(currentRow, 11).Value = string.IsNullOrWhiteSpace(asset.Description) ? "-" : asset.Description;

                            // Apply formatting to data row
                            var dataRange = ws.Range(currentRow, 1, currentRow, 11);
                            dataRange.Style.Fill.BackgroundColor = rowBg;
                            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                            dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
                            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                            no++;
                            currentRow++;
                        }

                        // 5. TOTAL ROW
                        int totalRowIndex = currentRow;
                        ws.Range(totalRowIndex, 1, totalRowIndex, 7).Merge();
                        var cellTotalLabel = ws.Cell(totalRowIndex, 1);
                        cellTotalLabel.Value = "TOTAL NILAI INVENTARIS ASET DESA";
                        cellTotalLabel.Style.Font.Bold = true;
                        cellTotalLabel.Style.Font.FontSize = 11;
                        cellTotalLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        cellTotalLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        var cellTotalValue = ws.Cell(totalRowIndex, 8);
                        cellTotalValue.FormulaA1 = $"SUM(H{startRow + 1}:H{totalRowIndex - 1})";
                        cellTotalValue.Style.NumberFormat.Format = "_(\"Rp \"* #,##0_);_(\"Rp \"* (#,##0);_(\"Rp \"* \"-\"_);_(@_)";
                        cellTotalValue.Style.Font.Bold = true;
                        cellTotalValue.Style.Font.FontSize = 11;
                        cellTotalValue.Style.Font.FontColor = XLColor.FromHtml("#047857");
                        cellTotalValue.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        ws.Range(totalRowIndex, 9, totalRowIndex, 11).Merge();
                        ws.Cell(totalRowIndex, 9).Value = $"{totalUnit} Unit Terdata";
                        ws.Cell(totalRowIndex, 9).Style.Font.Bold = true;
                        ws.Cell(totalRowIndex, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(totalRowIndex, 9).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        var totalRange = ws.Range(totalRowIndex, 1, totalRowIndex, 11);
                        totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                        totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        totalRange.Style.Border.TopBorderColor = XLColor.FromHtml("#94A3B8");
                        totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
                        totalRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#475569");
                        ws.Row(totalRowIndex).Height = 24;

                        // 6. LEMBAR PENGESAHAN (SIGNATURE BLOCK)
                        int sigRow = totalRowIndex + 3;
                        ws.Cell(sigRow, 2).Value = "Mengetahui,";
                        ws.Cell(sigRow + 1, 2).Value = "Kepala Desa Pucang";
                        ws.Cell(sigRow + 5, 2).Value = "( ..................................................... )";
                        ws.Range(sigRow, 2, sigRow + 5, 4).Style.Font.Bold = true;

                        ws.Cell(sigRow, 8).Value = $"Pucang, {DateTime.Now.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("id-ID"))}";
                        ws.Cell(sigRow + 1, 8).Value = "Pengurus / Petugas Inventaris Barang";
                        ws.Cell(sigRow + 5, 8).Value = "( ..................................................... )";
                        ws.Range(sigRow, 8, sigRow + 5, 11).Style.Font.Bold = true;

                        // Auto-fit Columns with padding
                        ws.Columns().AdjustToContents();
                        ws.Column(1).Width = 6;   // No
                        ws.Column(2).Width = 16;  // SKU
                        ws.Column(3).Width = 28;  // Nama
                        ws.Column(4).Width = 18;  // Kategori
                        ws.Column(5).Width = 20;  // Lokasi
                        ws.Column(6).Width = 22;  // Sumber Dana
                        ws.Column(7).Width = 15;  // Tgl Beli
                        ws.Column(8).Width = 22;  // Harga
                        ws.Column(9).Width = 14;  // Status
                        ws.Column(10).Width = 15; // Kondisi
                        ws.Column(11).Width = 30; // Keterangan

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    var openFile = MessageBox.Show($"Laporan resmi buku inventaris desa berhasil diekspor ke format Excel:\n{saveFileDialog.FileName}\n\nApakah Anda ingin langsung membuka file tersebut?", "Laporan Excel Berhasil", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (openFile == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membuat laporan Excel: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportLoans_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Loans.Count == 0)
                {
                    MessageBox.Show("Tidak ada data sirkulasi peminjaman untuk diekspor.", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 1. Tampilkan Dialog Pilihan Periode
                var periodDialog = new ExportPeriodWindow();
                periodDialog.Owner = this;
                periodDialog.ShowDialog();

                if (!periodDialog.IsConfirmed)
                {
                    return; // User membatalkan
                }

                // 2. Filter data pinjaman berdasarkan tanggal pinjam
                System.Collections.Generic.List<LoanDisplayModel> filteredLoans;
                if (periodDialog.FilterType == "All" || !periodDialog.StartDate.HasValue || !periodDialog.EndDate.HasValue)
                {
                    filteredLoans = new System.Collections.Generic.List<LoanDisplayModel>(Loans);
                }
                else
                {
                    filteredLoans = Loans.Where(l => l.BorrowDate >= periodDialog.StartDate.Value && l.BorrowDate <= periodDialog.EndDate.Value).ToList();
                }

                if (filteredLoans.Count == 0)
                {
                    MessageBox.Show($"Tidak ditemukan riwayat peminjaman untuk periode: {periodDialog.PeriodLabel}.\nSilakan pilih periode lain.", "Data Tidak Ditemukan", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 3. Dialog Simpan File Excel
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Laporan Sirkulasi Peminjaman Aset (Excel)",
                    Filter = "File Microsoft Excel (*.xlsx)|*.xlsx|Semua File (*.*)|*.*",
                    FileName = $"Laporan_Peminjaman_Desa_{periodDialog.SafeFileSuffix}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Laporan Peminjaman");
                        ws.ShowGridLines = true;

                        // 1. KOP SURAT RESMI DESA
                        ws.Range("A1:G1").Merge();
                        ws.Cell("A1").Value = "PEMERINTAH DESA PUCANG";
                        ws.Cell("A1").Style.Font.Bold = true;
                        ws.Cell("A1").Style.Font.FontSize = 14;
                        ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1E293B");
                        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Range("A2:G2").Merge();
                        ws.Cell("A2").Value = "BUKU REKAPITULASI SIRKULASI PEMINJAMAN DAN PENGEMBALIAN ASET DESA";
                        ws.Cell("A2").Style.Font.Bold = true;
                        ws.Cell("A2").Style.Font.FontSize = 12;
                        ws.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#1E40AF");
                        ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Range("A3:G3").Merge();
                        ws.Cell("A3").Value = $"PERIODE: {periodDialog.PeriodLabel.ToUpper()} | Status Data: Cloud Supabase | Tgl Cetak: {DateTime.Now.ToString("dd MMMM yyyy, HH:mm", new System.Globalization.CultureInfo("id-ID"))} WIB";
                        ws.Cell("A3").Style.Font.Italic = true;
                        ws.Cell("A3").Style.Font.FontSize = 10;
                        ws.Cell("A3").Style.Font.FontColor = XLColor.FromHtml("#64748B");
                        ws.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // 2. KOTAK RINGKASAN REKAPITULASI
                        int totalPeminjaman = filteredLoans.Count;
                        int totalKembali = filteredLoans.Count(l => l.Status == "Selesai");
                        int totalBelumKembali = filteredLoans.Count(l => l.Status != "Selesai");

                        ws.Range("A5:B5").Merge();
                        ws.Cell("A5").Value = "TOTAL TRANSAKSI";
                        ws.Cell("A5").Style.Font.Bold = true;
                        ws.Cell("A5").Style.Font.FontSize = 9;
                        ws.Cell("A5").Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                        ws.Cell("A5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Range("A6:B6").Merge();
                        ws.Cell("A6").Value = $"{totalPeminjaman} Transaksi";
                        ws.Cell("A6").Style.Font.Bold = true;
                        ws.Cell("A6").Style.Font.FontSize = 12;
                        ws.Cell("A6").Style.Font.FontColor = XLColor.FromHtml("#0F172A");
                        ws.Cell("A6").Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                        ws.Cell("A6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range("A5:B6").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Range("A5:B6").Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");

                        ws.Range("C5:D5").Merge();
                        ws.Cell("C5").Value = "SUDAH DIKEMBALIKAN";
                        ws.Cell("C5").Style.Font.Bold = true;
                        ws.Cell("C5").Style.Font.FontSize = 9;
                        ws.Cell("C5").Style.Font.FontColor = XLColor.FromHtml("#065F46");
                        ws.Cell("C5").Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
                        ws.Cell("C5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Range("C6:D6").Merge();
                        ws.Cell("C6").Value = $"{totalKembali} Selesai";
                        ws.Cell("C6").Style.Font.Bold = true;
                        ws.Cell("C6").Style.Font.FontSize = 12;
                        ws.Cell("C6").Style.Font.FontColor = XLColor.FromHtml("#047857");
                        ws.Cell("C6").Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
                        ws.Cell("C6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range("C5:D6").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Range("C5:D6").Style.Border.OutsideBorderColor = XLColor.FromHtml("#A7F3D0");

                        ws.Range("E5:G5").Merge();
                        ws.Cell("E5").Value = "MASIH DIPINJAM / BERJALAN";
                        ws.Cell("E5").Style.Font.Bold = true;
                        ws.Cell("E5").Style.Font.FontSize = 9;
                        ws.Cell("E5").Style.Font.FontColor = XLColor.FromHtml("#9A3412");
                        ws.Cell("E5").Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF7ED");
                        ws.Cell("E5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Range("E6:G6").Merge();
                        ws.Cell("E6").Value = $"{totalBelumKembali} Barang Belum Kembali";
                        ws.Cell("E6").Style.Font.Bold = true;
                        ws.Cell("E6").Style.Font.FontSize = 12;
                        ws.Cell("E6").Style.Font.FontColor = XLColor.FromHtml("#C2410C");
                        ws.Cell("E6").Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF7ED");
                        ws.Cell("E6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range("E5:G6").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Range("E5:G6").Style.Border.OutsideBorderColor = XLColor.FromHtml("#FED7AA");

                        // 3. TABLE HEADERS (BARIS 8)
                        int startRow = 8;
                        string[] headers = new string[] {
                            "NO", "NAMA LENGKAP PEMINJAM", "NIK PEMINJAM", "KODE SKU / ASET",
                            "TANGGAL PINJAM", "BATAS KEMBALI", "STATUS TRANSAKSI"
                        };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = ws.Cell(startRow, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.FontSize = 10;
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A"); // Navy dark
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#172554");
                        }
                        ws.Row(startRow).Height = 26;

                        // 4. DATA ROWS
                        int currentRow = startRow + 1;
                        int no = 1;
                        foreach (var loan in filteredLoans)
                        {
                            var rowBg = (no % 2 == 0) ? XLColor.FromHtml("#F8FAFC") : XLColor.White;
                            ws.Row(currentRow).Height = 20;

                            ws.Cell(currentRow, 1).Value = no;
                            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 2).Value = loan.BorrowerName;
                            ws.Cell(currentRow, 2).Style.Font.Bold = true;

                            ws.Cell(currentRow, 3).Value = loan.BorrowerNik;
                            ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 4).Value = loan.AssetCodeProxy;
                            ws.Cell(currentRow, 4).Style.Font.Bold = true;
                            ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 5).Value = loan.BorrowDateString;
                            ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            ws.Cell(currentRow, 6).Value = loan.ExpectedReturnString;
                            ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            var statusCell = ws.Cell(currentRow, 7);
                            statusCell.Value = loan.Status;
                            statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            if (loan.Status == "Selesai")
                            {
                                statusCell.Style.Font.FontColor = XLColor.FromHtml("#047857");
                            }
                            else
                            {
                                statusCell.Style.Font.FontColor = XLColor.FromHtml("#B45309");
                                statusCell.Style.Font.Bold = true;
                            }

                            var dataRange = ws.Range(currentRow, 1, currentRow, 7);
                            dataRange.Style.Fill.BackgroundColor = rowBg;
                            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
                            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                            dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
                            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                            no++;
                            currentRow++;
                        }

                        // 5. TOTAL FOOTER
                        ws.Range(currentRow, 1, currentRow, 6).Merge();
                        ws.Cell(currentRow, 1).Value = $"TOTAL TRANSAKSI PERIODE ({periodDialog.PeriodLabel.ToUpper()})";
                        ws.Cell(currentRow, 1).Style.Font.Bold = true;
                        ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(currentRow, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        ws.Cell(currentRow, 7).Value = $"{filteredLoans.Count} Transaksi";
                        ws.Cell(currentRow, 7).Style.Font.Bold = true;
                        ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(currentRow, 7).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        var totalRange = ws.Range(currentRow, 1, currentRow, 7);
                        totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                        totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        totalRange.Style.Border.TopBorderColor = XLColor.FromHtml("#94A3B8");
                        totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
                        totalRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#475569");
                        ws.Row(currentRow).Height = 24;

                        // 6. LEMBAR PENGESAHAN (SIGNATURE BLOCK)
                        int sigRow = currentRow + 3;
                        ws.Cell(sigRow, 2).Value = "Mengetahui,";
                        ws.Cell(sigRow + 1, 2).Value = "Kepala Desa Pucang";
                        ws.Cell(sigRow + 5, 2).Value = "( ................................................ )";
                        ws.Range(sigRow, 2, sigRow + 5, 3).Style.Font.Bold = true;

                        ws.Cell(sigRow, 5).Value = $"Pucang, {DateTime.Now.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("id-ID"))}";
                        ws.Cell(sigRow + 1, 5).Value = "Pengurus / Petugas Inventaris Desa";
                        ws.Cell(sigRow + 5, 5).Value = "( ................................................ )";
                        ws.Range(sigRow, 5, sigRow + 5, 7).Style.Font.Bold = true;

                        ws.Columns().AdjustToContents();
                        ws.Column(1).Width = 6;
                        ws.Column(2).Width = 28;
                        ws.Column(3).Width = 22;
                        ws.Column(4).Width = 18;
                        ws.Column(5).Width = 18;
                        ws.Column(6).Width = 18;
                        ws.Column(7).Width = 22;

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    var openFile = MessageBox.Show($"Laporan sirkulasi peminjaman ({periodDialog.PeriodLabel}) berhasil diekspor ke Excel:\n{saveFileDialog.FileName}\n\nApakah Anda ingin langsung membuka file tersebut?", "Laporan Excel Berhasil", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (openFile == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membuat laporan peminjaman Excel: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [Table("assets")]
    public class AssetModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("asset_code")]
        public string AssetCode { get; set; }

        [Column("category_id")]
        public string CategoryId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("location")]
        public string Location { get; set; }

        [Column("availability_status")]
        public string AvailabilityStatus { get; set; }

        [Column("condition")]
        public string Condition { get; set; }

        [Column("photo_url")]
        public string? PhotoUrl { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("purchase_date")]
        public DateTime? PurchaseDate { get; set; }

        [Column("price")]
        public decimal? Price { get; set; }

        [Column("funding_source")]
        public string? FundingSource { get; set; }

        public string PriceDisplay => Price.HasValue ? string.Format(new System.Globalization.CultureInfo("id-ID"), "Rp {0:N0}", Price.Value) : "-";
        public string PurchaseDateDisplay => PurchaseDate.HasValue ? PurchaseDate.Value.ToString("dd MMM yyyy") : "-";
        public string FundingSourceDisplay => !string.IsNullOrWhiteSpace(FundingSource) ? FundingSource : "-";
    }

    [Table("loan_transactions")]
    public class LoanTransactionModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("asset_id")]
        public string AssetId { get; set; }

        [Column("borrower_name")]
        public string BorrowerName { get; set; }

        [Column("borrower_nik")]
        public string BorrowerNik { get; set; }

        [Column("borrow_date")]
        public DateTime BorrowDate { get; set; }

        [Column("expected_return_date")]
        public DateTime? ExpectedReturnDate { get; set; }

        [Column("status")]
        public string Status { get; set; }
    }

    public class LoanDisplayModel
    {
        public string BorrowerName { get; set; }
        public string BorrowerNik { get; set; }
        public string AssetCodeProxy { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public string Status { get; set; }

        public string BorrowDateString => BorrowDate.ToString("dd MMM yyyy");
        public string ExpectedReturnString => ExpectedReturnDate.HasValue ? ExpectedReturnDate.Value.ToString("dd MMM yyyy") : "-";
    }

    [Table("categories")]
    public class CategoryModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }
    }
}