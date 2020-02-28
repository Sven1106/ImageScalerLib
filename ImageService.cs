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
using KVPLiteLib;

namespace ImageScalerLib
{
    public class ImageService
    {
        private readonly KVPLite _KVPLite;
        public ImageService()
        {
            _KVPLite = new KVPLite();
        }
        public async Task<string> GetOrSetScaledImageAsync(string src, int width, int height)
        {
            string key = src + "&width=" + width + "&height=" + height;

            KeyValuePair<string, string> keyValuePair = _KVPLite.GetKvp(key);
            if (keyValuePair.Equals(default(KeyValuePair<string, string>)))
            {
                string outBase64 = "";
                var base64Match = Regex.Match(src, @"data:(?<type>.+?);base64,(?<data>.+)");
                if (base64Match.Success)
                {

                    var base64Data = base64Match.Groups["data"].Value;
                    var contentType = base64Match.Groups["type"].Value;
                    var binData = Convert.FromBase64String(base64Data);
                    using (Image<Rgba32> image = Image.Load<Rgba32>(binData, out IImageFormat format))
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
                        outBase64 = image.ToBase64String(format); // Automatic encoder selected based on extension.
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
                            {
                                using (Image<Rgba32> image = Image.Load<Rgba32>(stream, out IImageFormat format))
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
                                    outBase64 = image.ToBase64String(format); // Automatic encoder selected based on extension.
                                }
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

                keyValuePair = new KeyValuePair<string, string>(key, outBase64);
                _KVPLite.SetKvp(keyValuePair);
            }
            return keyValuePair.Value;
        }

        public static string CreatePlaceholderImage(int width, int height)
        {
            string base64;
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

        public bool DeleteImage(string src, int width, int height)
        {
            string key = src + "&width=" + width + "&height=" + height;
            bool imageDeleted = _KVPLite.RemoveKvp(key);
            return imageDeleted;
        }
        public bool DeleteAllImages()
        {
            bool imagesDeleted = _KVPLite.RemoveAllKvp();
            return imagesDeleted;
        }

        public bool IsBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);

        }
        private static readonly Regex DataUriPattern = new Regex(@"^data\:(?<type>image\/(png|tiff|jpg|gif));base64,(?<data>[A-Z0-9\+\/\=]+)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
    }
}
