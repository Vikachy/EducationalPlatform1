using Microsoft.Maui.Controls;
using Microsoft.Maui;
using System;

namespace EducationalPlatform.Layouts
{
    public class WrapLayout : StackLayout
    {
        public static readonly BindableProperty SpacingProperty =
            BindableProperty.Create(nameof(Spacing), typeof(double), typeof(WrapLayout), 5.0,
                propertyChanged: (bindable, oldValue, newValue) => ((WrapLayout)bindable).OnSpacingChanged());

        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        private void OnSpacingChanged()
        {
            // Обновляем отступы
            InvalidateMeasure();
        }

        protected override void OnChildAdded(Element child)
        {
            base.OnChildAdded(child);
            if (child is View view)
            {
                // Подписываемся на изменение размера
                view.SizeChanged += OnChildSizeChanged;
            }
        }

        protected override void OnChildRemoved(Element child, int oldLogicalIndex)
        {
            base.OnChildRemoved(child, oldLogicalIndex);
            if (child is View view)
            {
                view.SizeChanged -= OnChildSizeChanged;
            }
        }

        private void OnChildSizeChanged(object? sender, EventArgs e)
        {
            // Пересчитываем макет при изменении размера дочернего элемента
            InvalidateMeasure();
        }
    }
}