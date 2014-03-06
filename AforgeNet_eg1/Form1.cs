using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Imaging;
using AForge.Imaging.Filters;
using Image = System.Drawing.Image;

namespace AforgeNet_eg1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.LightGreen, e.ClipRectangle);

            var index = 0;
            var src = (Bitmap)Image.FromFile("test.bmp");
            e.Graphics.DrawImage(src, new Point(10, (src.Height + 10) * index));
            index++;

            var bmp = src;


            for (var w = 0; w < bmp.Width; w++)
            {
                for (var h = 0; h < bmp.Height; h++)
                {
                    var px = bmp.GetPixel(w, h);
                    if (px.R < 50 && px.G < 50 && px.B > 150)
                    {
                        bmp.SetPixel(w, h, Color.White);
                    }
                }
            }


            //灰度化
            bmp = new Grayscale(0.2125, 0.7154, 0.0721).Apply(bmp);
            e.Graphics.DrawImage(bmp, new Point(10, (bmp.Height + 10) * index));
            index++;

            //二值化
            bmp = new Threshold(200).Apply(bmp);
            e.Graphics.DrawImage(bmp, new Point(10, (bmp.Height + 10) * index));
            index++;

            //去噪
            try
            {
                var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite,
                    bmp.PixelFormat);
                var bmpDataBuffer = new byte[bmpData.Stride * bmpData.Height];
                Marshal.Copy(bmpData.Scan0, bmpDataBuffer, 0, bmpDataBuffer.Length);
                for (var w = 0; w < bmpData.Width; w++)
                {
                    var count = 0;
                    for (var h = 0; h < bmpData.Height; h++)
                    {
                        if (bmpDataBuffer[h * bmpData.Stride + w] == 0)
                        {
                            count++;
                            bmpDataBuffer[h * bmpData.Stride + w] = 255;
                        }
                        else
                        {
                            bmpDataBuffer[h * bmpData.Stride + w] = 0;
                        }
                    }

                    if (count > bmp.Height * 4 / 5)
                    {
                        for (var h = 0; h < bmp.Height; h++)
                        {
                            bmpDataBuffer[h * bmpData.Stride + w] = 0;
                        }
                    }

                }
                Marshal.Copy(bmpDataBuffer, 0, bmpData.Scan0, bmpDataBuffer.Length);
                bmp.UnlockBits(bmpData);
                e.Graphics.DrawImage(bmp, new Point(10, (bmp.Height + 10) * index));
                index++;



                bmp = new BlobsFiltering(2, 10, 14, 16).Apply(bmp);
                e.Graphics.DrawImage(bmp, new Point(10, (bmp.Height + 10) * index));
                index++;
            }
            catch (Exception ex)
            {
                
                throw;
            }

        }
    }
}
