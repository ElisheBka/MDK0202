using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System.Data.Entity;

namespace WpfApp1
{
    public partial class PartnerProductsWindow : Window
    {
        private int partnerId;
        private string partnerName;

        public PartnerProductsWindow(int partnerId, string partnerName)
        {
            InitializeComponent();
            this.partnerId = partnerId;
            this.partnerName = partnerName;

            this.Title = $"Предлагаемая продукция - {partnerName}";
            txtPartnerName.Text = partnerName;
            LoadPartnerProducts();
        }

        // Загрузка предлагаемой продукции для партнера
        private void LoadPartnerProducts()
        {
            try
            {
                using (var context = new Entities())
                {
                    // Получаем рейтинг партнера
                    var partner = context.Partner.Find(partnerId);
                    int partnerRating = partner?.Rating ?? 0;

                    // Получаем всю продукцию без сложных LINQ-запросов
                    var productsList = new List<object>();

                    // Получаем все продукты
                    var allProducts = context.Product.ToList();

                    // Получаем все типы продуктов
                    var productTypes = context.TypeProduct.ToList();

                    foreach (var product in allProducts)
                    {
                        if (product.MinSumPartner > 0)
                        {
                            // Находим тип продукта
                            var productType = productTypes.FirstOrDefault(t => t.id == product.id_TypeProduct);
                            string typeName = productType?.Name ?? "Не указан";

                            int availableQuantity = CalculateAvailableQuantity(partnerRating, product);

                            productsList.Add(new
                            {
                                id = product.id,
                                ProductName = product.Name,
                                TypeName = typeName,
                                MinPrice = product.MinSumPartner,
                                AvailableQuantity = availableQuantity
                            });
                        }
                    }

                    // Сортируем по имени продукта
                    productsList = productsList.OrderBy(p => ((dynamic)p).ProductName).ToList();

                    dgProducts.ItemsSource = productsList;
                    statusText.Text = $"Загружено {productsList.Count} позиций. Рейтинг партнера: {partnerRating}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки продукции: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для расчета доступного количества
        private int CalculateAvailableQuantity(int partnerRating, Product product)
        {
            // Используем ID продукта для генерации базового количества
            int baseQuantity = (product.id * 15) + 30;

            // Модифицируем количество в зависимости от рейтинга партнера
            if (partnerRating >= 80)
                return (int)(baseQuantity * 1.2);
            else if (partnerRating >= 50)
                return (int)(baseQuantity * 1.1);
            else
                return baseQuantity;
        }

        // Расчет необходимого материала
        private void btnCalculateMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem == null)
            {
                MessageBox.Show("Выберите продукт для расчета материалов", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            dynamic selectedItem = dgProducts.SelectedItem;
            int productId = selectedItem.id;
            string productName = selectedItem.ProductName;
            int availableQuantity = selectedItem.AvailableQuantity;

            MaterialCalculationWindow calcWindow = new MaterialCalculationWindow(productId, productName, availableQuantity);
            calcWindow.Owner = this;
            calcWindow.ShowDialog();
        }

        // Обновить список
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPartnerProducts();
        }

        // Назад
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}