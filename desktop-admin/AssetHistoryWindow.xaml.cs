using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace DesktopAdmin
{
    public partial class AssetHistoryWindow : Window
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly AssetModel _asset;
        public ObservableCollection<LoanTransactionModel> LoanHistory { get; set; } = new ObservableCollection<LoanTransactionModel>();

        public AssetHistoryWindow(Supabase.Client supabaseClient, AssetModel asset)
        {
            InitializeComponent();
            _supabaseClient = supabaseClient;
            _asset = asset;
            
            TxtTitle.Text = $"Riwayat Peminjaman: {asset.Name}";
            TxtSubtitle.Text = $"SKU: {asset.AssetCode} | Lokasi: {asset.Location}";
            
            HistoryGrid.ItemsSource = LoanHistory;
            
            _ = LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                var response = await _supabaseClient
                    .From<LoanTransactionModel>()
                    .Where(l => l.AssetId == _asset.Id)
                    .Order(l => l.BorrowDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                var models = response.Models;

                Dispatcher.Invoke(() =>
                {
                    LoanHistory.Clear();
                    foreach (var loan in models)
                    {
                        LoanHistory.Add(loan);
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Gagal memuat riwayat peminjaman: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }
    }
}
