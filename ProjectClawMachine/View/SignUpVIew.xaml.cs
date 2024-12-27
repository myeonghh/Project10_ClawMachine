﻿using ProjectClawMachine.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace ProjectClawMachine.View
{
    /// <summary>
    /// SignUpVIew.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SignUpView : UserControl
    {
        public SignUpView()
        {
            InitializeComponent();
        }

        private void OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModel.SignUpViewModel viewModel)
            {
                viewModel.SignUpData.Password = (sender as PasswordBox)?.Password;
            }
        }
    }
}
