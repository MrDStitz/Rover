using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rover
{
    public abstract class RoverBase : ControllerBase
    {
        #region Settings
        internal const string ApiKey = "OEnUrRYeBEIGAVun4z1Wox8ftFeVsHjkNO1u7em7";
        internal const string ApiKeyParam = "api_key";
        internal const string BaseUrl = "https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/photos";
        internal const string EarthDateParam = "earth_date";
        internal const string LocalImageStorageDirectory = ".\\LocalImageStorage\\";
        internal const int MaxImagesForRover = 10;
        internal const string SourceDatesFileName = "dates.txt";
        #endregion
    }
}
