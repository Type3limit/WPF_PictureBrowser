using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;


namespace PicBrowser
{
    /// <summary>
    /// MyImage.xaml 的交互逻辑
    /// </summary>
    ///
    #region MVVM
    //class NotificationObject : INotifyPropertyChanged
    //{
    //    public event PropertyChangedEventHandler PropertyChanged;
    //    public void RaisePropertyChanged(string propertyName)
    //    {
    //        if(this.PropertyChanged!=null)
    //        {
    //            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    //        }
    //    }
    //}


    //class DelegateCommand :ICommand
    //{
    //    public Action<Object> ExecuteCommand = null;
    //    public Func<Object, bool> CanExecuteCommand = null;
    //    public event EventHandler CanExecuteChanged;
    //    public bool CanExecute(object parameter)
    //    {
    //        if (CanExecuteCommand != null)
    //        {
    //            return this.CanExecuteCommand(parameter);
    //        }
    //        else
    //            return true;
    //    }
    //    public void Execute(object parameter)
    //    {
    //        if (this.ExecuteCommand != null) this.ExecuteCommand(parameter);
    //    }

    //    public void RaiseCanExecuteChanged()
    //    {
    //        if(CanExecuteChanged !=null)
    //        {
    //            CanExecuteChanged(this, EventArgs.Empty);
    //        }
    //    }

    //}


    //class Model : NotificationObject
    //{
    //    private int _zoomValue = 0;
    //    public int ZoomValue
    //    {
    //        get { return _zoomValue; }
    //        set
    //        {
    //            _zoomValue = value;
    //            this.RaisePropertyChanged("ZoomValue");
    //        }
    //    }
    //    public void Zoom(Object obj)
    //    {
    //        int a = (int)obj;
    //        ZoomValue = a;
    //    }
    //}

    //class ViewModel
    //{
    //    public DelegateCommand ZoomChanged { get; set; }
    //    public Model model { set; get; }
    //    public ViewModel()
    //    {
    //        this.model = new Model();
    //        this.ZoomChanged = new DelegateCommand();
    //        this.ZoomChanged.ExecuteCommand = new Action<Object>(this.model.Zoom);
    //    }
    //}
    #endregion MVVM

    public partial class MyImage : UserControl
    {


        public delegate void ZoomValueChanged(int CurrentValue);
        public event ZoomValueChanged OnZoomChanged;//放缩比例改变事件

        private string ImagePath;//文件路径
        private BitmapImage Image;//文件生成的Bitmap


        public int ZoomValue//依赖属性
        {

            set
            {

                OnZoomChanged(value);
                SetValue(ZoomValueProperty, value);

            }
            get { return (int)GetValue(ZoomValueProperty); }
        }

        #region member
        private Point CurWheelCenterPoint { set; get; }//放缩前中心点位
        private Point OriginCenterPoint { set; get; }//上一次的中心点位
        private Point ZoomedCenterPoint { set; get; }//放缩后的中心点位

        private Point CurPoint { set; get; }//当前鼠标位置

        private Rect ImageRect { set; get; }//图像区域
        private Rect BorderRect { set; get; }//包含图像的区域
        private Rect OriginRect { set; get; }//初始图像矩形




        private bool IsZoomed { set; get; }//是否被缩放
        private bool IsDrag { set; get; }//是否移动

        //判断是否触及四个边界
        private bool IsReachTop;
        private bool IsReachBottom;
        private bool IsReachLeft;
        private bool IsReachRight;


        #endregion member


        //注册依赖
        public static DependencyProperty ZoomValueProperty = DependencyProperty.Register("ZoomValue",
            typeof(int), typeof(MyImage), new FrameworkPropertyMetadata(0) { BindsTwoWayByDefault = true });

        public MyImage()
        {
            InitializeComponent();
            ImagePath = null;
            OnZoomChanged += new ZoomValueChanged(ZoomPic);

            IsReachBottom = IsReachLeft = IsReachRight = IsReachTop = IsZoomed = IsDrag = false;

        }

        public void SetSource(string Path)//设定图像文件路径
        {

            ImagePath = Path;
            Image = new BitmapImage(new Uri(ImagePath));
            ImageArea.Source = Image;
            ZoomValue = 0;

        }


        private void DoPosJudge()
        //矫正图像使留白正确
        {
            //获得图形区域的矩形
            ImageRect = ImageArea.TransformToAncestor(this).TransformBounds(new Rect(ImageArea.RenderSize));
            //获取Border的矩形
            BorderRect = ViewPort.TransformToAncestor(this).TransformBounds(new Rect(ViewPort.RenderSize));

            IsReachTop = ImageRect.Top >= BorderRect.Top ? true : false;
            IsReachBottom = ImageRect.Bottom <= BorderRect.Bottom ? true : false;
            IsReachLeft = ImageRect.Left >= BorderRect.Left ? true : false;
            IsReachRight = ImageRect.Right <= BorderRect.Right ? true : false;


            //Duration duration = new Duration(new TimeSpan(0, 0, 0, 0, 125));
            //DoubleAnimation doubleAnimationX = new DoubleAnimation(TLT.X, TLT.X, duration);
            //DoubleAnimation doubleAnimationY = new DoubleAnimation(TLT.Y, TLT.Y, duration);
            //x轴判断
            if (IsReachLeft && IsReachRight)//两侧皆越界
            {
                double DiffL = ImageRect.Left - BorderRect.Left;
                double DiffR = BorderRect.Right - ImageRect.Right;
                if (DiffL > DiffR)//左侧越界大于右侧
                {
                    double Delta = (DiffL - DiffR) * 0.5d;
                    CurWheelCenterPoint = new Point(CurWheelCenterPoint.X - Delta, CurWheelCenterPoint.Y);
                    //doubleAnimationX = new DoubleAnimation(TLT.X, TLT.X - Delta, duration);
                    TLT.X -= Delta;
                }
                else// 右侧越界大于或等于左侧
                {
                    double Delta = (DiffR - DiffL) * 0.5d;
                    CurWheelCenterPoint = new Point(CurWheelCenterPoint.X + Delta, CurWheelCenterPoint.Y);
                    //doubleAnimationX = new DoubleAnimation(TLT.X, TLT.X + Delta, duration);
                    TLT.X += Delta;
                }
            }
            else//一侧越界或未越界
            {
                if (IsReachLeft && !IsReachRight)//仅左侧越界
                {
                    double DiffL = ImageRect.Left - BorderRect.Left;
                    double DiffR = ImageRect.Right - BorderRect.Right;
                    if (DiffL > DiffR)//左侧空缺不足以被填补
                    {
                        double Delta = (DiffL - DiffR) * 0.5d;
                        CurWheelCenterPoint = new Point(CurWheelCenterPoint.X - Delta - DiffR, CurWheelCenterPoint.Y);
                        //doubleAnimationX = new DoubleAnimation(TLT.X, TLT.X - Delta - DiffR, duration);
                        TLT.X -= (Delta+DiffR);
                    }
                    else//足以被填补
                    {
                        CurWheelCenterPoint = new Point(CurWheelCenterPoint.X - DiffL, CurWheelCenterPoint.Y);
                        //doubleAnimationX = new DoubleAnimation(TLT.X, TLT.X - DiffL, duration);
                        TLT.X -= DiffL;
                    }

                }
                else if (!IsReachLeft && IsReachRight)//仅右侧越界
                {
                    double DiffR = BorderRect.Right - ImageRect.Right;
                    double DiffL = BorderRect.Left - ImageRect.Left;
                    if (DiffL < DiffR)//右侧空缺不足以被填补
                    {
                        double Delta = (DiffR - DiffL) * 0.5d;
                        CurWheelCenterPoint = new Point(CurWheelCenterPoint.X + Delta + DiffL, CurWheelCenterPoint.Y);
                        //doubleAnimationX = new DoubleAnimation(TLT.X, TLT.X + Delta + DiffL, duration);
                        TLT.Y += Delta + DiffL;
                    }
                    else
                    {
                        CurWheelCenterPoint = new Point(CurWheelCenterPoint.X + DiffR, CurWheelCenterPoint.Y);
                        //doubleAnimationX = new DoubleAnimation(TLT.X, TLT.X + DiffR, duration);
                        TLT.X += DiffR;
                    }
                }
                //未越界则不做处理

            }
            //y轴判断
            if (IsReachTop && IsReachBottom)
            {
                double DiffT = ImageRect.Top - BorderRect.Top;
                double DiffB = BorderRect.Bottom - ImageRect.Bottom;
                if (DiffT > DiffB)//顶侧越界大于底侧
                {
                    double Delta = (DiffT - DiffB) * 0.5d;
                    CurWheelCenterPoint = new Point(CurWheelCenterPoint.X, CurWheelCenterPoint.Y - Delta);
                    //doubleAnimationY = new DoubleAnimation(TLT.Y, TLT.Y - Delta, duration);
                    TLT.Y -= Delta;
                }
                else// 底侧越界大于或等于左侧
                {
                    double Delta = (DiffB - DiffT) * 0.5d;

                    CurWheelCenterPoint = new Point(CurWheelCenterPoint.X, CurWheelCenterPoint.Y + Delta);
                    //doubleAnimationY = new DoubleAnimation(TLT.Y, TLT.Y + Delta, duration);
                    TLT.Y += Delta;
                }
            }
            else
            {
                if (IsReachTop && !IsReachBottom)//仅顶侧越界
                {
                    double DiffT = ImageRect.Top - BorderRect.Top;
                    double DiffB = ImageRect.Bottom - BorderRect.Bottom;
                    if (DiffT > DiffB)//顶侧空缺不足以被填补
                    {
                        double Delta = (DiffT - DiffB) * 0.5d;

                        CurWheelCenterPoint = new Point(CurWheelCenterPoint.X, CurWheelCenterPoint.Y - Delta - DiffB);
                        //doubleAnimationY = new DoubleAnimation(TLT.Y, TLT.Y - Delta - DiffB, duration);
                        TLT.Y -= (Delta + DiffB);
                    }
                    else//足以被填补
                    {

                        CurWheelCenterPoint = new Point(CurWheelCenterPoint.X, CurWheelCenterPoint.Y - DiffT);
                        //doubleAnimationY = new DoubleAnimation(TLT.Y, TLT.Y - DiffT, duration);
                        TLT.Y -= DiffT;
                    }

                }
                else if (!IsReachTop && IsReachBottom)//仅底侧越界
                {
                    double DiffB = BorderRect.Bottom - ImageRect.Bottom;
                    double DiffT = BorderRect.Top - ImageRect.Top;
                    if (DiffT < DiffB)//底侧空缺不足以被填补
                    {
                        double Delta = (DiffB - DiffT) * 0.5d;

                        CurWheelCenterPoint = new Point(CurWheelCenterPoint.X, CurWheelCenterPoint.Y + Delta + DiffB);
                        //doubleAnimationY = new DoubleAnimation(TLT.Y, TLT.Y + Delta, duration);
                        TLT.Y += Delta;
                    }
                    else//足以被填补
                    {

                        CurWheelCenterPoint = new Point(CurWheelCenterPoint.X, CurWheelCenterPoint.Y + DiffB);
                        //doubleAnimationY = new DoubleAnimation(TLT.Y, TLT.Y + DiffB, duration);
                        TLT.Y += DiffB;
                    }
                }
                //未越界则不做处理

            }
            //TLT.BeginAnimation(TranslateTransform.XProperty, doubleAnimationX);
            //TLT.BeginAnimation(TranslateTransform.YProperty, doubleAnimationY);
        }
        
        //(Before 2020/03/26 this is a forbidden function) 
        private void _ZoomPic(int CurrentValue)//Zoom with default scale transform center and animation
           
        {
            bool model = CurrentValue > ZoomValue ? true : false;
            IsZoomed = (CurrentValue != 0 ? true : false);
            double DeltaScale = CurrentValue / 100.0d;

            Duration duration = new Duration(new TimeSpan(0, 0, 0, 0, 125));

            //获得图形区域的矩形
            ImageRect = ImageArea.TransformToAncestor(this).TransformBounds(new Rect(ImageArea.RenderSize));
            //获取Border的矩形
            BorderRect = ViewPort.TransformToAncestor(this).TransformBounds(new Rect(ViewPort.RenderSize));


            if (ZoomValue == 0)
            {
                OriginRect = ImageRect;
            }



            ImageRect = ImageArea.TransformToVisual(ImageArea).TransformBounds(new Rect(ImageArea.RenderSize));
            BorderRect = ViewPort.TransformToVisual(ImageArea).TransformBounds(new Rect(ViewPort.RenderSize));




            IsReachTop = ImageRect.Top >= BorderRect.Top ? true : false;
            IsReachBottom = ImageRect.Bottom <= BorderRect.Bottom ? true : false;

            IsReachLeft = ImageRect.Left >= BorderRect.Left ? true : false;
            IsReachRight = ImageRect.Right <= BorderRect.Right ? true : false;

            DoubleAnimation doubleAnimationScale = new DoubleAnimation((1 + DeltaScale), duration);


            Point CurCenter = new Point();

            double BorderCenterX = (BorderRect.Right + BorderRect.Left) * 0.5d;
            double BorderCenterY = (BorderRect.Bottom + BorderRect.Top) * 0.5d;

            Vector PointDiff = new Vector();
            Point PointOfOrigin = ImageGroup.Inverse.Transform(CurWheelCenterPoint);
            ZoomedCenterPoint = new Point(PointOfOrigin.X * (1 + DeltaScale), PointOfOrigin.Y * (1 + DeltaScale));
            PointDiff = ZoomedCenterPoint - CurWheelCenterPoint;


            DoubleAnimation doubleAnimationCenterX = new DoubleAnimation(BorderCenterX, duration);
            DoubleAnimation doubleAnimationCenterY = new DoubleAnimation(BorderCenterY, duration);

            if (CurrentValue == 0)
            {

                Duration Recovery = new Duration(new TimeSpan(0, 0, 0, 0, 125));
                DoubleAnimation doubleAnimationRX = new DoubleAnimation(TLT.X, 0, Recovery);
                DoubleAnimation doubleAnimationRY = new DoubleAnimation(TLT.Y, 0, Recovery);
                TLT.BeginAnimation(TranslateTransform.XProperty, doubleAnimationRX);
                TLT.BeginAnimation(TranslateTransform.YProperty, doubleAnimationRY);
                STF.BeginAnimation(ScaleTransform.ScaleXProperty, doubleAnimationScale);
                STF.BeginAnimation(ScaleTransform.ScaleYProperty, doubleAnimationScale);
                STF.BeginAnimation(ScaleTransform.CenterXProperty, doubleAnimationCenterX);
                STF.BeginAnimation(ScaleTransform.CenterYProperty, doubleAnimationCenterY);
                //DoPosJudge();

                return;

            }




            DoubleAnimation doubleAnimationX = new DoubleAnimation(TLT.X, TLT.X, duration);
            DoubleAnimation doubleAnimationY = new DoubleAnimation(TLT.Y, TLT.Y, duration);

            // X轴中心点选择
            if (!IsReachLeft && !IsReachRight)
            {
                double DiffX = Math.Abs(OriginCenterPoint.X - CurWheelCenterPoint.X);
                double DiffL = BorderRect.Left - ImageRect.Left;
                double DiffR = ImageRect.Right - BorderRect.Right;

                if (CurWheelCenterPoint.X < BorderCenterX)
                {
                    if (DiffX > DiffL)
                        CurCenter.X = CurWheelCenterPoint.X + (DiffX - DiffL);
                    else
                        CurCenter.X = CurWheelCenterPoint.X;
                }
                else
                {
                    if (DiffX > DiffR)
                        CurCenter.X = CurWheelCenterPoint.X - (DiffX - DiffR);
                    else
                        CurCenter.X = CurWheelCenterPoint.X;
                }

            }



            else if (IsReachLeft && !IsReachRight)
            {
                //double diff = ImageRect.Left - BorderRect.Left;
                //doubleAnimationX = new DoubleAnimation(TLT.X, TLT.X - diff, duration);
                CurCenter.X = OriginCenterPoint.X ;
            }
            else if (!IsReachLeft && IsReachRight)
            {
                //double diff = BorderRect.Right - ImageRect.Right;
                //doubleAnimationX = new DoubleAnimation(TLT.X, TLT.X + diff, duration);
                CurCenter.X = OriginCenterPoint.X;
            }
            else
            {

                CurCenter.X = BorderCenterX;

            }



            //Y轴中心点选择
            if (!IsReachTop && !IsReachBottom)
            {
                double DiffY = Math.Abs(OriginCenterPoint.Y - CurWheelCenterPoint.Y);
                double DiffT = BorderRect.Top - ImageRect.Top;
                double DiffB = ImageRect.Bottom - BorderRect.Bottom;

                if (CurWheelCenterPoint.Y < BorderCenterY)
                {
                    if (DiffY > DiffT)
                        CurCenter.Y = CurWheelCenterPoint.Y + (DiffY - DiffT);
                    else
                        CurCenter.Y = CurWheelCenterPoint.Y;
                }
                else
                {
                    if (DiffY > DiffB)
                        CurCenter.Y = CurWheelCenterPoint.Y - (DiffY - DiffB);
                    else
                        CurCenter.Y = CurWheelCenterPoint.Y;
                }
            }
            else if (IsReachTop && !IsReachBottom)
            {
                //double diff = ImageRect.Top - BorderRect.Top;
                //doubleAnimationY = new DoubleAnimation(TLT.Y, TLT.Y - diff, duration);
                CurCenter.Y = OriginCenterPoint.Y;
            }
            else if (IsReachBottom && !IsReachTop)
            {
                //double diff = BorderRect.Bottom - ImageRect.Bottom;
                //doubleAnimationY = new DoubleAnimation(TLT.Y, TLT.Y + diff, duration);
                CurCenter.Y = OriginCenterPoint.Y;
            }
            else
            {
                CurCenter.Y = BorderCenterY;
            }






            OriginCenterPoint = new Point(CurCenter.X, CurCenter.Y);

            /* ScaleTransform Center是以Image为参考坐标系*/
            //CurCenter = this.TransformToVisual(ImageArea).Transform(CurCenter);

            doubleAnimationCenterX = new DoubleAnimation(CurCenter.X, duration);
            doubleAnimationCenterY = new DoubleAnimation(CurCenter.Y, duration);





            STF.BeginAnimation(ScaleTransform.ScaleXProperty, doubleAnimationScale);
            STF.BeginAnimation(ScaleTransform.ScaleYProperty, doubleAnimationScale);

            STF.BeginAnimation(ScaleTransform.CenterXProperty, doubleAnimationCenterX);
            STF.BeginAnimation(ScaleTransform.CenterYProperty, doubleAnimationCenterY);



            ImageRect = ImageArea.TransformToAncestor(this).TransformBounds(new Rect(ImageArea.RenderSize));
            BorderRect = ViewPort.TransformToAncestor(this).TransformBounds(new Rect(ViewPort.RenderSize));

            CurCenter = ImageArea.TransformToAncestor(this).Transform(CurCenter);


            double IL = CurCenter.X - ImageRect.Left;
            double IR = ImageRect.Right - CurCenter.X;
            double IT = CurCenter.Y - ImageRect.Top;
            double IB = ImageRect.Bottom - CurCenter.Y;

            double afterZoomL = IL * (1 + DeltaScale);
            double afterZoomR = IR * (1 + DeltaScale);
            double afterZoomT = IT * (1 + DeltaScale);
            double afterZoomB = IB * (1 + DeltaScale);

            double ZoomedWidth = OriginRect.Width * (1 + DeltaScale);
            double ZoomedHeight = OriginRect.Height * (1 + DeltaScale);

            if(ZoomedWidth >= BorderRect.Width)
            {
                if (CurCenter.X - afterZoomL > BorderRect.Left)
                {
                    double diff = (CurCenter.X - afterZoomL) - BorderRect.Left;
                    doubleAnimationX = new DoubleAnimation(TLT.X, TLT.X - diff, duration);
                }

                else if (CurCenter.X + afterZoomR < BorderRect.Right)
                {
                    double diff = BorderRect.Right - (CurCenter.X + afterZoomR);
                    doubleAnimationX = new DoubleAnimation(TLT.X, TLT.X + diff, duration);
                }
            }

            if(ZoomedHeight>=BorderRect.Height)
            {
                if(CurCenter.Y - afterZoomT > BorderRect.Top)
                {
                    double diff = CurCenter.Y - afterZoomT - BorderRect.Top;
                    doubleAnimationY = new DoubleAnimation(TLT.Y, TLT.Y - diff, duration);
                }
                else if (CurCenter.Y+ afterZoomB<BorderRect.Bottom)
                {
                    double diff = BorderRect.Bottom - (CurCenter.Y + afterZoomB);
                    doubleAnimationY = new DoubleAnimation(TLT.Y, TLT.Y + diff, duration);
                }
            }

            TLT.BeginAnimation(TranslateTransform.XProperty, doubleAnimationX);
            TLT.BeginAnimation(TranslateTransform.YProperty,doubleAnimationY);




        }

        private void ZoomPic(int CurrentValue) //Zoom by self judgement without animation 
        {
            if (CurrentValue < 0)
                CurrentValue = 0;
            //得到当前应呈现的放缩比
            double DeltaScale = (1 + CurrentValue * 0.01);
            //得到图像区、边框区矩形
            ImageRect = ImageArea.TransformToAncestor(this).TransformBounds(new Rect(ImageArea.RenderSize));
            BorderRect = ViewPort.TransformToAncestor(this).TransformBounds(new Rect(ViewPort.RenderSize));


            //Duration duration = new Duration(new TimeSpan(0, 0, 0, 0, 125));
            //DoubleAnimation doubleAnimationScale = new DoubleAnimation((DeltaScale), duration);
            //DoubleAnimation doubleAnimationX = new DoubleAnimation(TLT.X, duration);
            //DoubleAnimation doubleAnimationY = new DoubleAnimation(TLT.Y, duration);


            //反变换求得最初的矩形用作参考（可能有偏差？待确定）

            //OriginRect = ImageGroup.Inverse.TransformBounds(ImageRect);


            if (ZoomValue == 0)
                OriginRect = ImageRect;//若当前的放缩比为0，存储这个矩形的具体参数用作参照

            if (CurrentValue == 0)//若放缩比归0，使所有transform还原为初值
            {
                STF.ScaleX = 1;
                STF.ScaleY = 1;
                TLT.X = 0;
                TLT.Y = 0;
                //Duration Recovery = new Duration(new TimeSpan(0, 0, 0, 0, 125));
                //DoubleAnimation doubleAnimationScaleR = new DoubleAnimation(1, Recovery);
                //DoubleAnimation doubleAnimationR = new DoubleAnimation(0, Recovery);

                //STF.BeginAnimation(ScaleTransform.ScaleXProperty, doubleAnimationScaleR);
                //STF.BeginAnimation(ScaleTransform.ScaleYProperty, doubleAnimationScaleR);
                //TLT.BeginAnimation(TranslateTransform.XProperty, doubleAnimationR);
                //TLT.BeginAnimation(TranslateTransform.YProperty, doubleAnimationR);
                return;
            }
            Point PointOfOrigin = ImageGroup.Inverse.Transform(CurWheelCenterPoint);//将点位转换到最初的图像矩形上
            
            Point ZoomedCenter = new Point(PointOfOrigin.X * DeltaScale, PointOfOrigin.Y * DeltaScale);//放大后的点（未进行translate transform）

            Vector PointDiff = ZoomedCenter - CurWheelCenterPoint;//未进行translate transfrom的放大点位与当前鼠标点位的差异，就是应该移动的量

            Rect ZoomedRect = new Rect(OriginRect.X, OriginRect.Y, OriginRect.Width * DeltaScale, OriginRect.Height * DeltaScale);//放大后的矩形（未进行translate transform）



            /*预测判断（若进行了默认的translate transform之后，对可能会出现的情况进行预测修正）*/
            //x轴
            if (ZoomedRect.Width <= BorderRect.Width)//若图像放大后的宽度依旧小于边框宽度
            {
                double SingleWidthDiff = (BorderRect.Width - ZoomedRect.Width) * 0.5d;//得到单边的差值
                double Left = BorderRect.Left + SingleWidthDiff;                      //算出正确呈现的点位
                double diff = Left - ImageRect.Left;                                  //算出当前图像与正确点位的差值，即为应该移动的差值
                TLT.X += diff;                                                        //执行translatetransform


                //doubleAnimationX = new DoubleAnimation(TLT.X + diff, duration);
                
            }
            else//图像放大后宽度大于边框
            {
                //double moveX = -PointDiff.X;

                TLT.X = -PointDiff.X;
                if (ZoomedRect.Left + TLT.X > BorderRect.Left)//平移后左越界
                {
                    TLT.X -= ((ZoomedRect.Left + TLT.X) - BorderRect.Left);


                    //moveX -= ((ZoomedRect.Left + TLT.X) - BorderRect.Left);
                    //doubleAnimationX = new DoubleAnimation(moveX, duration);
                }
                else if (ZoomedRect.Right + TLT.X < BorderRect.Right)//平移后右越界
                {
                    TLT.X += (BorderRect.Right - (ZoomedRect.Right + TLT.X));
                    //moveX += (BorderRect.Right - (ZoomedRect.Right + TLT.X));
                    //doubleAnimationX = new DoubleAnimation(moveX, duration);
                }
            }

            //y轴
            if(ZoomedRect.Height <= BorderRect.Height)//若图像放大后的高度依旧小于边框高度
            {
                double SingleHeightDiff = (BorderRect.Height - ZoomedRect.Height) * 0.5d;
                double Top = BorderRect.Top + SingleHeightDiff;
                double diff = Top-ImageRect.Top;
                TLT.Y += diff;
                //doubleAnimationY = new DoubleAnimation(TLT.Y + diff, duration);
            }
            else// 图像放大后宽度大于边框
            {
                //double moveY = -PointDiff.Y;
                TLT.Y = -PointDiff.Y;
                if(ZoomedRect.Top + TLT.Y > BorderRect.Top)//平移后上越界
                {
                    TLT.Y -= (ZoomedRect.Top + TLT.Y) - BorderRect.Top;
                    //moveY -= (ZoomedRect.Top + TLT.Y) - BorderRect.Top;
                    //doubleAnimationY = new DoubleAnimation(moveY, duration);
                }
                else if(ZoomedRect.Bottom + TLT.Y < BorderRect.Bottom)//平移后下越界
                {
                    TLT.Y += (BorderRect.Bottom - (ZoomedRect.Bottom + TLT.Y));
                    //moveY += (BorderRect.Bottom - (ZoomedRect.Bottom + TLT.Y));
                    //doubleAnimationY = new DoubleAnimation(moveY, duration);
                }
            }
            STF.ScaleX = DeltaScale;
            STF.ScaleY = DeltaScale;
            //STF.BeginAnimation(ScaleTransform.ScaleXProperty, doubleAnimationScale);
            //STF.BeginAnimation(ScaleTransform.ScaleYProperty, doubleAnimationScale);

            //TLT.BeginAnimation(TranslateTransform.XProperty, doubleAnimationX);
            //TLT.BeginAnimation(TranslateTransform.YProperty, doubleAnimationY);

        }


        private void ImageArea_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int CurDelta = (e.Delta / 120);



            if (ImageArea.Source == null)
                return;
            if ((ZoomValue <= 0 && CurDelta < 0) || (ZoomValue >= 100 && CurDelta > 0))
                return;

            CurWheelCenterPoint = e.GetPosition(ImageArea);

            //if (ZoomValue == 0)
            //{
            //    OriginCenterPoint = CurWheelCenterPoint;
            //}

            ZoomValue += CurDelta;
        }

        private void ImageArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            Point CurPos = e.GetPosition(this);
            //CurPos = ImageGroup.Inverse.Transform(CurPos);
            if (ZoomValue != 0)
                IsZoomed = true;
            else
            {
                IsZoomed = false;
            }
            Mouse.Capture(ImageArea);
            IsDrag = true;
            CurPoint = CurPos;
        }

        private void ImageArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);

            IsDrag = false;
        }

        private void ImageArea_MouseMove(object sender, MouseEventArgs e)
        {


            //获得图形区域的矩形
            ImageRect = ImageArea.TransformToAncestor(this).TransformBounds(new Rect(ImageArea.RenderSize));
            //获得Border的矩形
            BorderRect = ViewPort.TransformToAncestor(this).TransformBounds(new Rect(ViewPort.RenderSize));

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (IsDrag && IsZoomed)
                {
                    //确认当前四边界状态
                    IsReachTop = ImageRect.Top >= BorderRect.Top ? true : false;
                    IsReachBottom = ImageRect.Bottom <= BorderRect.Bottom ? true : false;
                    IsReachLeft = ImageRect.Left >= BorderRect.Left ? true : false;
                    IsReachRight = ImageRect.Right <= BorderRect.Right ? true : false;





                    Point PointBuffer = e.GetPosition(this);

                    Vector vector = PointBuffer - CurPoint;


                    //多个MouseMove截断
                    if (ImageRect.Left + vector.X > BorderRect.Left && vector.X > 0)
                        vector.X = BorderRect.Left - ImageRect.Left;
                    if (ImageRect.Right + vector.X < BorderRect.Right && vector.X < 0)
                        vector.X = BorderRect.Right - ImageRect.Right;
                    if (ImageRect.Top + vector.Y > BorderRect.Top && vector.Y > 0)
                        vector.Y = BorderRect.Top - ImageRect.Top;
                    if (ImageRect.Bottom + vector.Y < BorderRect.Bottom && vector.Y < 0)
                        vector.Y = BorderRect.Bottom - ImageRect.Bottom;

                    //已触边时
                    if (((IsReachLeft) && (vector.X > 0)) || ((IsReachRight) && (vector.X < 0)))
                        vector.X = 0;
                    if (((IsReachTop) && (vector.Y > 0)) || ((IsReachBottom) && (vector.Y < 0)))
                        vector.Y = 0;

                    //Duration duration = new Duration(new TimeSpan(0, 0, 0, 0, 0));

                    //DoubleAnimation doubleAnimationX = new DoubleAnimation(TLT.X + vector.X, duration);
                    //DoubleAnimation doubleAnimationY = new DoubleAnimation(TLT.Y + vector.Y, duration);
                    //TLT.BeginAnimation(TranslateTransform.XProperty, doubleAnimationX);
                    //TLT.BeginAnimation(TranslateTransform.YProperty, doubleAnimationY);
                    TLT.X += vector.X;
                    TLT.Y += vector.Y;

                    //点位更新
                    CurPoint = PointBuffer;
                }
            }

        }

        private void ViewPort_LostFocus(object sender, RoutedEventArgs e)
        {
            Mouse.Capture(null);
            IsDrag = false;
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ImageRect = ImageArea.TransformToAncestor(this).TransformBounds(new Rect(ImageArea.RenderSize));
            OriginRect = ImageGroup.Inverse.TransformBounds(ImageRect);
            
            DoPosJudge();

        }
    }
}
