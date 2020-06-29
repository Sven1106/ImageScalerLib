using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Formats;
using System.Text.RegularExpressions;
using System.Web;
using KVPLite;

namespace Foxhole
{
    public class ImageCache
    {

        private Database KVPLiteDatabase { get; }
        public ImageCache()
        {
            KVPLiteDatabase = new Database();
        }
        ~ImageCache()
        {

        }
        public ScaledImage TryGetScaledImage(string src, int width = 0, int height = 0)
        {
            ScaledImage scaledImage = new ScaledImage();
            string compositeKey = CreateCompositeKey(src, width, height);
            KeyValuePair<string, string> base64ByCompositeKeyPair = KVPLiteDatabase.GetKvp(compositeKey);
            if (base64ByCompositeKeyPair.Equals(default(KeyValuePair<string, string>))) // if kvp does not exist
            {
                string base64Out = "";
                var base64Matcher = Regex.Match(src, @"data:(?<type>.+?);base64,(?<data>.+)");
                bool isSrcBase64 = base64Matcher.Success;
                if (isSrcBase64)
                {
                    try
                    {
                        string base64In = base64Matcher.Groups["data"].Value;
                        byte[] dataAsBytes = Convert.FromBase64String(base64In);
                        base64Out = CreateModifiedImage(dataAsBytes, width, height);
                        scaledImage.ResponseCode = ImageResponseCode.ImageFromBase64Inserted;
                    }
                    catch (Exception ex)
                    {
                        if (Math.Min(width, height) == 0)
                        {
                            width = 100;
                            height = 100;
                        }
                        base64Out = CreatePlaceholderImage(width, height);
                        scaledImage.ResponseCode = ImageResponseCode.Base64ValueWasNotValid;
                    }
                }
                else
                {
                    string srcDecoded = HttpUtility.UrlDecode(src);
                    using (System.Net.WebClient webClient = new System.Net.WebClient())
                    {
                        try
                        {
                            using (Stream stream = webClient.OpenRead(srcDecoded))
                            using (MemoryStream ms = new MemoryStream())
                            {
                                stream.CopyTo(ms);
                                base64Out = CreateModifiedImage(ms.ToArray(), width, height);
                                scaledImage.ResponseCode = ImageResponseCode.ImageFromSrcInserted;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (Math.Min(width, height) == 0)
                            {
                                width = 100;
                                height = 100;
                            }
                            base64Out = CreatePlaceholderImage(width, height);
                            scaledImage.ResponseCode = ImageResponseCode.RemoteServerNotFound;
                        }
                    }
                }
                base64ByCompositeKeyPair = new KeyValuePair<string, string>(compositeKey, base64Out);
                KVPLiteDatabase.SetKvp(base64ByCompositeKeyPair);
            }
            else
            {
                scaledImage.ResponseCode = ImageResponseCode.ImageExists;
            }
            scaledImage.Image = base64ByCompositeKeyPair;
            return scaledImage;
        }

        public static string CreatePlaceholderImage(int width, int height)
        {
            string base64 = "";
            using (var image = new Image<Rgba32>(Configuration.Default, width, height, Rgba32.LightGray))
            {
                int padding = 15;
                var text = width + " x " + height;
                var font = new Font(SystemFonts.Find("Calibri"), 36, FontStyle.Regular);
                var textGraphicsOptions = new TextGraphicsOptions(true)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                SizeF textSize = TextMeasurer.Measure(text, new RendererOptions(font));
                float scalingFactor = Math.Min(image.Width / textSize.Width, image.Height / textSize.Height);

                Font scaledFont = new Font(font, (scalingFactor * font.Size) - scalingFactor * padding);
                image.Mutate(x => x.DrawText(textGraphicsOptions, text, scaledFont, Color.DarkGray, new PointF(image.Width / 2, image.Height / 2)));
                base64 = image.ToBase64String(JpegFormat.Instance);
            }
            return base64;
        }

        public bool DeleteImage(string src, int width = 0, int height = 0)
        {
            string compositeKey = CreateCompositeKey(src, width, height);
            bool imageDeleted = KVPLiteDatabase.RemoveKvp(compositeKey);
            return imageDeleted;
        }
        public bool DeleteAllImages()
        {
            bool imagesDeleted = KVPLiteDatabase.RemoveAllKvp();
            return imagesDeleted;
        }

        private static string CreateCompositeKey(string src, int width, int height)
        {
            string compositeKey = src;
            if (width != 0)
            {
                compositeKey += "&width=" + width;
            }
            if (height != 0)
            {
                compositeKey += "&height=" + height;
            }

            return compositeKey;
        }

        private static string CreateModifiedImage(byte[] data, int width, int height)
        {
            string base64String = "";
            using (Image<Rgba32> image = Image.Load<Rgba32>(data, out IImageFormat format))
            {
                int newHeight;
                int newWidth;

                if (width == 0 && height == 0)
                {
                    newHeight = image.Height;
                    newWidth = image.Width;
                }
                else
                {
                    if (width == 0)
                    {
                        newWidth = (int)(height / (double)image.Height * image.Width);
                    }
                    else
                    {
                        newWidth = width;
                    }
                    if (height == 0)
                    {
                        newHeight = (int)(width / (double)image.Width * image.Height);
                    }
                    else
                    {
                        newHeight = height;
                    }
                }
                image.Mutate(x =>
                {
                    x.Resize(new ResizeOptions
                    {
                        Size = new Size(width, newHeight),
                        Mode = ResizeMode.Crop
                    });
                });
                base64String = image.ToBase64String(format); // Automatically encodes image to the format derived from base64Data.
            }
            return base64String;
        }
    }
}
