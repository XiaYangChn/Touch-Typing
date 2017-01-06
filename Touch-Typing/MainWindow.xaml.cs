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

using System.Threading;

using System.Windows.Forms;
using System.Runtime.InteropServices;   //调用WINDOWS API函数时要用到
using Microsoft.Win32;
using System.Windows.Interop;                  //写入注册表时要用到

namespace Touch_Typing
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region  屏幕穿透处理
        private const int GWL_EXSTYLE = (-20);              //  获取窗口扩展风格
        private const int WS_EX_TRANSPARENT = 0x20;         //  窗口穿透风格

        [DllImport("user32", EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        //  窗口扩展全局变量
        private IntPtr exHwnd;          //  窗口句柄
        private uint exBasicStyle;      //  基本扩展风格
        private uint exTransStyle;      //  窗口穿透拓展风格
        private bool isCtrlKeyDown = false;     //  Ctrl事件处理辅助变量

        private void InitWindowExStyle()
        {
            this.SourceInitialized += delegate
            {
                exHwnd = new WindowInteropHelper(this).Handle;
                exBasicStyle = GetWindowLong(exHwnd, GWL_EXSTYLE);

                SetWindowLong(exHwnd, GWL_EXSTYLE, exBasicStyle | WS_EX_TRANSPARENT);
                exTransStyle = GetWindowLong(exHwnd, GWL_EXSTYLE);
            };
        }
        #endregion

        #region 全局属性 & MainWindow()
        //  全局属性
        private double screenWidth;
        private double screenHeight;

        private double defaultWidth;
        private double defaultHeight;

        //  键盘监听钩子
        KeyboardHook keyHook;

        public MainWindow()
        {
            InitializeComponent();
            //  初始化窗口大小、位置
            {
                screenWidth = SystemParameters.PrimaryScreenWidth;
                screenHeight = SystemParameters.PrimaryScreenHeight;

                this.Width = screenWidth / 1920 * 1080 * 1.25;
                this.Height = this.Width / 1080 * 360;

                defaultWidth = this.Width;
                defaultHeight = this.Height;

                this.Left = (screenWidth - this.Width) / 2;
                this.Top = (screenHeight - this.Height) / 2;
            }
            //  初始化键盘钩子
            InitUserActivityHook();
            //  初始化窗口扩展风格（操作穿透）
            InitWindowExStyle();
        }
        #endregion

        #region 全局钩子安装、监听事件处理
        private void InitUserActivityHook()
        {
            //  安装键盘钩子
            keyHook = new KeyboardHook();
            //  钩住键按下
            keyHook.KeyDown += new System.Windows.Forms.KeyEventHandler(hook_KeyDown);
            //  钩住键松开
            keyHook.KeyUp += new System.Windows.Forms.KeyEventHandler(hook_KeyUp);

            keyHook.Start();
        }

        //  判断输入键值（实现KeyDown事件）
        private void hook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            #region 全局键盘监听设置
            //  e.KeyValue
            String keyUp = "_" + e.KeyValue.ToString();
            String keyDown = "__" + e.KeyValue.ToString();
            Image keyUpImg = (Image)this.FindName(keyUp);
            Image keyDownImg = (Image)this.FindName(keyDown);

            if (null != keyUpImg && null != keyDownImg)
            {
                keyUpImg.Visibility = Visibility.Hidden;
                keyDownImg.Visibility = Visibility.Visible;
            }

            //  Ctrl快捷键，窗口对操作不穿透
            if ((162 == e.KeyValue || 163 == e.KeyValue) && false == isCtrlKeyDown)
            {
                SetWindowLong(exHwnd, GWL_EXSTYLE, exBasicStyle);
                //  可以调整大小
                this.ResizeMode = ResizeMode.CanResizeWithGrip;

                isCtrlKeyDown = true;
            }
            #endregion

            #region 局部键盘监听设置
            //  快捷键设置 透明度
            if (this.IsActive)
            {
                //  Ctrl + "-"快捷键，降低透明度 10%
                if (189 == e.KeyValue && (int)System.Windows.Forms.Control.ModifierKeys == (int)Keys.Control)
                {
                    double currentOpacity = this.Opacity;
                    double nowOpacity = currentOpacity - 0.1;

                    if (nowOpacity > 0.1)
                    {
                        this.Opacity = nowOpacity;
                    }
                    else
                    {
                        this.Opacity = 0.1;
                    }
                }
                //  Ctrl + "="快捷键，提高透明度 10%
                if (187 == e.KeyValue && (int)System.Windows.Forms.Control.ModifierKeys == (int)Keys.Control)
                {
                    double currentOpacity = this.Opacity;
                    double nowOpacity = currentOpacity + 0.1;

                    if (nowOpacity < 1)
                    {
                        this.Opacity = nowOpacity;
                    }
                    else
                    {
                        this.Opacity = 1;
                    }
                }
            #endregion
            }
        }
        //  判断输入键值（实现KeyUp事件）
        private void hook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //  e.KeyValue
            String keyUp = "_" + e.KeyValue.ToString();
            String keyDown = "__" + e.KeyValue.ToString();
            Image keyUpImg = (Image)this.FindName(keyUp);
            Image keyDownImg = (Image)this.FindName(keyDown);

            if (null != keyUpImg && null != keyDownImg)
            {
                keyUpImg.Visibility = Visibility.Visible;
                keyDownImg.Visibility = Visibility.Hidden;
            }

            //  Ctrl快捷键，窗口对操作可穿透
            if ((162 == e.KeyValue || 163 == e.KeyValue) && true == isCtrlKeyDown)
            {
                SetWindowLong(exHwnd, GWL_EXSTYLE, exTransStyle);

                //  不可调整大小
                this.ResizeMode = ResizeMode.CanResize;

                isCtrlKeyDown = false;
            }
        }
        #endregion

        #region 窗口内部事件处理
        //  实现窗口拖拽（Window.Style = None AllowsTransparency = true）
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);  //  ？

            // Begin dragging the window
            this.DragMove();
        }
        //  实现窗口大小等比例变换（窗口会有闪烁）
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            this.Height = this.Width / 1080 * 360;
        }
        //  滚轮滚动改变窗口大小（窗口居中）
        private void BaseWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                double nowWidth = this.Width + defaultWidth * 0.05;

                if (nowWidth < screenWidth)
                {
                    this.Width = nowWidth;
                }
                else
                {
                    this.Width = screenWidth;
                }

                this.Height = this.Width / 1080 * 360;
            }
            if (e.Delta < 0)
            {
                double nowWidth = this.Width - defaultWidth * 0.05;
                double minWidth = 200;

                if (nowWidth > minWidth)
                {
                    this.Width = nowWidth;
                }
                else
                {
                    this.Width = minWidth;
                }

                this.Height = this.Width / 1080 * 360;
            }

            this.Left = (screenWidth - this.Width) / 2;
            this.Top = (screenHeight - this.Height) / 2;
        }
        //  程序关闭后对钩子进行释放
        private void OnWindowClosed(object sender, EventArgs e)
        {
            keyHook.Stop();
        }
        #endregion

    }
}
