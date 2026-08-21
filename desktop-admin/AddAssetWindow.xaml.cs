using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace DesktopAdmin
{
    public partial class AddAssetWindow : Window
    {
        private Supabase.Client _supabaseClient;
        private AssetModel _editingAsset;
        private string _selectedLocalImagePath;
        private string _existingPhotoUrl;
        private bool _isPhotoRemoved = false;
        public bool IsSuccess { get; private set; }

        public AddAssetWindow(Supabase.Client supabaseClient, AssetModel assetToEdit = null)
        {
            InitializeComponent();
            _supabaseClient = supabaseClient;
            _editingAsset = assetToEdit;
            
            if (_editingAsset != null)
            {
                this.Title = "Ubah Data Aset";
                TxtHeaderTitle.Text = "Ubah Data Aset";
                TxtAssetCode.Text = _editingAsset.AssetCode;
                TxtName.Text = _editingAsset.Name;
                TxtLocation.Text = _editingAsset.Location;
                TxtDescription.Text = _editingAsset.Description ?? string.Empty;
                CmbFundingSource.Text = _editingAsset.FundingSource ?? string.Empty;
                DpPurchaseDate.SelectedDate = _editingAsset.PurchaseDate;
                TxtPrice.Text = _editingAsset.Price.HasValue ? _editingAsset.Price.Value.ToString("0") : "0";
                _existingPhotoUrl = _editingAsset.PhotoUrl;
                
                // Select condition
                foreach (ComboBoxItem item in CmbCondition.Items)
                {
                    if (item.Content.ToString() == _editingAsset.Condition)
                    {
                        CmbCondition.SelectedItem = item;
                        break;
                    }
                }

                // Load existing photo preview if available
                if (!string.IsNullOrWhiteSpace(_existingPhotoUrl))
                {
                    LoadPhotoPreviewFromUrl(_existingPhotoUrl);
                }
            }
            else
            {
                DpPurchaseDate.SelectedDate = DateTime.Now;
            }

            _ = LoadCategoriesAsync();
        }

        private void LoadPhotoPreviewFromUrl(string url)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                ImgAssetPreview.Source = bitmap;
                ImgAssetPreview.Visibility = Visibility.Visible;
                PhotoEmptyPlaceholder.Visibility = Visibility.Collapsed;
                TxtPhotoStatus.Text = "Foto saat ini terpasang.";
            }
            catch
            {
                ImgAssetPreview.Source = null;
                ImgAssetPreview.Visibility = Visibility.Collapsed;
                PhotoEmptyPlaceholder.Visibility = Visibility.Visible;
                TxtPhotoStatus.Text = "Gagal memuat preview foto online.";
            }
        }

        private void LoadPhotoPreviewFromFile(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }

                ImgAssetPreview.Source = bitmap;
                ImgAssetPreview.Visibility = Visibility.Visible;
                PhotoEmptyPlaceholder.Visibility = Visibility.Collapsed;
                TxtPhotoStatus.Text = System.IO.Path.GetFileName(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat gambar: " + ex.Message, "Error Gambar", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Pilih Foto Aset",
                Filter = "File Gambar (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp|Semua File (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var fileInfo = new FileInfo(openFileDialog.FileName);
                if (fileInfo.Length > 5 * 1024 * 1024) // 5 MB limit
                {
                    MessageBox.Show("Ukuran file terlalu besar. Maksimal ukuran foto adalah 5MB.", "Validasi Ukuran", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedLocalImagePath = openFileDialog.FileName;
                _isPhotoRemoved = false;
                LoadPhotoPreviewFromFile(_selectedLocalImagePath);
                
                long sizeKb = fileInfo.Length / 1024;
                TxtPhotoStatus.Text = $"{fileInfo.Name} ({sizeKb} KB)\n⚡ Otomatis dikompres (~40-90 KB) saat disimpan";
            }
        }

        private void BtnRemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            _selectedLocalImagePath = null;
            _existingPhotoUrl = null;
            _isPhotoRemoved = true;

            ImgAssetPreview.Source = null;
            ImgAssetPreview.Visibility = Visibility.Collapsed;
            PhotoEmptyPlaceholder.Visibility = Visibility.Visible;
            TxtPhotoStatus.Text = "Foto dihapus (tidak menggunakan foto).";
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var response = await _supabaseClient.From<CategoryModel>().Get();
                Dispatcher.Invoke(() =>
                {
                    CmbCategory.ItemsSource = response.Models;

                    // Set selected category if editing
                    if (_editingAsset != null && !string.IsNullOrEmpty(_editingAsset.CategoryId))
                    {
                        CmbCategory.SelectedValue = _editingAsset.CategoryId;
                    }
                    else if (_editingAsset == null && CmbCategory.Items.Count > 0 && CmbCategory.SelectedIndex == -1)
                    {
                        CmbCategory.SelectedIndex = 0;
                    }
                });
            }
            catch { }
        }

        private string GetCategoryAbbreviation(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return "UMM";
            
            string lower = categoryName.ToLower();
            if (lower.Contains("elektronik") || lower.Contains("komputer") || lower.Contains("laptop") || lower.Contains("it") || lower.Contains("printer"))
                return "ELK";
            if (lower.Contains("mebel") || lower.Contains("kursi") || lower.Contains("meja") || lower.Contains("lemari") || lower.Contains("perabot") || lower.Contains("furnitur"))
                return "MBL";
            if (lower.Contains("kendaraan") || lower.Contains("motor") || lower.Contains("mobil") || lower.Contains("sepeda"))
                return "KDR";
            if (lower.Contains("peralatan") || lower.Contains("mesin") || lower.Contains("genset") || lower.Contains("tenda") || lower.Contains("sound"))
                return "ALAT";
            if (lower.Contains("bangunan") || lower.Contains("gedung") || lower.Contains("tanah") || lower.Contains("ruang"))
                return "GDG";
            
            var clean = System.Text.RegularExpressions.Regex.Replace(categoryName, "[^a-zA-Z]", "");
            return clean.Length >= 3 ? clean.Substring(0, 3).ToUpper() : "UMM";
        }

        private async Task GenerateAutoSkuCodeAsync(bool force = false)
        {
            if (_editingAsset != null && !force) return;

            try
            {
                var selectedCategory = CmbCategory.SelectedItem as CategoryModel;
                string catAbbr = selectedCategory != null ? GetCategoryAbbreviation(selectedCategory.Name) : "UMM";
                int year = DpPurchaseDate.SelectedDate?.Year ?? DateTime.Now.Year;
                string prefix = $"PCG-{catAbbr}-{year}-";

                var response = await _supabaseClient.From<AssetModel>().Get();
                int maxSeq = 0;
                if (response.Models != null)
                {
                    foreach (var a in response.Models)
                    {
                        if (string.IsNullOrWhiteSpace(a.AssetCode)) continue;
                        if (a.AssetCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            string suffix = a.AssetCode.Substring(prefix.Length);
                            if (int.TryParse(suffix, out int num) && num > maxSeq)
                            {
                                maxSeq = num;
                            }
                        }
                    }
                }

                int nextSeq = maxSeq + 1;
                Dispatcher.Invoke(() =>
                {
                    TxtAssetCode.Text = $"{prefix}{nextSeq:D3}";
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Auto-generate SKU error: " + ex.Message);
            }
        }

        private void BtnAutoGenerateCode_Click(object sender, RoutedEventArgs e)
        {
            _ = GenerateAutoSkuCodeAsync(force: true);
        }

        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_editingAsset == null && (string.IsNullOrWhiteSpace(TxtAssetCode.Text) || TxtAssetCode.Text.StartsWith("PCG-") || TxtAssetCode.Text.StartsWith("INV-")))
            {
                _ = GenerateAutoSkuCodeAsync(force: false);
            }
        }

        private void DpPurchaseDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_editingAsset == null && (string.IsNullOrWhiteSpace(TxtAssetCode.Text) || TxtAssetCode.Text.StartsWith("PCG-") || TxtAssetCode.Text.StartsWith("INV-")))
            {
                _ = GenerateAutoSkuCodeAsync(force: false);
            }
        }

        private byte[] CompressAndResizeImage(string filePath, int maxDimension = 1000, long quality = 75L)
        {
            using (var originalImage = System.Drawing.Image.FromFile(filePath))
            {
                int originalWidth = originalImage.Width;
                int originalHeight = originalImage.Height;

                int newWidth = originalWidth;
                int newHeight = originalHeight;

                // Scale down proportionally if larger than maxDimension
                if (originalWidth > maxDimension || originalHeight > maxDimension)
                {
                    double ratio = (double)originalWidth / originalHeight;
                    if (ratio > 1)
                    {
                        newWidth = maxDimension;
                        newHeight = (int)(maxDimension / ratio);
                    }
                    else
                    {
                        newHeight = maxDimension;
                        newWidth = (int)(maxDimension * ratio);
                    }
                }

                using (var resizedBitmap = new System.Drawing.Bitmap(newWidth, newHeight))
                {
                    using (var graphics = System.Drawing.Graphics.FromImage(resizedBitmap))
                    {
                        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
                    }

                    // Save with JPEG Compression
                    var jpegEncoder = GetEncoder(System.Drawing.Imaging.ImageFormat.Jpeg);
                    var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                    encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                    using (var memoryStream = new MemoryStream())
                    {
                        if (jpegEncoder != null)
                        {
                            resizedBitmap.Save(memoryStream, jpegEncoder, encoderParams);
                        }
                        else
                        {
                            resizedBitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                        return memoryStream.ToArray();
                    }
                }
            }
        }

        private System.Drawing.Imaging.ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format)
        {
            var codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private async Task<string> UploadPhotoToSupabaseAsync(string filePath, string assetCode)
        {
            var safeCode = string.IsNullOrWhiteSpace(assetCode) ? "asset" : assetCode.Replace(" ", "_").Replace("/", "-");
            var fileName = $"{safeCode}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.jpg";
            
            // Kompresi dan optimasi gambar otomatis (Maks 1000px, Kualitas 75%)
            var compressedBytes = await Task.Run(() => CompressAndResizeImage(filePath, maxDimension: 1000, quality: 75L));

            try
            {
                var storage = _supabaseClient.Storage.From("asset-photos");
                await storage.Upload(compressedBytes, fileName, new Supabase.Storage.FileOptions { Upsert = true, ContentType = "image/jpeg" });
                return storage.GetPublicUrl(fileName);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                {
                    msg += " -> " + ex.InnerException.Message;
                }

                throw new Exception($"Gagal mengunggah foto ke Supabase Storage: {msg}\n\n" +
                                    "Jika bucket 'asset-photos' sudah dibuat, pastikan:\n" +
                                    "1. Nama bucket persis: 'asset-photos' (bukan 'assset-photos' / huruf kecil semua).\n" +
                                    "2. RLS Policy Upload sudah diaktifkan di Supabase SQL Editor:\n" +
                                    "   CREATE POLICY \"Allow Upload\" ON storage.objects FOR INSERT WITH CHECK (bucket_id = 'asset-photos');");
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtName.Text) || string.IsNullOrWhiteSpace(TxtAssetCode.Text))
                {
                    MessageBox.Show("Nama dan Kode Aset tidak boleh kosong.", "Validasi Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // UI loading state
                BtnSave.IsEnabled = false;
                TxtSavingStatus.Visibility = Visibility.Visible;

                string finalPhotoUrl = _existingPhotoUrl;

                // 1. Upload photo if newly selected
                if (!string.IsNullOrWhiteSpace(_selectedLocalImagePath))
                {
                    finalPhotoUrl = await UploadPhotoToSupabaseAsync(_selectedLocalImagePath, TxtAssetCode.Text.Trim());
                }
                else if (_isPhotoRemoved)
                {
                    finalPhotoUrl = null;
                }

                // Parse price
                decimal? price = null;
                if (!string.IsNullOrWhiteSpace(TxtPrice.Text))
                {
                    string cleanPrice = TxtPrice.Text.Replace("Rp", "").Replace(".", "").Replace(",", "").Trim();
                    if (decimal.TryParse(cleanPrice, out decimal p))
                    {
                        price = p;
                    }
                }

                string fundingSource = CmbFundingSource.Text?.Trim();
                DateTime? purchaseDate = DpPurchaseDate.SelectedDate;
                string description = TxtDescription.Text?.Trim();

                // 2. Insert or Update Asset in database
                if (_editingAsset == null)
                {
                    var newAsset = new AssetModel
                    {
                        AssetCode = TxtAssetCode.Text.Trim(),
                        Name = TxtName.Text.Trim(),
                        Location = TxtLocation.Text.Trim(),
                        Condition = (CmbCondition.SelectedItem as ComboBoxItem)?.Content.ToString(),
                        AvailabilityStatus = "Tersedia",
                        CategoryId = CmbCategory.SelectedValue?.ToString(),
                        PhotoUrl = finalPhotoUrl,
                        FundingSource = string.IsNullOrWhiteSpace(fundingSource) ? null : fundingSource,
                        PurchaseDate = purchaseDate,
                        Price = price,
                        Description = string.IsNullOrWhiteSpace(description) ? null : description
                    };
                    await _supabaseClient.From<AssetModel>().Insert(newAsset);
                }
                else
                {
                    _editingAsset.AssetCode = TxtAssetCode.Text.Trim();
                    _editingAsset.Name = TxtName.Text.Trim();
                    _editingAsset.Location = TxtLocation.Text.Trim();
                    _editingAsset.Condition = (CmbCondition.SelectedItem as ComboBoxItem)?.Content.ToString();
                    _editingAsset.CategoryId = CmbCategory.SelectedValue?.ToString();
                    _editingAsset.PhotoUrl = finalPhotoUrl;
                    _editingAsset.FundingSource = string.IsNullOrWhiteSpace(fundingSource) ? null : fundingSource;
                    _editingAsset.PurchaseDate = purchaseDate;
                    _editingAsset.Price = price;
                    _editingAsset.Description = string.IsNullOrWhiteSpace(description) ? null : description;
                    
                    await _supabaseClient.From<AssetModel>().Where(a => a.Id == _editingAsset.Id).Update(_editingAsset);
                }
                
                IsSuccess = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan ke Supabase: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSave.IsEnabled = true;
                TxtSavingStatus.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            IsSuccess = false;
            this.Close();
        }
    }
}

