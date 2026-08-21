using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace DesktopAdmin
{
    public partial class ExportPeriodWindow : Window
    {
        public bool IsConfirmed { get; private set; } = false;
        public string FilterType { get; private set; } = "Monthly";
        public int SelectedYear { get; private set; } = DateTime.Now.Year;
        public int SelectedMonth { get; private set; } = DateTime.Now.Month;
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public string PeriodLabel { get; private set; } = string.Empty;
        public string SafeFileSuffix { get; private set; } = string.Empty;

        private readonly string[] _monthsIndonesian = new string[]
        {
            "Januari", "Februari", "Maret", "April", "Mei", "Juni",
            "Juli", "Agustus", "September", "Oktober", "November", "Desember"
        };

        public ExportPeriodWindow()
        {
            InitializeComponent();
            InitializeSelectors();
        }

        private void InitializeSelectors()
        {
            // Populate Months
            for (int i = 0; i < _monthsIndonesian.Length; i++)
            {
                CmbMonth.Items.Add(new KeyValuePair<int, string>(i + 1, _monthsIndonesian[i]));
            }
            CmbMonth.DisplayMemberPath = "Value";
            CmbMonth.SelectedValuePath = "Key";
            CmbMonth.SelectedIndex = DateTime.Now.Month - 1;

            // Populate Years
            int currentYear = DateTime.Now.Year;
            for (int y = currentYear - 4; y <= currentYear + 1; y++)
            {
                CmbYear.Items.Add(y);
            }
            CmbYear.SelectedItem = currentYear;

            // Initialize DatePickers to current month range
            var now = DateTime.Now;
            DpStartDate.SelectedDate = new DateTime(now.Year, now.Month, 1);
            DpEndDate.SelectedDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
        }

        private void Rb_Checked(object sender, RoutedEventArgs e)
        {
            if (GridMonthlyControls == null || GridRangeControls == null) return;

            if (RbMonthly.IsChecked == true)
            {
                GridMonthlyControls.IsEnabled = true;
                GridRangeControls.IsEnabled = false;
            }
            else if (RbCustomRange.IsChecked == true)
            {
                GridMonthlyControls.IsEnabled = false;
                GridRangeControls.IsEnabled = true;
            }
            else
            {
                GridMonthlyControls.IsEnabled = false;
                GridRangeControls.IsEnabled = false;
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (RbMonthly.IsChecked == true)
            {
                FilterType = "Monthly";
                SelectedMonth = CmbMonth.SelectedIndex + 1;
                SelectedYear = (int)(CmbYear.SelectedItem ?? DateTime.Now.Year);
                string monthName = _monthsIndonesian[SelectedMonth - 1];

                StartDate = new DateTime(SelectedYear, SelectedMonth, 1);
                EndDate = new DateTime(SelectedYear, SelectedMonth, DateTime.DaysInMonth(SelectedYear, SelectedMonth), 23, 59, 59);

                PeriodLabel = $"Bulan {monthName} {SelectedYear}";
                SafeFileSuffix = $"{monthName}_{SelectedYear}";
            }
            else if (RbCustomRange.IsChecked == true)
            {
                if (!DpStartDate.SelectedDate.HasValue || !DpEndDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Silakan pilih tanggal mulai dan tanggal selesai.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DpStartDate.SelectedDate.Value > DpEndDate.SelectedDate.Value)
                {
                    MessageBox.Show("Tanggal mulai tidak boleh melebihi tanggal selesai.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FilterType = "Range";
                StartDate = DpStartDate.SelectedDate.Value.Date;
                EndDate = DpEndDate.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);

                PeriodLabel = $"{StartDate.Value:dd/MM/yyyy} s/d {EndDate.Value:dd/MM/yyyy}";
                SafeFileSuffix = $"{StartDate.Value:yyyyMMdd}_{EndDate.Value:yyyyMMdd}";
            }
            else
            {
                FilterType = "All";
                StartDate = null;
                EndDate = null;
                PeriodLabel = "Seluruh Riwayat (Semua Waktu)";
                SafeFileSuffix = "Semua_Riwayat";
            }

            IsConfirmed = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.Close();
        }
    }
}
