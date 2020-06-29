using System;
using System.Collections.Generic;
using System.Text;

namespace Foxhole
{
    public class ScaledImage
    {
        public ImageResponseCode ResponseCode { get; set; }
        public KeyValuePair<string, string> Image { get; set; }
    }
}
