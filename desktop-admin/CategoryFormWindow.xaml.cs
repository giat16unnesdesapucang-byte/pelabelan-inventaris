using System;
using System.Threading.Tasks;
using System.Windows;
using Postgrest.Models;
using Postgrest.Attributes;

namespace DesktopAdmin
{
    public partial class CategoryFormWindow : Window
    {
        private Supabase.Client _supabaseClient;
        private CategoryModel _editingCategory;

        public CategoryFormWindow(Supabase.Client supabaseClient, CategoryModel categoryToEdit = null)
        {
            InitializeComponent();
            _supabaseClient = supabaseClient;
            _editingCategory = categoryToEdit;

            if (_editingCategory != null)
            {
                this.Title = "Ubah Kategori";
                TxtName.Text = _editingCategory.Name;
                TxtDescription.Text = _editingCategory.Description;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtName.Text.Trim();
            string description = TxtDescription.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                TxtError.Text = "Nama kategori wajib diisi.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                if (_editingCategory == null)
                {
                    var newCategory = new CategoryModel
                    {
                        Name = name,
                        Description = string.IsNullOrEmpty(description) ? null : description
                    };
                    await _supabaseClient.From<CategoryModel>().Insert(newCategory);
                }
                else
                {
                    _editingCategory.Name = name;
                    _editingCategory.Description = string.IsNullOrEmpty(description) ? null : description;
                    await _supabaseClient.From<CategoryModel>().Where(c => c.Id == _editingCategory.Id).Update(_editingCategory);
                }
                
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                TxtError.Text = "Terjadi kesalahan saat menyimpan data.";
                TxtError.Visibility = Visibility.Visible;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
