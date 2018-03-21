using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using InstaSharper;
using InstaSharper.API.Builder;
using InstaSharper.API;
using InstaSharper.Classes;
using InstaSharper.Logger;
using InstaSharper.Classes.Models;

namespace Instagram_Uploader
{
    class Program
    {
        static void Main(string[] args)
        {
            var sb = new StringBuilder();
            foreach (var x in args) { sb.Append(x); }
            Instagram.UploadPhoto(sb.ToString());
            while (true)
            {
                Console.ReadLine();
            }
        }
    }

    class Instagram
    {
        private static IInstaApi _instaApi;

        public static async void UploadPhoto(string path)
        {
            const string stateFile = "state.bin";

            var userSession = new UserSessionData
            {
                UserName = File.ReadAllLines("LoginInfo.txt")[0],
                Password = File.ReadAllLines("LoginInfo.txt")[1]
            };

            Console.WriteLine("Initializing API");

            _instaApi = InstaApiBuilder.CreateBuilder()
                    .SetUser(userSession)
                    .UseLogger(new DebugLogger(LogLevel.Exceptions))
                    .SetRequestDelay(TimeSpan.FromSeconds(2))
                    .Build();

            Console.WriteLine($"Logging into {userSession.UserName}");

            var logInResult = await _instaApi.LoginAsync();

            if (!logInResult.Succeeded)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Login failed => {logInResult.Info.Message}");
                return;
            }
            else
            {
                Console.WriteLine("Login successfull");
            }

            var state = _instaApi.GetStateDataAsStream();
            using (var fileStream = File.Create(stateFile))
            {
                state.Seek(0, SeekOrigin.Begin);
                state.CopyTo(fileStream);
            }

            Console.WriteLine("API initialized succesfully ");

            var upl = new UploadPhoto(_instaApi);

            Console.WriteLine("Press ENTER and type optional caption (leave blank if undesired)");

            string caption = Console.ReadLine();

            Console.WriteLine($"Uploading with caption {caption}");

            await upl.UploadImg(caption, path);

        }
    }

    class UploadPhoto
    {
        private readonly IInstaApi _instaApi;

        public UploadPhoto(IInstaApi instaApi)
        {
            _instaApi = instaApi;
        }

        public async Task UploadImg(string caption, string localPath)
        {
            var mediaImage = new InstaImage
            {
                Height = 1080,
                Width = 1080,
                URI = new Uri(Path.GetFullPath(localPath), UriKind.Absolute).LocalPath
            };
            var result = await _instaApi.UploadPhotoAsync(mediaImage, caption);
            Console.WriteLine(result.Succeeded
                ? $"Media created: {result.Value.Pk}, {result.Value.Caption}"
                : $"Unable to upload photo: {result.Info.Message}");
            Console.ReadLine();
        }
    }
}
