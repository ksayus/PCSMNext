using LAE;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PCSMNext.App.Controls;

public class MyIconButton : Button
{
    // 覆盖默认样式键
    static MyIconButton() =>
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MyIconButton),
            new FrameworkPropertyMetadata(typeof(MyIconButton)));

    private static int _instanceCounter;
    private readonly string _scaleSlot;
    private ScaleTransform? _scaleTransform;

    public MyIconButton()
    {
        int id = ++_instanceCounter;
        _scaleSlot = $"__mib_scale_{id}__";
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _scaleTransform = GetTemplateChild("ScaleTransform") as ScaleTransform;
    }

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

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        AnimateTo(1.015, 150);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        AnimateTo(1.0, 150);
    }

    private void AnimateTo(double scale, double ms)
    {
        if (_scaleTransform == null) return;
        _ = LA.Builder(_scaleSlot).Scale(_scaleTransform, scale, ms, Easing.OutCubic).Play();
    }
}
