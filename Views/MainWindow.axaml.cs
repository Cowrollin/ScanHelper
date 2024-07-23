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
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.MyEvent += UpdateImage;
        }
    }

    private void UpdateImage(object? sender, EventArgs e)
    {
        BaseImage.InvalidateVisual();
    }

    //BaseImage.InvalidateVisual();

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        
    }
}