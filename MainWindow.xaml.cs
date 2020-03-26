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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
namespace PicBrowser
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 

    //public class ViewModel
    //{
    //    MyImage Imageobj { get; set; }
    //    public ViewModel()
    //    {
    //        Imageobj = new MyImage();
    //    }
    //}



    public partial class MainWindow : Window
    {

        //public ViewModel viewmodel;
        private bool IsFullScreen { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();
            IsFullScreen = false;

            //viewmodel = new ViewModel();
        }

        private void FileChoose_Click(object sender, RoutedEventArgs e)
        {
            var openFile = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "Image files (.jpg;.png;.ico;.jpeg;.bmp)|*.jpg;*.png;*.ico;*.jpeg;*.bmp|All Files(*.*)|*.*"
            };
            var Result = openFile.ShowDialog();
            if (Result == true)
            {
                ImageBrowser.SetSource(openFile.FileName);
            }
            FullScreenButton.Visibility = Visibility.Visible;
            ImageZoom.Visibility = Visibility.Visible;
            ZoomValueLabel.Visibility = Visibility.Visible;
        }

        private void FullScreen_Click(object sender, RoutedEventArgs e)
        { 
            this.Topmost = true;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;

            this.FileChooseButton.Visibility = Visibility.Collapsed;
            this.FullScreenButton.Visibility = Visibility.Collapsed;
            this.ImageZoom.Visibility = Visibility.Collapsed;
            this.ZoomValueLabel.Visibility = Visibility.Collapsed;
            Keyboard.Focus(MainFrame);
            IsFullScreen = true;
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && IsFullScreen)
            {
                this.Topmost = false;
                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                IsFullScreen = false;
                this.FileChooseButton.Visibility = Visibility.Visible;
                this.FullScreenButton.Visibility = Visibility.Visible;
                this.ImageZoom.Visibility = Visibility.Visible;
                this.ZoomValueLabel.Visibility = Visibility.Visible;
            }
            else if (e.Key == Key.Escape&&!IsFullScreen)
            {
                if(MessageBox.Show("Are you sure to quit?","Tips", MessageBoxButton.OKCancel)==MessageBoxResult.OK)
                   Close();
            }
        }

        private void ImageZoom_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                ImageBrowser.ZoomValue = (int)ImageZoom.Value;
            }
        }


    }
}
