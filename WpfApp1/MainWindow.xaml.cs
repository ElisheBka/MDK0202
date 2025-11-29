using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Data.Entity;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadPartners();
        }

        // Загрузка всех партнеров
        private void LoadPartners()
        {
            try
            {
                using (var context = new Entities())
                {
                    var partners = context.Partner
                        .Include(p => p.TypePartner)
                        .Select(p => new
                        {
                            id = p.id,
                            TypeName = p.TypePartner.Name,
                            p.Name,
                            p.Director,
                            p.Adress,

                            p.Rating,
                            p.NumberPartner,
                            p.email
                        })
                        .OrderBy(p => p.Name)
                        .ToList();

                    dgPartners.ItemsSource = partners;
                    statusText.Text = $"Загружено {partners.Count} партнеров";
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка загрузки партнеров: {ex.Message}",
                    "Проверьте подключение к базе данных и повторите попытку.");
                statusText.Text = "Ошибка загрузки данных";
            }
        }

        // Добавить партнера
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            PartnerFormWindow partnerWindow = new PartnerFormWindow();
            partnerWindow.Owner = this;
            if (partnerWindow.ShowDialog() == true)
            {
                LoadPartners();
                ShowInfoMessage("Партнер успешно добавлен", "Новый партнер был добавлен в систему.");
            }
        }

        // Редактировать партнера
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgPartners.SelectedItem == null)
            {
                ShowWarningMessage("Выберите партнера", "Пожалуйста, выберите партнера для редактирования.");
                return;
            }

            dynamic selectedItem = dgPartners.SelectedItem;
            int partnerId = selectedItem.id;

            PartnerFormWindow partnerWindow = new PartnerFormWindow(partnerId);
            partnerWindow.Owner = this;
            if (partnerWindow.ShowDialog() == true)
            {
                LoadPartners();
                ShowInfoMessage("Данные обновлены", "Информация о партнере была успешно обновлена.");
            }
        }

        // Удалить партнера - ОДИН ЕДИНСТВЕННЫЙ МЕТОД
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgPartners.SelectedItem == null)
            {
                ShowWarningMessage("Выберите партнера", "Пожалуйста, выберите партнера для удаления.");
                return;
            }

            dynamic selectedItem = dgPartners.SelectedItem;
            int partnerId = selectedItem.id;
            string partnerName = selectedItem.Name;

            var result = ShowConfirmationMessage(
                $"Удалить партнера '{partnerName}'?",
                "Это действие невозможно отменить. Все связанные заявки также будут удалены.");

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new Entities())
                    {
                        var partner = context.Partner.Find(partnerId);
                        if (partner != null)
                        {
                            // Удаляем связанные заявки и их позиции
                            var requests = context.Requests.Where(r => r.id_Partner == partnerId).ToList();
                            foreach (var request in requests)
                            {
                                // Удаляем позиции заявки
                                var requestItems = context.RequestItems.Where(ri => ri.id_Request == request.id).ToList();
                                foreach (var item in requestItems)
                                {
                                    context.RequestItems.Remove(item);
                                }
                                // Удаляем заявку
                                context.Requests.Remove(request);
                            }

                            // Удаляем партнера
                            context.Partner.Remove(partner);
                            context.SaveChanges();

                            ShowInfoMessage("Партнер удален", $"Партнер '{partnerName}' был успешно удален из системы.");
                            LoadPartners();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Ошибка удаления",
                        $"Не удалось удалить партнера: {ex.Message}\n\nВозможно, существуют связанные записи, препятствующие удалению.");
                }
            }
        }

        // Показать предлагаемую продукцию
        private void btnShowProducts_Click(object sender, RoutedEventArgs e)
        {
            if (dgPartners.SelectedItem == null)
            {
                ShowWarningMessage("Выберите партнера", "Пожалуйста, выберите партнера для просмотра предлагаемой продукции.");
                return;
            }

            dynamic selectedItem = dgPartners.SelectedItem;
            int partnerId = selectedItem.id;
            string partnerName = selectedItem.Name;

            PartnerProductsWindow productsWindow = new PartnerProductsWindow(partnerId, partnerName);
            productsWindow.Owner = this;
            productsWindow.ShowDialog();
        }

        // Обновить список
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPartners();
        }

        // Вспомогательные методы для показа сообщений
        private void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowWarningMessage(string title, string message)
        {
            MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowInfoMessage(string title, string message)
        {
            MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private MessageBoxResult ShowConfirmationMessage(string title, string message)
        {
            return MessageBox.Show(this, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        // Обработка двойного клика для редактирования
        private void dgPartners_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            btnEdit_Click(sender, e);
        }
    }
}