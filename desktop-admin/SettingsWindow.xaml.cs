using System;
using System.Threading.Tasks;
using System.Windows;
using Postgrest.Attributes;
using Postgrest.Models;

namespace DesktopAdmin
{
    [Table("system_settings")]
    public class SystemSettingModel : BaseModel
    {
        [PrimaryKey("key", false)]
        public string Key { get; set; }

        [Column("value")]
        public string Value { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public partial class SettingsWindow : Window
    {
        private readonly Supabase.Client _supabaseClient;
        public bool IsSuccess { get; private set; } = false;

        public SettingsWindow(Supabase.Client supabaseClient)
        {
            InitializeComponent();
            _supabaseClient = supabaseClient;
            _ = LoadCurrentPinAsync();
        }

        private async Task LoadCurrentPinAsync()
        {
            try
            {
                var response = await _supabaseClient.From<SystemSettingModel>().Where(s => s.Key == "staff_pin").Get();
                var setting = response.Model;
                if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
                {
                    TxtCurrentPinDisplay.Text = setting.Value;
                }
                else
                {
                    TxtCurrentPinDisplay.Text = "123456 (Default)";
                }
            }
            catch (Exception ex)
            {
                TxtCurrentPinDisplay.Text = "123456 (Offline/Default)";
                Console.WriteLine("Error loading PIN: " + ex.Message);
            }
        }

        private async void BtnSavePin_Click(object sender, RoutedEventArgs e)
        {
            string newPin = TxtNewPin.Text.Trim();
            string confirmPin = TxtConfirmPin.Text.Trim();

            if (string.IsNullOrWhiteSpace(newPin))
            {
                MessageBox.Show("PIN baru tidak boleh kosong.", "Validasi Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPin.Length < 4 || newPin.Length > 8)
            {
                MessageBox.Show("PIN harus terdiri dari 4 hingga 8 digit angka.", "Validasi Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPin != confirmPin)
            {
                MessageBox.Show("Konfirmasi PIN tidak cocok dengan PIN baru.", "Validasi Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                BtnSavePin.IsEnabled = false;
                TxtSavingStatus.Visibility = Visibility.Visible;

                var setting = new SystemSettingModel
                {
                    Key = "staff_pin",
                    Value = newPin,
                    Description = "PIN Keamanan Aktivasi Aplikasi Petugas Lapangan",
                    UpdatedAt = DateTime.UtcNow
                };

                // Upsert into Supabase system_settings
                await _supabaseClient.From<SystemSettingModel>().Upsert(setting);

                IsSuccess = true;
                MessageBox.Show($"PIN Keamanan Petugas berhasil diperbarui menjadi: {newPin}\n\nPetugas dapat mengaktivasi smartphone dengan PIN baru ini.", "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan PIN ke database: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSavePin.IsEnabled = true;
                TxtSavingStatus.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
