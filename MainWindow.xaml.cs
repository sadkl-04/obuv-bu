using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private readonly User _user;
        private bool IsAdmin => _user?.Role == "Администратор";
        private bool CanManageCatalog => IsAdmin || _user?.Role == "Менеджер";

        public MainWindow(User user)
        {
            _user = user;
            InitializeComponent();
            TxtUser.Text = user == null ? "Гость" : $"{user.FullName} · {user.Role}";
            FilterPanel.Visibility = CanManageCatalog ? Visibility.Visible : Visibility.Collapsed;
            AdminPanel.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            OrderAdminPanel.Visibility = AdminPanel.Visibility;
            if (!CanManageCatalog)
                MainTabControl.Items.Remove(TabOrders);

            using (var db = new AppDbContext())
            {
                CmbSupplier.ItemsSource = new[] { "Все поставщики" }.Concat(
                    db.Products.Select(p => p.Supplier).Distinct().ToList().OrderBy(s => s)).ToList();
                CmbSupplier.SelectedIndex = 0;
            }
            LoadProducts();
            LoadOrders();
        }

        private void LoadProducts()
        {
            if (CmbSort == null || CmbSupplier == null || LvProducts == null)
                return;

            using (var db = new AppDbContext())
            {
                var products = db.Products.AsNoTracking().OrderBy(p => p.Id).ToList().AsEnumerable();
                if (CanManageCatalog)
                {
                    var search = TxtSearch.Text.Trim();
                    products = products.Where(p =>
                        new[] { p.Name, p.Article, p.Category, p.Manufacturer, p.Supplier, p.Description }
                            .Any(value => value.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0));

                    if (CmbSupplier.SelectedIndex > 0)
                        products = products.Where(p => p.Supplier == (string)CmbSupplier.SelectedItem);

                    if (CmbSort.SelectedIndex == 1)
                        products = products.OrderBy(p => p.Stock);
                    else if (CmbSort.SelectedIndex == 2)
                        products = products.OrderByDescending(p => p.Stock);
                    else if (CmbSort.SelectedIndex == 3)
                        products = products.OrderBy(p => p.FinalPrice);
                    else if (CmbSort.SelectedIndex == 4)
                        products = products.OrderByDescending(p => p.FinalPrice);
                }
                LvProducts.ItemsSource = products.ToList();
            }
        }

        private void LoadOrders()
        {
            if (!CanManageCatalog)
                return;

            using (var db = new AppDbContext())
            {
                LvOrders.ItemsSource = db.Orders.AsNoTracking().Include(o => o.User).Include(o => o.PickupPoint)
                    .Include(o => o.Items).ThenInclude(i => i.Product).OrderBy(o => o.Id).ToList();
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadProducts();
        private void CmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadProducts();

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e) => EditProduct(null);

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (LvProducts.SelectedItem is Product product)
                EditProduct(product);
            else
                MessageBox.Show("Выберите товар.");
        }

        private void EditProduct(Product product)
        {
            if (IsAdmin && new ProductEditWindow(product) { Owner = this }.ShowDialog() == true)
            {
                using (var db = new AppDbContext())
                {
                    var supplier = CmbSupplier.SelectedItem;
                    CmbSupplier.ItemsSource = new[] { "Все поставщики" }.Concat(
                        db.Products.Select(p => p.Supplier).Distinct().ToList().OrderBy(s => s)).ToList();
                    CmbSupplier.SelectedItem = supplier;
                    if (CmbSupplier.SelectedIndex < 0)
                        CmbSupplier.SelectedIndex = 0;
                }
                LoadProducts();
                LoadOrders();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin)
                return;
            if (!(LvProducts.SelectedItem is Product product))
            {
                MessageBox.Show("Выберите товар.");
                return;
            }

            using (var db = new AppDbContext())
            {
                if (db.OrderItems.Any(item => item.ProductId == product.Id))
                {
                    MessageBox.Show("Товар входит в заказ. Удаление невозможно.");
                    return;
                }
                if (MessageBox.Show("Удалить товар?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    return;
                db.Products.Remove(product);
                try
                {
                    db.SaveChanges();
                    LoadProducts();
                }
                catch (DbUpdateException)
                {
                    MessageBox.Show("Не удалось удалить товар. Проверьте, используется ли он в заказах.");
                }
            }
        }

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e) => EditOrder(null);

        private void BtnEditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (LvOrders.SelectedItem is Order order)
                EditOrder(order);
            else
                MessageBox.Show("Выберите заказ.");
        }

        private void EditOrder(Order order)
        {
            if (IsAdmin && new OrderEditWindow(order) { Owner = this }.ShowDialog() == true)
                LoadOrders();
        }

        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin)
                return;
            if (!(LvOrders.SelectedItem is Order order))
            {
                MessageBox.Show("Выберите заказ.");
                return;
            }
            if (MessageBox.Show("Удалить заказ?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            using (var db = new AppDbContext())
            {
                var savedOrder = db.Orders.Find(order.Id);
                if (savedOrder != null)
                {
                    db.Orders.Remove(savedOrder);
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (DbUpdateException)
                    {
                        MessageBox.Show("Не удалось удалить заказ.");
                    }
                }
            }
            LoadOrders();
        }
    }
}
