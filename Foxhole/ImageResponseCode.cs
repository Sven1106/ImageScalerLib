using System;
using System.Collections.Generic;
using System.Text;

namespace Foxhole
{
    public enum ImageResponseCode
    {
        NotSet = 0,
        ImageFromBase64Inserted = 100,
        ImageFromSrcInserted = 200,
        ImageExists = 300,
        Base64ValueWasNotValid = 400,
        RemoteServerNotFound = 500,
    }
}
