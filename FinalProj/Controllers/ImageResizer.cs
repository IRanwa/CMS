using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;

namespace FinalProj.Controllers
{
    public class ImageResizer
    {
        public void ResizeImage(string ServerSavePath, int width, int height, string saveFilePath)
        {
            Rectangle destRect = new Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(width, height);

            Image img = Image.FromFile(ServerSavePath);
            destImage.SetResolution(img.HorizontalResolution, img.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(img, destRect, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, wrapMode);
                    wrapMode.Dispose();
                }
                graphics.Dispose();
            }
            img.Dispose();
            destImage.Save(saveFilePath);
            destImage.Dispose();
        }

        internal Bitmap ResizeImage(Image imgPhoto, int thumbWidth, int thumbHeight)
        {
            throw new NotImplementedException();
        }
    }
}