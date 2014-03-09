using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace AforgeNet_eg1
{
    class RectangleLeftComparer : IComparer<Rectangle>
    {
        public int Compare(Rectangle x, Rectangle y)
        {
            if (x.Left == y.Left)
            {
                return 0;
            }
            
            if (x.Left < y.Left)
            {
                return -1;
            }
            
            return 1;
        }
    }

    public class CardsBitmapProcesser
    {
        public Dictionary<Rectangle, Bitmap> Process(Bitmap bmpCard)
        {
            var result = new Dictionary<Rectangle, Bitmap>();

            //灰度化、二值化、去噪
            bmpCard = FilterProcess(bmpCard);

            //去掉不在同一水平线上的块
            var bc = new BlobCounter(bmpCard);
            var rects = bc.GetObjectsRectangles().ToList();
            var avTop = rects.Average(r => r.Top);

            rects = rects.Where(r => Math.Abs(avTop - r.Top) < 3).ToList();
            rects.Sort(new RectangleLeftComparer());

            if (rects.Count <= 0)
            {
                return result;
            }

            //处理10特殊情况，因为10分两部分，需要合并区域
            var tmpRects = new List<Rectangle>{rects[0]};
            var rectIndex = rects[0];
            rects.ForEach(r =>
            {
                if (r != rectIndex)
                {
                    //如果两个区域间隔小于4，则说明是10，需要合并区域
                    if ((r.Left - rectIndex.Right) < 4)
                    {
                        tmpRects.Remove(rectIndex);
                        tmpRects.Add(new Rectangle(rectIndex.X,
                            rectIndex.Y > r.Y ? r.Y : rectIndex.Y,
                            r.Right - rectIndex.Left,
                            rectIndex.Height > r.Height ? rectIndex.Height : r.Height));
                    }
                    else
                    {
                        tmpRects.Add(r);
                    }
                }

                rectIndex = r;
            });

            
            //将所有字符区域及图像返回
            tmpRects.ForEach(r => result.Add(r, bmpCard.Clone(r, bmpCard.PixelFormat)));
            
            return result;
        }

        private Bitmap FilterProcess(Bitmap bmpCard)
        {
            var sqf = new FiltersSequence
            {
                {
                    //去掉蓝色线条
                    new ColorFiltering(new IntRange(0, 100), new IntRange(0, 100), new IntRange(130, 255))
                    {
                        FillColor = new RGB(Color.White),
                        FillOutsideRange = false
                    }
                },
                //灰度化
                {new Grayscale(0.2125, 0.7154, 0.0721)},
                //二值化
                {new Threshold(200)}
            };

            bmpCard = sqf.Apply(bmpCard);


            //白色变黑色，黑色变白色
            var bmpData = bmpCard.LockBits(new Rectangle(0, 0, bmpCard.Width, bmpCard.Height), ImageLockMode.ReadWrite, bmpCard.PixelFormat);
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

                if (count > bmpCard.Height * 4 / 5)
                {
                    for (var h = 0; h < bmpCard.Height; h++)
                    {
                        bmpDataBuffer[h * bmpData.Stride + w] = 0;
                    }
                }

            }
            Marshal.Copy(bmpDataBuffer, 0, bmpData.Scan0, bmpDataBuffer.Length);
            bmpCard.UnlockBits(bmpData);

            //宽度最小2，高度最小10，宽度最大14，高度最大16过滤噪音
            bmpCard = new BlobsFiltering(2, 12, 14, 16).Apply(bmpCard);

            return bmpCard;
        }
    }
}
