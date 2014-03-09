using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Neuro;
using AForge.Neuro.Learning;
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;

namespace AforgeNet_eg1
{
    public partial class Form1 : Form
    {

        Bitmap src = (Bitmap)Image.FromFile("test111.b");
        CardsBitmapProcesser processer = new CardsBitmapProcesser();
        private Dictionary<Rectangle, Bitmap> Result = new Dictionary<Rectangle, Bitmap>();

        //待识别图像固定为12x16，用3x4的小块进行切分统计，总共有16份数据作为输入
        ActivationNetwork network = new ActivationNetwork(new SigmoidFunction(2), 16, 4, 13);

        //待识别12x16图像
        List<Bitmap> bmpsToRecognized = new List<Bitmap>(); 

        public Form1()
        {
            InitializeComponent();

            //Result = processer.Process(src);
            //var resizeFilter = new ResizeBilinear(12, 16);
            //bmpsToRecognized = Result.Select(r => resizeFilter.Apply(r.Value)).ToList();

            //var input = new double[17][];
            //var output = new double[17][]
            //{
            //    new []{2.0},
            //    new []{1.0},
            //    new []{1.0},
            //    new []{1.0},
            //    new []{13.0},
            //    new []{13.0},
            //    new []{12.0},
            //    new []{11.0},
            //    new []{11.0},
            //    new []{9.0},
            //    new []{9.0},
            //    new []{8.0},
            //    new []{5.0},
            //    new []{5.0},
            //    new []{5.0},
            //    new []{4.0},
            //    new []{3.0}
            //};

            //var indexFirst = 0;
            //bmpsToRecognized.ForEach(bmp =>
            //{
            //    input[indexFirst] = new double[16];

            //    //特征点提取，用3x4的小块进行切分统计白点个数(黑色为背景色)，总共有16份数据作为输入
            //    var features = GetFeature(bmp);
            //    for (var i = 0; i < features.Count; i++)
            //    {
            //        input[indexFirst][i] = features[i];
            //    }

            //    indexFirst++;
            //});

            var input = new List<double[]>();
            var output = new List<double[]>();
            Directory.EnumerateDirectories(".\\Result")
                .ToList()
                .ForEach(dir =>
                {
                    var dirName = dir.Substring(dir.LastIndexOf('\\') + 1);
                    var numOutput = (Convert.ToDouble(dirName) - 7) / 10;

                    Directory.EnumerateFiles(dir)
                        .ToList()
                        .ForEach(file =>
                        {
                            var features = GetFeature((Bitmap)Image.FromFile(file));

                            input.Add(features.ToArray());

                            var opTmp = new List<double>();
                            for (var i = 0; i < 13; i++)
                            {
                                opTmp.Add(0);
                            }
                            opTmp[Convert.ToInt32(dirName) - 1] = 1;
                            output.Add(opTmp.ToArray());

                            GC.Collect();
                        });
                });


            TransNetWork(input.ToArray(), output.ToArray());
            network.Save("network.data");

            //测试
            var result = network.Compute(input[11]).ToArray();


            var tmp = result;
        }

        private void TransNetWork(double[][] input, double[][] output)
        {
            // create teacher
            var teacher = new BackPropagationLearning(network);
            
            // loop
            int iteration = 0;
            double error = 9999;
            while (error > 0.01)
            {
                error = teacher.RunEpoch(input, output);
                Console.WriteLine("{0}:learning error ===> {1}", iteration++, error);
            }
        }

        private List<double> GetFeature(Bitmap bmp)
        {
            var result = new List<double>();
            //特征点提取，用3x4的小块进行切分统计白点个数(黑色为背景色)，总共有16份数据作为输入
            for (var j = 1; j <= 4; j++)
            {
                for (var i = 1; i <= 4; i++)
                {
                    double count = 0;
                    for (var k = (i - 1) * 3; k < (i * 3 - 1); k++)
                    {
                        for (var l = (j - 1) * 4; l < (j * 4 - 1); l++)
                        {
                            if (bmp.GetPixel(k, l).R == 255)
                            {
                                count++;
                            }
                        }
                    }

                    result.Add(count / 100);
                }
            }

            return result;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.LightGreen, e.ClipRectangle);

            var index = 0;

            e.Graphics.DrawImage(src, new Point(0, 0));

            Result.ToList()
                .ForEach(kv =>
                {
                    var cardBmp = src.Clone(new Rectangle(kv.Key.X, kv.Key.Y, kv.Key.Width, kv.Key.Height*2),
                        src.PixelFormat);
                    e.Graphics.DrawImage(kv.Value, new Point(20*index, 150));
                    index++;
                });

            return;
            
            
            
            
            
            
            e.Graphics.DrawImage(src, new Point(10, (src.Height + 10) * index));
            index++;

            var bmp = src;


            //去掉蓝色线条
            bmp = new ColorFiltering(new IntRange(0, 100), new IntRange(0, 100), new IntRange(130, 255))
            {
                FillColor = new RGB(Color.White),
                FillOutsideRange = false
            }.Apply(bmp);
            e.Graphics.DrawImage(bmp, new Point(10, (bmp.Height + 10) * index));
            index++;


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
                //白色变黑色，黑色变白色
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
                        else if (bmpDataBuffer[h * bmpData.Stride + w] == 255)
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



                //宽度最小2，高度最小10，宽度最大14，高度最大16过滤噪音
                bmp = new BlobsFiltering(2, 12, 14, 16).Apply(bmp);
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
