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
                    Title = "Export Laporan Inventaris Aset Desa",
                    Filter = "File CSV Excel (*.csv)|*.csv|Semua File (*.*)|*.*",
                    FileName = $"Laporan_Inventaris_Aset_Desa_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();
                    // Header CSV
                    sb.AppendLine("No,Kode SKU,Nama Aset,Kategori,Lokasi,Sumber Dana,Tanggal Perolehan,Nilai Aset (Rp),Status Ketersediaan,Kondisi Fisik,Keterangan");

                    int no = 1;
                    foreach (var asset in Assets)
                    {
                        var categoryName = Categories.FirstOrDefault(c => c.Id == asset.CategoryId)?.Name ?? "-";
                        var tglBeli = asset.PurchaseDate.HasValue ? asset.PurchaseDate.Value.ToString("yyyy-MM-dd") : "-";
                        var harga = asset.Price.HasValue ? asset.Price.Value.ToString("0") : "0";

                        sb.AppendLine(string.Join(",",
                            no++,
                            EscapeCsv(asset.AssetCode),
                            EscapeCsv(asset.Name),
                            EscapeCsv(categoryName),
                            EscapeCsv(asset.Location),
                            EscapeCsv(asset.FundingSource ?? "-"),
                            EscapeCsv(tglBeli),
                            harga,
                            EscapeCsv(asset.AvailabilityStatus),
                            EscapeCsv(asset.Condition),
                            EscapeCsv(asset.Description ?? "-")
                        ));
                    }

                    // Tulis file dengan UTF-8 BOM agar rapi langsung di Excel
                    File.WriteAllText(saveFileDialog.FileName, sb.ToString(), new UTF8Encoding(true));

                    var openFile = MessageBox.Show($"Data {Assets.Count} aset berhasil diekspor ke:\n{saveFileDialog.FileName}\n\nApakah Anda ingin langsung membuka file tersebut?", "Export Berhasil", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (openFile == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengekspor data aset: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Laporan Sirkulasi Peminjaman Aset",
                    Filter = "File CSV Excel (*.csv)|*.csv|Semua File (*.*)|*.*",
                    FileName = $"Laporan_Sirkulasi_Peminjaman_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();
                    // Header CSV
                    sb.AppendLine("No,Nama Peminjam,NIK Peminjam,ID Aset,Tanggal Pinjam,Batas Kembali,Status");

                    int no = 1;
                    foreach (var loan in Loans)
                    {
                        sb.AppendLine(string.Join(",",
                            no++,
                            EscapeCsv(loan.BorrowerName),
                            EscapeCsv(loan.BorrowerNik),
                            EscapeCsv(loan.AssetCodeProxy),
                            EscapeCsv(loan.BorrowDateString),
                            EscapeCsv(loan.ExpectedReturnString),
                            EscapeCsv(loan.Status)
                        ));
                    }

                    // Tulis file dengan UTF-8 BOM
                    File.WriteAllText(saveFileDialog.FileName, sb.ToString(), new UTF8Encoding(true));

                    var openFile = MessageBox.Show($"Data {Loans.Count} riwayat peminjaman berhasil diekspor ke:\n{saveFileDialog.FileName}\n\nApakah Anda ingin langsung membuka file tersebut?", "Export Berhasil", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (openFile == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengekspor data peminjaman: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            string escaped = value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            return $"\"{escaped}\"";
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