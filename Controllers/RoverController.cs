using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rover.Data;

namespace Rover.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RoverController : RoverBase
    {
        private readonly ILogger<RoverController> _logger;

        public RoverController(ILogger<RoverController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ContentResult Get()
        {
            if (!Directory.Exists(LocalImageStorageDirectory))
            {
                Directory.CreateDirectory(LocalImageStorageDirectory);
            }

            var sourceDatesRaw = System.IO.File.ReadAllText(SourceDatesFileName);
            var sourceDates = JsonSerializer.Deserialize<List<string>>(sourceDatesRaw);
            
            //Request image data from the NASA Api for a given set of dates
            var responses = ExecuteImageInfoRequest(sourceDates);

            //Retrieve image file details as HTML anchor tage from local storage
            var imageTags = GetImageTags();

            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = $"<html><body>{imageTags}</body></html>"
            };
        }

        private List<bool> ExecuteImageInfoRequest(List<string> sourceDates)
        {
            var getImageTasks = sourceDates.Select(sourceDate => Task.Factory.StartNew(() => SendRequest(sourceDate))).ToArray();
            var tasks = Task.WhenAll(getImageTasks);
            tasks.Wait();

            return tasks.Result.ToList();
        }

        private bool SendRequest(string rawDate)
        {
            if (!DateTime.TryParse(rawDate, out var date))
            {
                _logger.LogWarning($"Unable to parse date, raw value [{rawDate}]... skipping");
                return false;
            }

            var formattedDate = date.ToString("yyyy-MM-dd");
            var responseContent = string.Empty;
            var requestUrl = $"{BaseUrl}?{EarthDateParam}={formattedDate}&{ApiKeyParam}={ApiKey}";

            var webRequest = WebRequest.CreateHttp(requestUrl);
            webRequest.Method = "GET";
            webRequest.ContentLength = 0;
            webRequest.ContentType = "application/json";

            using (var webResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                if (webResponse.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogWarning($"Error returned [{webResponse.StatusCode}] from request Url [{requestUrl}]");
                    return false;
                }

                var stream = webResponse.GetResponseStream();
                if (stream == null)
                {
                    _logger.LogWarning($"ResponseStream is null for request [{requestUrl}]... skipping");
                    return false;
                }

                using (var reader = new StreamReader(stream))
                {
                    responseContent = reader.ReadToEnd();

                    var data = JsonSerializer.Deserialize<RoverInfo>(responseContent);

                    var rovers = data.Photos.Select(photo => photo.Rover.Name).Distinct();
                    foreach (var rover in rovers)
                    {
                        var roverPhotos = data.Photos.Where(photo => photo.Rover.Name == rover).Take(MaxImagesForRover);

                        //Retieve the actual image file for each image returned in the first call so we can store it locally
                        var responses = ExecuteImageRequest(roverPhotos);
                    }
                }
            }
            return true;
        }

        private List<bool> ExecuteImageRequest(IEnumerable<Photo> roverPhotos)
        {
            var getRoverPhotoTasks = roverPhotos.Select(roverPhoto => Task.Factory.StartNew(() => SendImageRequest(roverPhoto))).ToArray();
            var tasks = Task.WhenAll(getRoverPhotoTasks);
            tasks.Wait();

            return tasks.Result.ToList();
        }

        private bool SendImageRequest(Photo roverPhoto)
        {
            try
            {
                var responseImageContent = string.Empty;
                var imageUrl = roverPhoto.ImageSource;

                if (string.IsNullOrEmpty(imageUrl))
                {
                    _logger.LogWarning($"ImageUrl is null or empty for rover [{roverPhoto.Rover.Name}], Earth date [{roverPhoto.EarthDate}]");
                    return false;
                }

                var webImageRequest = WebRequest.CreateHttp(imageUrl);
                webImageRequest.Method = "GET";
                webImageRequest.ContentLength = 0;

                using (var webImageResponse = (HttpWebResponse)webImageRequest.GetResponse())
                {
                    var imageStream = webImageResponse.GetResponseStream();
                    if (imageStream == null)
                    {
                        _logger.LogWarning($"ImageStream is null for rover image [{roverPhoto.Id}], [{roverPhoto.Rover.Name}], Earth date [{roverPhoto.EarthDate}]");
                        return false;
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        imageStream.CopyTo(memoryStream);

                        var imageBytes = memoryStream.ToArray();

                        System.IO.File.WriteAllBytes($"{LocalImageStorageDirectory}Rover.{roverPhoto.Rover.Name}.{roverPhoto.EarthDate}.{roverPhoto.Camera.FullName}.{roverPhoto.Id}.jpg", imageBytes);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error in SendImageRequest for rover image [{roverPhoto.Id}], [{roverPhoto.Rover.Name}], Earth date [{roverPhoto.EarthDate}], Message: [{ex.Message}], StackTrace: [{ex.StackTrace}]");
                return false;
            }
        }

        private static string GetImageTags()
        {
            var imageTags = string.Empty;
            foreach (var imageFile in Directory.GetFiles(LocalImageStorageDirectory, "*.jpg"))
            {
                var fileNameSplit = imageFile.Split('.');
                imageTags += $"<img src='{imageFile}' title='Rover: {fileNameSplit[2]}, EarthDate: {fileNameSplit[3]}, Camera: {fileNameSplit[4]} '>";
            }

            return imageTags;
        }
    }
}
