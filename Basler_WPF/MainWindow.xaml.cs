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

namespace Basler_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Emgu.CV.UI.ImageBox imageBox;
        private BaslerCamera baslerCamera;

        public MainWindow()
        {
            InitializeComponent();

            imageBox = new Emgu.CV.UI.ImageBox();
            baslerCamera = new BaslerCamera("192.168.1.6");
        }

        ~MainWindow()
        {
            baslerCamera.stop();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            wfhost.Child = imageBox;
        }

        private void Btn_snap_Click(object sender, RoutedEventArgs e)
        {
            baslerCamera.snapImage(imageBox, 400, 400, 0);
        }

        private void Btn_grab_Click(object sender, RoutedEventArgs e)
        {
            baslerCamera.grab(imageBox, 200, 200, 0, 0);
        }

        private void Btn_stop_Click(object sender, RoutedEventArgs e)
        {
            baslerCamera.stop();
        }
    }
}
