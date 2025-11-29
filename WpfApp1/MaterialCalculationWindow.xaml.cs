using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace WpfApp1
{
    public partial class MaterialCalculationWindow : Window
    {
        private int productId;
        private string productName;
        private int availableQuantity;

        public MaterialCalculationWindow(int productId, string productName, int availableQuantity)
        {
            InitializeComponent();
            this.productId = productId;
            this.productName = productName;
            this.availableQuantity = availableQuantity;

            this.Title = $"Расчет материалов - {productName}";
            txtProductName.Text = productName;
            txtAvailableQuantity.Text = availableQuantity.ToString();

            InitializeForm();
        }

        private void InitializeForm()
        {
            // Загрузка типов продукции и материалов
            var productTypes = MaterialCalculator.GetProductTypes();
            var materialTypes = MaterialCalculator.GetMaterialTypes();

            cmbProductType.ItemsSource = productTypes;
            cmbProductType.DisplayMemberPath = "Value";
            cmbProductType.SelectedValuePath = "Key";

            cmbMaterialType.ItemsSource = materialTypes;
            cmbMaterialType.DisplayMemberPath = "Value";
            cmbMaterialType.SelectedValuePath = "Key";

            // Установка значений по умолчанию
            txtParameter1.Text = "1.0";
            txtParameter2.Text = "1.0";
            txtRequiredQuantity.Text = "100";
            txtWarehouseQuantity.Text = availableQuantity.ToString();
        }

        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            try
            {
                int productTypeId = (int)cmbProductType.SelectedValue;
                int materialTypeId = (int)cmbMaterialType.SelectedValue;
                int requiredQuantity = int.Parse(txtRequiredQuantity.Text);
                int warehouseQuantity = int.Parse(txtWarehouseQuantity.Text);
                double parameter1 = double.Parse(txtParameter1.Text);
                double parameter2 = double.Parse(txtParameter2.Text);

                int result = MaterialCalculator.CalculateRequiredMaterial(
                    productTypeId, materialTypeId, requiredQuantity,
                    warehouseQuantity, parameter1, parameter2);

                if (result == -1)
                {
                    ShowErrorMessage("Ошибка расчета",
                        "Проверьте правильность введенных данных. Возможно, указаны несуществующие типы продукции или материалов.");
                }
                else
                {
                    txtResult.Text = result.ToString();
                    ShowInfoMessage("Расчет завершен",
                        $"Для производства {requiredQuantity} единиц продукции '{productName}' " +
                        $"требуется {result} единиц материала.");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка", $"Произошла ошибка при расчете: {ex.Message}");
            }
        }

        private bool ValidateInput()
        {
            // Проверка типа продукции
            if (cmbProductType.SelectedValue == null)
            {
                ShowValidationError("Выберите тип продукции");
                cmbProductType.Focus();
                return false;
            }

            // Проверка типа материала
            if (cmbMaterialType.SelectedValue == null)
            {
                ShowValidationError("Выберите тип материала");
                cmbMaterialType.Focus();
                return false;
            }

            // Проверка требуемого количества
            if (!int.TryParse(txtRequiredQuantity.Text, out int reqQty) || reqQty <= 0)
            {
                ShowValidationError("Требуемое количество должно быть положительным целым числом");
                txtRequiredQuantity.Focus();
                return false;
            }

            // Проверка количества на складе
            if (!int.TryParse(txtWarehouseQuantity.Text, out int whQty) || whQty < 0)
            {
                ShowValidationError("Количество на складе должно быть неотрицательным целым числом");
                txtWarehouseQuantity.Focus();
                return false;
            }

            // Проверка параметров
            if (!double.TryParse(txtParameter1.Text, out double param1) || param1 <= 0)
            {
                ShowValidationError("Параметр 1 должен быть положительным числом");
                txtParameter1.Focus();
                return false;
            }

            if (!double.TryParse(txtParameter2.Text, out double param2) || param2 <= 0)
            {
                ShowValidationError("Параметр 2 должен быть положительным числом");
                txtParameter2.Focus();
                return false;
            }

            return true;
        }

        private void ShowValidationError(string message)
        {
            MessageBox.Show(this, message, "Ошибка валидации",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(this, message, title,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowInfoMessage(string title, string message)
        {
            MessageBox.Show(this, message, title,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Валидация числового ввода
        private void NumericTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string newText = textBox.Text + e.Text;

            if (textBox.Name == "txtParameter1" || textBox.Name == "txtParameter2")
            {
                // Для вещественных чисел
                e.Handled = !double.TryParse(newText, out _);
            }
            else
            {
                // Для целых чисел
                e.Handled = !int.TryParse(newText, out _);
            }
        }
    }
}
