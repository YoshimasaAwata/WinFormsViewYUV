namespace WinFormsViewYUV
{
    public partial class Form1 : Form
    {
        public const int CifWidth = 352;
        public const int CifHeight = 288;
        public const int YSize = CifWidth * CifHeight;
        public const int UVSize = (CifWidth / 2) * (CifHeight / 2);

        private byte[] _y = new byte[YSize];
        private byte[] _u = new byte[UVSize];
        private byte[] _v = new byte[UVSize];

        private Bitmap _bmp = new(CifWidth, CifHeight, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        private Rectangle _rect = new(0, 0, CifWidth, CifHeight);

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                FileName = "akiyo_cif.yuv",
                Title = "Select CIF Size YUV File",
                Filter = "YUV file (*.yuv)|*.yuv",
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = ofd.FileName;
                button2.Enabled = true;
            }
        }

        private int ReadYUV(FileStream fs)
        {
            if (fs != null)
            {
                int size = fs.Read(_y, 0, YSize);
                if (size != YSize) return -1;
                size = fs.Read(_u, 0, UVSize);
                if (size != UVSize) return -1;
                size = fs.Read(_v, 0, UVSize);
                if (size != UVSize) return -1;
            }
            else return -1;

            return 0;
        }

        unsafe private void ConvertYUV2RGB(IntPtr xrgb_data, int stride)
        {
            Func<long, long> Clip = (n) => (n <= 0) ? 0 : ((n >= 256) ? 255 : n);

            uint* xrgb = (uint*)xrgb_data;

            for (var h = 0; h < CifHeight; h++)
            {
                int y_pos = h * CifWidth;
                int uv_pos = (h / 2) * (CifWidth / 2);
                int xrgb_pos = h * (stride / sizeof(uint));

                for (var w = 0; w < CifWidth; w++)
                {
                    double y16 = _y[y_pos + w] - 16.0;
                    double u128 = _u[uv_pos + (w / 2)] - 128.0;
                    double v128 = _v[uv_pos + (w / 2)] - 128.0;

                    double dr = (1.164 * y16) + (0.0 * u128) + (1.596 * v128);
                    double dg = (1.164 * y16) + (-0.392 * u128) + (-0.813 * v128);
                    double db = (1.164 * y16) + (2.017 * u128) + (0.0 * v128);
                    dr = Math.Round(dr, MidpointRounding.AwayFromZero);
                    dg = Math.Round(dg, MidpointRounding.AwayFromZero);
                    db = Math.Round(db, MidpointRounding.AwayFromZero);

                    long r = Clip((long)dr);
                    long g = Clip((long)dg);
                    long b = Clip((long)db);

                    xrgb[xrgb_pos + w] = (uint)((r << 16) | (g << 8) | b);
                }
            }

            return;
        }

        private void DrawImage()
        {
            using (var fs = File.OpenRead(textBox1.Text))
            {
                while (0 == ReadYUV(fs))
                {
                    var bmData = _bmp.LockBits(_rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, _bmp.PixelFormat);
                    ConvertYUV2RGB(bmData.Scan0, bmData.Stride);
                    _bmp.UnlockBits(bmData);
                    pictureBox1.Invalidate();
                    pictureBox1.Update();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = _bmp;
            DrawImage();
        }
    }
}