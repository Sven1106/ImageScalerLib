using Foxhole;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Foxhole.Tests
{
    [TestFixture]
    public class Tests
    {
        public Tests()
        {
            ImageCache ImageCache = new ImageCache();
            ImageCache.DeleteAllImages();
        }

        [Test]
        public void TryGet_One_ImageFromBase64Inserted_Success()
        {
            ImageCache ImageCache = new ImageCache();
            var scaledImage = ImageCache.TryGetScaledImage("data:image/png;base64,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", 150, 150);
            Assert.IsTrue(scaledImage.ResponseCode == ImageResponseCode.ImageFromBase64Inserted);
        }
        [Test]
        public void TryGet_One_ImageFromSrcInserted_Success()
        {
            ImageCache ImageCache = new ImageCache();
            var scaledImage = ImageCache.TryGetScaledImage("https://cdn.pixabay.com/photo/2016/06/05/23/13/random-1438434_960_720.png", 150, 150);
            Assert.IsTrue(scaledImage.ResponseCode == ImageResponseCode.ImageFromSrcInserted);
        }
        [Test]
        public void TryGet_Many_ImageFromSrcInserted_Success()
        {
            ImageCache ImageCache = new ImageCache();
            List<ScaledImage> scaledImages = new List<ScaledImage>();

            for (int i = 0; i < 100; i++)
            {
                var scaledImage = ImageCache.TryGetScaledImage("https://cdn.pixabay.com/photo/2019/02/21/14/48/bratislava-4011506_960_720.jpg", 150 + i, 150 + i);
                scaledImages.Add(scaledImage);
            }
            Assert.IsTrue(scaledImages.All(x => x.ResponseCode == ImageResponseCode.ImageFromSrcInserted));
        }

        [Test]
        public void TryGet_One_ExistingImage_Success()
        {
            ImageCache ImageCache = new ImageCache();
            ImageCache.TryGetScaledImage("https://cdn.pixabay.com/photo/2016/12/06/17/11/torii-1886975_960_720.jpg", 150, 150);
            var scaledImage = ImageCache.TryGetScaledImage("https://cdn.pixabay.com/photo/2016/12/06/17/11/torii-1886975_960_720.jpg", 150, 150);
            Assert.IsTrue(scaledImage.ResponseCode == ImageResponseCode.ImageExists);
        }

        [Test]
        public void TryGet_One_InvalidBase64Value()
        {
            ImageCache ImageCache = new ImageCache();
            var scaledImage = ImageCache.TryGetScaledImage("data:image/jpeg;base64,/", 150, 150);
            Assert.IsTrue(scaledImage.ResponseCode == ImageResponseCode.Base64ValueWasNotValid);
        }
        [Test]
        public void TryGet_One_InvalidSrc()
        {
            ImageCache ImageCache = new ImageCache();
            var scaledImage = ImageCache.TryGetScaledImage("https://videnskab.dk/files/article_media/koala_1s", 150, 150);
            Assert.IsTrue(scaledImage.ResponseCode == ImageResponseCode.RemoteServerNotFound);
        }
    }
}