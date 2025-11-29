using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;
using System.Collections.Generic;

namespace WpfApp1
{
    public partial class AddRequestWindow : Window
    {
        private string connectionString;
        private DataTable productsTable;
        private List<RequestItem> requestItems = new List<RequestItem>();

        public class RequestItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
        }

        public AddRequestWindow()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            LoadPartners();
            LoadProducts();
            UpdateTotal();
        }

        // Загрузка партнеров
        private void LoadPartners()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT id, Name FROM Partner ORDER BY Name";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    cmbPartners.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки партнеров: " + ex.Message);
            }
        }

        // Загрузка продуктов
        private void LoadProducts()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT id, Name, MinSumPartner FROM Product ORDER BY Name";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    productsTable = new DataTable();
                    adapter.Fill(productsTable);
                    cmbProducts.ItemsSource = productsTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки продуктов: " + ex.Message);
            }
        }

        // Добавить продукт в заявку
        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProducts.SelectedItem == null)
            {
                MessageBox.Show("Выберите продукт");
                return;
            }

            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите правильное количество");
                return;
            }

            DataRowView row = (DataRowView)cmbProducts.SelectedItem;
            int productId = (int)row["id"];
            string productName = row["Name"].ToString();
            decimal price = (decimal)row["MinSumPartner"];

            // Проверяем, не добавлен ли уже этот продукт
            RequestItem existingItem = requestItems.Find(item => item.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
            }
            else
            {
                RequestItem newItem = new RequestItem
                {
                    ProductId = productId,
                    ProductName = productName,
                    Quantity = quantity,
                    UnitPrice = price,
                    TotalPrice = quantity * price
                };
                requestItems.Add(newItem);
            }

            dgSelectedProducts.ItemsSource = null;
            dgSelectedProducts.ItemsSource = requestItems;
            UpdateTotal();
        }

        // Удалить продукт из заявки
        private void btnRemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            RequestItem item = (RequestItem)button.DataContext;
            requestItems.Remove(item);

            dgSelectedProducts.ItemsSource = null;
            dgSelectedProducts.ItemsSource = requestItems;
            UpdateTotal();
        }

        // Обновить итоговую сумму
        private void UpdateTotal()
        {
            decimal total = 0;
            foreach (RequestItem item in requestItems)
            {
                total += item.TotalPrice;
            }
            txtTotal.Text = $"Итоговая сумма: {total:N2} руб.";
        }

        // Сохранить заявку
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPartners.SelectedItem == null)
            {
                MessageBox.Show("Выберите партнера");
                return;
            }

            if (requestItems.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один продукт");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Начинаем транзакцию
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        // 1. Вставляем заявку
                        DataRowView partnerRow = (DataRowView)cmbPartners.SelectedItem;
                        int partnerId = (int)partnerRow["id"];
                        decimal totalAmount = 0;
                        foreach (RequestItem item in requestItems)
                        {
                            totalAmount += item.TotalPrice;
                        }

                        string insertRequest = @"
                            INSERT INTO Requests (id_Partner, TotalAmount) 
                            VALUES (@PartnerId, @TotalAmount);
                            SELECT SCOPE_IDENTITY();";

                        SqlCommand cmdRequest = new SqlCommand(insertRequest, connection, transaction);
                        cmdRequest.Parameters.AddWithValue("@PartnerId", partnerId);
                        cmdRequest.Parameters.AddWithValue("@TotalAmount", totalAmount);

                        int requestId = Convert.ToInt32(cmdRequest.ExecuteScalar());

                        // 2. Вставляем позиции заявки
                        foreach (RequestItem item in requestItems)
                        {
                            string insertItem = @"
                                INSERT INTO RequestItems (id_Request, id_Product, Quantity, UnitPrice, TotalPrice)
                                VALUES (@RequestId, @ProductId, @Quantity, @UnitPrice, @TotalPrice)";

                            SqlCommand cmdItem = new SqlCommand(insertItem, connection, transaction);
                            cmdItem.Parameters.AddWithValue("@RequestId", requestId);
                            cmdItem.Parameters.AddWithValue("@ProductId", item.ProductId);
                            cmdItem.Parameters.AddWithValue("@Quantity", item.Quantity);
                            cmdItem.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                            cmdItem.Parameters.AddWithValue("@TotalPrice", item.TotalPrice);
                            cmdItem.ExecuteNonQuery();
                        }

                        // Подтверждаем транзакцию
                        transaction.Commit();

                        MessageBox.Show("Заявка успешно создана!");
                        this.DialogResult = true;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }

        // Отмена
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Показываем цену при выборе продукта
        private void cmbProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProducts.SelectedItem != null)
            {
                DataRowView row = (DataRowView)cmbProducts.SelectedItem;
                decimal price = (decimal)row["MinSumPartner"];
                txtProductPrice.Text = $"{price:N2} руб.";
            }
        }
    }
}