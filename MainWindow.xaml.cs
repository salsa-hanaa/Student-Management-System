using System.Windows;
using StudentManager.ViewModels;

namespace StudentManager;

/// <summary>
/// Code-behind hanya berisi InitializeComponent dan DataContext.
/// Seluruh logika ada di StudentViewModel (MVVM).
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new StudentViewModel();
    }
}
