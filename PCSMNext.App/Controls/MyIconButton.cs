using System.Windows;
using System.Windows.Controls;

namespace PCSMNext.App.Controls;

public class MyIconButton : Button
{
    // 覆盖默认样式键
    static MyIconButton() => DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MyIconButton),
            new FrameworkPropertyMetadata(typeof(MyIconButton))
            );

    // 定义依赖属性

    // IconText
    public static readonly DependencyProperty IconTextProperty =
        DependencyProperty.Register("IconText", typeof(string), typeof(MyIconButton));

    public string IconText
    {
        get {  return (string)GetValue(IconTextProperty); }
        set { SetValue(IconTextProperty, value); }
    }

    // CornerRadius
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(MyIconButton));

    public CornerRadius CornerRadius
    {
        get { return (CornerRadius)GetValue(CornerRadiusProperty); }
        set { SetValue(CornerRadiusProperty, value); }
    }
}
