using System;
using Avalonia.Controls;
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
}