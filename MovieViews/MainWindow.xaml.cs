﻿using System.Windows;

namespace MovieViews
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            this.DataContext = new ViewModels.MainWindowViewModel();
        }
    }
}
