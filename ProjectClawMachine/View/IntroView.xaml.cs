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
using WpfAnimatedGif;

namespace ProjectClawMachine.View
{
    /// <summary>
    /// IntroView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class IntroView : UserControl
    {
        public IntroView()
        {
            InitializeComponent();

            var gif = new BitmapImage();
            gif.BeginInit();
            gif.UriSource = new Uri("pack://application:,,,/View/Resource/start.gif", UriKind.Absolute);
            gif.EndInit();

            // WpfAnimatedGif로 애니메이션 적용
            ImageBehavior.SetAnimatedSource(backgroundGif, gif);
        }
    }
}
