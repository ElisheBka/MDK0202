using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    public static class MaterialCalculator
    {
        // Коэффициенты для разных типов продукции
        private static readonly Dictionary<int, double> ProductTypeCoefficients = new Dictionary<int, double>
        {
            {1, 1.2},  // Электроника
            {2, 1.0},  // Мебель
            {3, 1.5},  // Одежда
            {4, 0.8}   // Канцелярия
        };

        // Процент брака для разных типов материалов
        private static readonly Dictionary<int, double> MaterialDefectPercentages = new Dictionary<int, double>
        {
            {1, 0.05},  // Металл - 5% брака
            {2, 0.10},  // Дерево - 10% брака
            {3, 0.03},  // Пластик - 3% брака
            {4, 0.08},  // Ткань - 8% брака
            {5, 0.02}   // Стекло - 2% брака
        };

        /// <summary>
        /// Расчет количества материала, необходимого для производства продукции
        /// </summary>
        /// <param name="productTypeId">Идентификатор типа продукции</param>
        /// <param name="materialTypeId">Идентификатор типа материала</param>
        /// <param name="requiredQuantity">Требуемое количество продукции</param>
        /// <param name="warehouseQuantity">Количество продукции на складе</param>
        /// <param name="parameter1">Первый параметр продукции (вещественное положительное число)</param>
        /// <param name="parameter2">Второй параметр продукции (вещественное положительное число)</param>
        /// <returns>Количество необходимого материала или -1 при ошибке</returns>
        public static int CalculateRequiredMaterial(
            int productTypeId,
            int materialTypeId,
            int requiredQuantity,
            int warehouseQuantity,
            double parameter1,
            double parameter2)
        {
            try
            {
                // Валидация входных параметров
                if (productTypeId <= 0 || materialTypeId <= 0 ||
                    requiredQuantity <= 0 || warehouseQuantity < 0 ||
                    parameter1 <= 0 || parameter2 <= 0)
                {
                    return -1;
                }

                // Проверка существования типа продукции
                if (!ProductTypeCoefficients.ContainsKey(productTypeId))
                {
                    return -1;
                }

                // Проверка существования типа материала
                if (!MaterialDefectPercentages.ContainsKey(materialTypeId))
                {
                    return -1;
                }

                // Расчет количества продукции для производства (с учетом наличия на складе)
                int productionQuantity = requiredQuantity - warehouseQuantity;
                if (productionQuantity <= 0)
                {
                    return 0; // Вся продукция уже есть на складе
                }

                // Получение коэффициентов
                double productCoefficient = ProductTypeCoefficients[productTypeId];
                double defectPercentage = MaterialDefectPercentages[materialTypeId];

                // Расчет материала на одну единицу продукции
                double materialPerUnit = parameter1 * parameter2 * productCoefficient;

                // Расчет общего количества материала с учетом брака
                double totalMaterial = materialPerUnit * productionQuantity * (1 + defectPercentage);

                // Округление до целого числа в большую сторону
                return (int)Math.Ceiling(totalMaterial);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        // Метод для получения информации о типах продукции (для UI)
        public static Dictionary<int, string> GetProductTypes()
        {
            try
            {
                using (var context = new Entities())
                {
                    return context.TypeProduct
                        .OrderBy(t => t.Name)
                        .ToDictionary(t => t.id, t => t.Name);
                }
            }
            catch
            {
                return new Dictionary<int, string>();
            }
        }

        // Метод для получения информации о типах материалов (для UI)
        public static Dictionary<int, string> GetMaterialTypes()
        {
            try
            {
                using (var context = new Entities())
                {
                    return context.TypeProduct
                        .OrderBy(t => t.Name)
                        .ToDictionary(t => t.id, t => t.Name);
                }
            }
            catch
            {
                return new Dictionary<int, string>();
            }
        }
    }
}
