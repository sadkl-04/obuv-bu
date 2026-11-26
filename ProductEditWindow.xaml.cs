using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1
{
    public partial class ProductEditWindow : Window
    {
        private readonly int? _id;

        public ProductEditWindow(Product product)
        {
            InitializeComponent();
            _id = product?.Id;
            DataContext = product ?? new Product { Unit = "шт." };
            Title = product == null ? "Добавление товара" : "Редактирование товара";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Article.Text) || string.IsNullOrWhiteSpace(ProductName.Text) ||
                string.IsNullOrWhiteSpace(Unit.Text) || string.IsNullOrWhiteSpace(Supplier.Text) ||
                string.IsNullOrWhiteSpace(Manufacturer.Text) || string.IsNullOrWhiteSpace(Category.Text))
            {
                MessageBox.Show("Заполните артикул, название, единицу измерения, поставщика, производителя и категорию.");
                return;
            }
            if (!decimal.TryParse(Price.Text, out var price) || price < 0 ||
                !int.TryParse(Stock.Text, out var stock) || stock < 0 ||
                !int.TryParse(Discount.Text, out var discount) || discount < 0 || discount > 100)
            {
                MessageBox.Show("Проверьте цену, остаток и скидку от 0 до 100%.");
                return;
            }

            using (var db = new AppDbContext())
            {
                string article = Article.Text.Trim();
                if (db.Products.Any(p => p.Article == article && p.Id != _id))
                {
                    MessageBox.Show("Такой артикул уже есть.");
                    return;
                }
                var product = _id.HasValue ? db.Products.Find(_id.Value) : new Product();
                if (product == null)
                {
                    MessageBox.Show("Товар уже удалён.");
                    return;
                }
                product.Article = article;
                product.Name = ProductName.Text.Trim();
                product.Unit = Unit.Text.Trim();
                product.Supplier = Supplier.Text.Trim();
                product.Manufacturer = Manufacturer.Text.Trim();
                product.Category = Category.Text.Trim();
                product.Price = price;
                product.Stock = stock;
                product.Discount = discount;
                product.Description = Description.Text.Trim();
                if (!_id.HasValue)
                    db.Products.Add(product);

                try
                {
                    db.SaveChanges();
                    DialogResult = true;
                }
                catch (DbUpdateException)
                {
                    MessageBox.Show("Не удалось сохранить товар. Проверьте артикул.");
                }
            }
        }
    }
}
