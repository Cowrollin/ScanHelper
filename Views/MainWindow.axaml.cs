using System;
using System.Reflection.Metadata;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ScanHelper.ViewModels;

namespace ScanHelper.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    //BaseImage.InvalidateVisual();

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        
    }
}