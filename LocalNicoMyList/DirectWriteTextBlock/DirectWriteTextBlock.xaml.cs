using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LocalNicoMyList.DirectWriteTextBlock
{
    /// <summary>
    /// DirectWriteTextBlock.xaml の相互作用ロジック
    /// </summary>
    public partial class DirectWriteTextBlock : UserControl
    {

        #region ■■■■■ 依存関係プロパティ
#if true
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                "Text", // プロパティ名を指定
                typeof(String), // プロパティの型を指定
                typeof(DirectWriteTextBlock), // プロパティを所有する型を指定
                new PropertyMetadata(
                    "", // デフォルト値の設定
                    new PropertyChangedCallback(OnTextChanged))); // プロパティの変更時に呼ばれるコールバックの設定

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as DirectWriteTextBlock;
            self.textPropertyChanged(e.NewValue as string);
        }

        public String Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
#endif

#if false
        // 1. 依存プロパティの作成
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text",
                                        typeof(string),
                                        typeof(DirectWriteTextBlock),
                                        new FrameworkPropertyMetadata("Text", new PropertyChangedCallback(OnTextChanged)));

        // 2. CLI用プロパティを提供するラッパー
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // 3. 依存プロパティが変更されたとき呼ばれるコールバック関数の定義
        private static void OnTextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            // オブジェクトを取得して処理する
            DirectWriteTextBlock ctrl = obj as DirectWriteTextBlock;
            if (ctrl != null)
            {
                ctrl.textPropertyChanged(e.NewValue as string);
            }
        }
#endif
#if false
        public new static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                "FontSize", // プロパティ名を指定
                typeof(float), // プロパティの型を指定
                typeof(DirectWriteTextBlock), // プロパティを所有する型を指定
                new PropertyMetadata(
                    "", // デフォルト値の設定
                    FontSizePropertyChanged)); // プロパティの変更時に呼ばれるコールバックの設定

        private static void FontSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as DirectWriteTextBlock;
            //self.fontSizePropertyChanged(e.NewValue as float);
        }

        public new float FontSize
        {
            get { return (float)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }
#endif

#endregion

        SharpDX.Direct3D11.Device _device;
        Texture2D _renderTarget;
        RenderTarget _d2DRenderTarget;

        Dx11ImageSource _d3dImage;

        Size _textSize;

        public DirectWriteTextBlock()
        {
            InitializeComponent();
        }

        public void textPropertyChanged(string newText)
        {
            _textSize = sizeOfString(newText);

            if (null == _device)
                return;

            createAndBindTargets(); // サイズが変わると必要

            prepareAndCallRender();
            _d3dImage.InvalidateD3DImage();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            startD3D();

            prepareAndCallRender();
            _d3dImage.InvalidateD3DImage();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            endD3D();
        }

        void startD3D()
        {
            _device = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);

            _d3dImage = new Dx11ImageSource();
            //d3DSurface.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

            createAndBindTargets();

            this.image.Source = _d3dImage;
        }

        void endD3D()
        {
            //d3DSurface.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
            this.image.Source = null;

            Disposer.SafeDispose(ref _d2DRenderTarget);
            //Disposer.SafeDispose(ref d2DFactory);
            Disposer.SafeDispose(ref _d3dImage);
            Disposer.SafeDispose(ref _renderTarget);
            Disposer.SafeDispose(ref _device);
        }

        private void createAndBindTargets()
        {
            _d3dImage.SetRenderTarget(null);

            Disposer.SafeDispose(ref _d2DRenderTarget);
            //Disposer.SafeDispose(ref d2DFactory);
            Disposer.SafeDispose(ref _renderTarget);

            var width = Math.Max((int)_textSize.Width, 1);
            var height = Math.Max((int)_textSize.Height, 1);

            var renderDesc = new Texture2DDescription
            {
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.Shared,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };

            _renderTarget = new Texture2D(_device, renderDesc);

            var surface = _renderTarget.QueryInterface<SharpDX.DXGI.Surface>();

            var d2DFactory = new SharpDX.Direct2D1.Factory();
            var rtp = new RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied));
            _d2DRenderTarget = new RenderTarget(d2DFactory, surface, rtp);
            //resCache.RenderTarget = d2DRenderTarget;

            _d3dImage.SetRenderTarget(_renderTarget);

            _device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height, 0.0f, 1.0f);
        }

        void prepareAndCallRender()
        {
            if (_device == null)
            {
                return;
            }

            _d2DRenderTarget.BeginDraw();
            render(_d2DRenderTarget);
            _d2DRenderTarget.EndDraw();

            _device.ImmediateContext.Flush();
        }

        void render(RenderTarget target)
        {
            var fm = getFontMetrics(getFontFamilyName());
            float ratio = getFontSize() / fm.Value.DesignUnitsPerEm;
            var textHeight = (fm.Value.Ascent + fm.Value.Descent) * ratio;

            var factoryDWrite = new SharpDX.DirectWrite.Factory();
            var textFormat = new TextFormat(factoryDWrite, getFontFamilyName(), getFontWeight(), SharpDX.DirectWrite.FontStyle.Normal, getFontSize());
            textFormat.WordWrapping = WordWrapping.NoWrap;
            var textLayout = new TextLayout(factoryDWrite, this.Text, textFormat, 10, 10);

            var brush = new SharpDX.Direct2D1.SolidColorBrush(_d2DRenderTarget, new RawColor4(0.0f, 0.0f, 0.0f, 1.0f));

            _d2DRenderTarget.Clear(new RawColor4(1.0f, 1.0f, 1.0f, 0.0f));
            _d2DRenderTarget.DrawTextLayout(new RawVector2(0, 0), textLayout, brush);
        }

        string getFontFamilyName()
        {
            string name;
            this.FontFamily.FamilyNames.TryGetValue(XmlLanguage.GetLanguage("en-us"), out name);
            return name;
        }

        float getFontSize() { return (float)this.FontSize; }

        SharpDX.DirectWrite.FontWeight getFontWeight()
        {
            if (this.FontWeight == System.Windows.FontWeights.Thin) // 100
                return SharpDX.DirectWrite.FontWeight.Thin;
            else if (this.FontWeight == System.Windows.FontWeights.ExtraLight ||
                    this.FontWeight == System.Windows.FontWeights.UltraLight) // 200
                return SharpDX.DirectWrite.FontWeight.ExtraLight;
            else if (this.FontWeight == System.Windows.FontWeights.Light) // 300
                return SharpDX.DirectWrite.FontWeight.Light;
            else if (this.FontWeight == System.Windows.FontWeights.Normal ||
                    this.FontWeight == System.Windows.FontWeights.Regular) // 400
                return SharpDX.DirectWrite.FontWeight.Normal;
            else if (this.FontWeight == System.Windows.FontWeights.Medium) // 500
                return SharpDX.DirectWrite.FontWeight.Medium;
            else if (this.FontWeight == System.Windows.FontWeights.DemiBold ||
                    this.FontWeight == System.Windows.FontWeights.SemiBold) // 600
                return SharpDX.DirectWrite.FontWeight.DemiBold;
            else if (this.FontWeight == System.Windows.FontWeights.Bold) // 700
                return SharpDX.DirectWrite.FontWeight.Bold;
            else if (this.FontWeight == System.Windows.FontWeights.ExtraBold ||
                    this.FontWeight == System.Windows.FontWeights.UltraBold) // 800
                return SharpDX.DirectWrite.FontWeight.ExtraBold;
            else if (this.FontWeight == System.Windows.FontWeights.Black ||
                    this.FontWeight == System.Windows.FontWeights.Heavy) // 900
                return SharpDX.DirectWrite.FontWeight.Black;
            else if (this.FontWeight == System.Windows.FontWeights.ExtraBlack ||
                    this.FontWeight == System.Windows.FontWeights.UltraBlack) // 950
                return SharpDX.DirectWrite.FontWeight.ExtraBlack;

            return SharpDX.DirectWrite.FontWeight.Normal;
        }


        #region ■■■■■ フォント関連

        FontMetrics? getFontMetrics(string familyName)
        {
            var factoryDWrite = new SharpDX.DirectWrite.Factory();
            var fontCollection = factoryDWrite.GetSystemFontCollection(false);
#if false // test
            for (int ni = 0; ni < fontCollection.FontFamilyCount; ++ni)
            {
                Console.WriteLine("-----");
                var fontFamily_ = fontCollection.GetFontFamily(ni);
                var localizedStrs = fontFamily_.FamilyNames;
                for (int nj = 0; nj < localizedStrs.Count; ++nj) {
                    Console.WriteLine(localizedStrs.GetString(nj));
                }
            }
#endif
            int index;
            bool exists = fontCollection.FindFamilyName(familyName, out index);
            if (!exists)
            {
                return null;
            }
            var fontFamily = fontCollection.GetFontFamily(index);
            var font = fontFamily.GetFirstMatchingFont(SharpDX.DirectWrite.FontWeight.Normal, SharpDX.DirectWrite.FontStretch.Normal, SharpDX.DirectWrite.FontStyle.Normal);
            return font.Metrics;
        }

        Size sizeOfString(string text)
        {
            var factoryDWrite = new SharpDX.DirectWrite.Factory();
            var textFormat = new TextFormat(factoryDWrite, getFontFamilyName(), getFontWeight(), SharpDX.DirectWrite.FontStyle.Normal, getFontSize());
            textFormat.WordWrapping = WordWrapping.NoWrap;
            var textLayout = new TextLayout(factoryDWrite, text, textFormat, 10, 10);
            var textMetrics = textLayout.Metrics;
            return new Size(textMetrics.Width, textMetrics.Height);
        }

#endregion
    }
}
