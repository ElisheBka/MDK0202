using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Text.RegularExpressions;

namespace WpfApp1
{
    public partial class PartnerFormWindow : Window
    {
        private int? partnerId = null;
        private bool isEditMode = false;

        public PartnerFormWindow()
        {
            InitializeComponent();
            InitializeForm();
        }

        public PartnerFormWindow(int id) : this()
        {
            partnerId = id;
            isEditMode = true;
            this.Title = "Редактирование партнера";
            LoadPartnerData();
        }

        private void InitializeForm()
        {
            LoadPartnerTypes();
            if (!isEditMode)
            {
                txtRating.Text = "0";
            }
        }

        // Загрузка типов партнеров
        private void LoadPartnerTypes()
        {
            try
            {
                using (var context = new Entities())
                {
                    var types = context.TypePartner
                        .OrderBy(t => t.Name)
                        .ToList();

                    cmbPartnerType.ItemsSource = types;
                    cmbPartnerType.DisplayMemberPath = "Name";
                    cmbPartnerType.SelectedValuePath = "id";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов партнеров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка данных партнера для редактирования
        private void LoadPartnerData()
        {
            try
            {
                using (var context = new Entities())
                {
                    var partner = context.Partner.Find(partnerId);
                    if (partner != null)
                    {
                        cmbPartnerType.SelectedValue = partner.id_TypePartner;
                        txtName.Text = partner.Name;
                        txtDirector.Text = partner.Director;
                        txtAddress.Text = partner.Adress;
                        txtRating.Text = partner.Rating.ToString();
                        txtPhone.Text = partner.NumberPartner;
                        txtEmail.Text = partner.email ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных партнера: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = false;
                this.Close();
            }
        }

        // Валидация данных
        private bool ValidateData()
        {
            // Проверка типа партнера
            if (cmbPartnerType.SelectedValue == null)
            {
                ShowValidationError("Выберите тип партнера");
                cmbPartnerType.Focus();
                return false;
            }

            // Проверка наименования
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ShowValidationError("Введите наименование компании");
                txtName.Focus();
                return false;
            }

            if (txtName.Text.Length < 2)
            {
                ShowValidationError("Наименование компании должно содержать не менее 2 символов");
                txtName.Focus();
                return false;
            }

            // Проверка ФИО директора
            if (string.IsNullOrWhiteSpace(txtDirector.Text))
            {
                ShowValidationError("Введите ФИО директора");
                txtDirector.Focus();
                return false;
            }

            // Проверка рейтинга
            if (!int.TryParse(txtRating.Text, out int rating) || rating < 0)
            {
                ShowValidationError("Рейтинг должен быть целым неотрицательным числом");
                txtRating.Focus();
                return false;
            }

            // Проверка телефона
            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                ShowValidationError("Введите номер телефона");
                txtPhone.Focus();
                return false;
            }

            // Проверка email
            if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !IsValidEmail(txtEmail.Text))
            {
                ShowValidationError("Введите корректный email адрес");
                txtEmail.Focus();
                return false;
            }

            return true;
        }

        // Проверка валидности email
        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private void ShowValidationError(string message)
        {
            MessageBox.Show(this, message, "Ошибка валидации",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Сохранение партнера
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateData())
            {
                return;
            }

            try
            {
                using (var context = new Entities())
                {
                    if (isEditMode)
                    {
                        // Обновление существующего партнера
                        var partner = context.Partner.Find(partnerId);
                        if (partner != null)
                        {
                            partner.id_TypePartner = (int)cmbPartnerType.SelectedValue;
                            partner.Name = txtName.Text.Trim();
                            partner.Director = txtDirector.Text.Trim();
                            partner.Adress = txtAddress.Text.Trim();
                            partner.Rating = int.Parse(txtRating.Text);
                            partner.NumberPartner = txtPhone.Text.Trim();
                            partner.email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
                        }
                    }
                    else
                    {
                        // Добавление нового партнера
                        var newPartner = new Partner
                        {
                            id_TypePartner = (int)cmbPartnerType.SelectedValue,
                            Name = txtName.Text.Trim(),
                            Director = txtDirector.Text.Trim(),
                            Adress = txtAddress.Text.Trim(),
                            Rating = int.Parse(txtRating.Text),
                            NumberPartner = txtPhone.Text.Trim(),
                            email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim()
                        };
                        context.Partner.Add(newPartner);
                    }

                    context.SaveChanges();
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Отмена
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Валидация ввода рейтинга - только цифры
        private void txtRating_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private bool IsTextAllowed(string text)
        {
            return Array.TrueForAll(text.ToCharArray(), c => char.IsDigit(c));
        }

        // Подсказка при наведении на поля
        private void TextBox_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string tooltip = "";

                switch (textBox.Name)
                {
                    case "txtName":
                        tooltip = "Введите полное наименование компании";
                        break;
                    case "txtDirector":
                        tooltip = "Введите ФИО директора в формате: Иванов Иван Иванович";
                        break;
                    case "txtAddress":
                        tooltip = "Введите юридический адрес компании";
                        break;
                    case "txtRating":
                        tooltip = "Рейтинг должен быть целым неотрицательным числом";
                        break;
                    case "txtPhone":
                        tooltip = "Введите номер телефона в любом формате";
                        break;
                    case "txtEmail":
                        tooltip = "Введите email адрес (необязательно)";
                        break;
                    default:
                        tooltip = "";
                        break;
                }

                textBox.ToolTip = tooltip;
            }
        }
    }
}