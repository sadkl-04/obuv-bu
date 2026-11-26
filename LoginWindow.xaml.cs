using System;
using System.Linq;
using System.Windows;
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var user = db.Users.FirstOrDefault(u => u.Login == TxtLogin.Text.Trim() && u.Password == TxtPassword.Password);
                    if (user == null)
                    {
                        MessageBox.Show("Проверьте логин и пароль.");
                        return;
                    }

                    OpenCatalog(user);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть базу данных. " + ex.Message);
            }
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenCatalog(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть каталог. " + ex.Message);
            }
        }

        private void OpenCatalog(User user)
        {
            var catalog = new MainWindow(user);
            catalog.Show();
            Close();
        }
    }
}
