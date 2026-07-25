using System.Windows;
using System.Windows.Controls;

namespace PCSMNext.App.Controls;

public class MyCard : ContentControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string),
            typeof(MyCard), new PropertyMetadata(""));

    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(nameof(IsCollapsed), typeof(bool),
            typeof(MyCard), new PropertyMetadata(false));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    static MyCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MyCard),
            new FrameworkPropertyMetadata(typeof(MyCard)));
    }
}