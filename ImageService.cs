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
using System.Threading.Tasks;
using KVPLite;

namespace ImageScalerLib
{
    public class ImageService
    {
        private readonly Database database;
        public ImageService()
        {
            database = new Database();
        }
        public async Task<string> SetAndGetImageAsBase64Async(string src, int width = 0, int height = 0) // TODO Find a better name
        {
            string compositeKey = CreateCompositeKey(src, width, height);
            KeyValuePair<string, string> base64ByCompositeKeyPair = database.GetKvp(compositeKey);
            if (base64ByCompositeKeyPair.Equals(default(KeyValuePair<string, string>)))
            {
                string outBase64 = "";
                var base64Matcher = Regex.Match(src, @"data:(?<type>.+?);base64,(?<data>.+)");
                if (base64Matcher.Success)
                {
                    string inBase64 = base64Matcher.Groups["data"].Value;
                    byte[] dataAsBytes = Convert.FromBase64String(inBase64);
                    if (Image.DetectFormat(dataAsBytes) == null)
                    {
                        // TODO Handle exception
                    }
                    else
                    {
                        outBase64 = CreateModifiedImage(dataAsBytes, width, height);
                    }
                }
                else
                {
                    string srcDecoded = HttpUtility.UrlDecode(src);
                    using (System.Net.WebClient webClient = new System.Net.WebClient())
                    {
                        try
                        {
                            using (Stream stream = await webClient.OpenReadTaskAsync(srcDecoded))
                            using (MemoryStream ms = new MemoryStream())
                            {
                                await stream.CopyToAsync(ms);
                                outBase64 = CreateModifiedImage(ms.ToArray(), width, height);
                            }
                        }
                        catch (Exception)
                        {
                            if (Math.Min(width, height) == 0)
                            {
                                width = 100;
                                height = 100;
                            }
                            outBase64 = CreatePlaceholderImage(width, height);
                        }
                    }
                }
                base64ByCompositeKeyPair = new KeyValuePair<string, string>(compositeKey, outBase64);
                database.SetKvp(base64ByCompositeKeyPair);
            }
            return base64ByCompositeKeyPair.Value;
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
            string outBase64 = "";
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
                outBase64 = image.ToBase64String(format); // Automatically encodes image to the format derived from base64Data.
            }
            return outBase64;
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
            bool imageDeleted = database.RemoveKvp(compositeKey);
            return imageDeleted;
        }
        public bool DeleteAllImages()
        {
            bool imagesDeleted = database.RemoveAllKvp();
            return imagesDeleted;
        }

    }
}
