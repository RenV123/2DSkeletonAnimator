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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _2DSkeletonAnimator.View
{
    /// <summary>
    /// Interaction logic for SlideButton.xaml
    /// </summary>
    public partial class SlideButton : UserControl
    {
        public Storyboard SlideForwardStoryBoard, SlideBackwardStoryBoard;


        public bool IsAnimationState
        {
            get
            {
                return (bool)GetValue(IsAnimationStateProperty);
            }
            set
            {
                SetValue(IsAnimationStateProperty, value);
            }
        }

        public static readonly DependencyProperty IsAnimationStateProperty =
        DependencyProperty.Register(
        "IsAnimationState", typeof(bool), typeof(SlideButton),
        new FrameworkPropertyMetadata(
            false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        //public bool IsRight;
        public SlideButton()
        {
            InitializeComponent();
            SlideForwardStoryBoard = FindResource("SlideBtnForwardStoryBoard") as Storyboard;
            SlideBackwardStoryBoard = FindResource("SlideBtnBackwardStoryBoard") as Storyboard;
        }
        public void Slidebtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (stpAnim.IsMouseOver)
            {
                SlideForwardStoryBoard.Begin();
                IsAnimationState = true;
            }
                
            else if(stpArma.IsMouseOver)
            {
                SlideBackwardStoryBoard.Begin();
                IsAnimationState = false;
            }
                
        }
    }
}
