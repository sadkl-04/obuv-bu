using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using WpfApp1.Data;
using WpfApp1.Models;

namespace WpfApp1
{
    public partial class OrderEditWindow : Window
    {
        private readonly int? _id;

        public OrderEditWindow(Order order)
        {
            InitializeComponent();
            _id = order?.Id;
            Title = order == null ? "Добавление заказа" : $"Заказ № {order.Id}";
            using (var db = new AppDbContext())
            {
                Client.ItemsSource = db.Users.OrderBy(u => u.FullName).ToList();
                Point.ItemsSource = db.PickupPoints.OrderBy(p => p.Id).ToList();
            }

            Status.ItemsSource = new[] { "Новый", "Завершен" };
            Status.SelectedItem = order?.Status ?? "Новый";
            Placed.SelectedDate = order == null ? DateTime.Today : order.OrderDate;
            if (order != null)
            {
                Client.SelectedValue = order.UserId;
                Point.SelectedValue = order.PickupPointId;
                Delivery.SelectedDate = order.DeliveryDate;
                Code.Text = order.PickupCode.ToString();
                Lines.Text = string.Join(Environment.NewLine, order.Items.Select(i => $"{i.Product.Article}, {i.Quantity}"));
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!(Client.SelectedValue is int userId) || !(Point.SelectedValue is int pointId) ||
                !Delivery.SelectedDate.HasValue || (!_id.HasValue && !Placed.SelectedDate.HasValue) ||
                Delivery.SelectedDate < Placed.SelectedDate ||
                !int.TryParse(Code.Text, out var code) || code <= 0)
            {
                MessageBox.Show("Проверьте клиента, пункт выдачи, даты и код получения.");
                return;
            }

            using (var db = new AppDbContext())
            {
                if (db.Orders.Any(o => o.PickupCode == code && o.Id != _id))
                {
                    MessageBox.Show("Такой код получения уже используется.");
                    return;
                }

                var products = db.Products.ToDictionary(p => p.Article, p => p.Id, StringComparer.Ordinal);
                var quantities = new Dictionary<int, int>();
                foreach (var line in Lines.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var fields = line.Split(',');
                    if (fields.Length != 2 || !products.TryGetValue(fields[0].Trim(), out var productId) ||
                        !int.TryParse(fields[1], out var quantity) || quantity <= 0 || quantities.ContainsKey(productId))
                    {
                        MessageBox.Show("Проверьте строку «" + line + "». Укажите существующий артикул один раз и положительное количество.");
                        return;
                    }
                    quantities.Add(productId, quantity);
                }
                if (quantities.Count == 0)
                {
                    MessageBox.Show("Добавьте хотя бы один товар в заказ.");
                    return;
                }

                var order = _id.HasValue ? db.Orders.Include(o => o.Items).SingleOrDefault(o => o.Id == _id) : new Order();
                if (order == null)
                {
                    MessageBox.Show("Заказ уже удалён.");
                    return;
                }

                order.UserId = userId;
                order.PickupPointId = pointId;
                order.OrderDate = Placed.SelectedDate;
                order.DeliveryDate = Delivery.SelectedDate.Value;
                order.PickupCode = code;
                order.Status = (string)Status.SelectedItem;

                foreach (var item in order.Items.ToList())
                {
                    if (quantities.TryGetValue(item.ProductId, out var quantity))
                    {
                        item.Quantity = quantity;
                        quantities.Remove(item.ProductId);
                    }
                    else
                    {
                        order.Items.Remove(item);
                        db.OrderItems.Remove(item);
                    }
                }
                foreach (var item in quantities)
                    order.Items.Add(new OrderItem { ProductId = item.Key, Quantity = item.Value });

                if (!_id.HasValue)
                    db.Orders.Add(order);

                try
                {
                    db.SaveChanges();
                    DialogResult = true;
                }
                catch (DbUpdateException)
                {
                    MessageBox.Show("Не удалось сохранить заказ. Проверьте код получения и товары.");
                }
            }
        }
    }
}

