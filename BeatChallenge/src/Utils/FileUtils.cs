using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;
using BeatChallenge.Utils;

namespace BeatChallenge.Utils
{
    public class FileUtils
    {

        public static void MoveFilesRecursively(DirectoryInfo source, DirectoryInfo target, bool moveOld)
        {
            try
            {
                foreach (DirectoryInfo dir in source.GetDirectories())
                {
                    MoveFilesRecursively(dir, target.CreateSubdirectory(dir.Name), moveOld);
                }
                foreach (FileInfo file in source.GetFiles())
                {
                    if (moveOld && File.Exists(Path.Combine(target.FullName, file.Name)))
                    {
                        File.Move(Path.Combine(target.FullName, file.Name), Path.Combine(target.FullName, $"{file.Name}.old"));
                    }
                    file.MoveTo(Path.Combine(target.FullName, file.Name));
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public static void EmptyDirectory(string directory, bool delete = true)
        {
            if (Directory.Exists(directory))
            {
                var directoryInfo = new DirectoryInfo(directory);
                foreach (System.IO.FileInfo file in directoryInfo.GetFiles()) file.Delete();
                foreach (System.IO.DirectoryInfo subDirectory in directoryInfo.GetDirectories()) subDirectory.Delete(true);

                if (delete) Directory.Delete(directory);
            }
        }

        public static IEnumerator ExtractZip(string zipPath, string extractPath, string cachePath, bool moveOld)
        {
            if (File.Exists(zipPath))
            {
                bool extracted = false;
                try
                {
                    ZipFile.ExtractToDirectory(zipPath, $"{cachePath}");
                    extracted = true;
                }
                catch (Exception e)
                {
                    Logger.Debug($"An error occured while trying to extract \"{zipPath}\"!");
                    Logger.Error(e);
                    yield break;
                }

                yield return new WaitForSeconds(0.25f);

                File.Delete(zipPath);

                try
                {
                    if (extracted)
                    {
                        if (!Directory.Exists(extractPath))
                            Directory.CreateDirectory(extractPath);

                        MoveFilesRecursively(new DirectoryInfo($"{Environment.CurrentDirectory}\\{ cachePath }"), new DirectoryInfo(extractPath), moveOld);
                    }
                }
                catch (Exception e)
                {
                    Logger.Debug($"An exception occured while trying to move files into their final directory! {e.ToString()}");
                    Logger.Error(e);
                }
            }
        }

        public static IEnumerator DownloadFile(string url, string path, Action<float> progressChanged)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                float initTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                UnityWebRequestAsyncOperation req = www.SendWebRequest();

                while (!req.isDone)
                {
                    yield return null;
                    
                    if (new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() - initTime > 5 && req.progress == 0f)
                    {
                        Logger.Error("Did not download anything within 5 second, aborting download");
                        progressChanged?.Invoke(-1f);
                        www.Abort();
                        yield break;

                    }
                    progressChanged?.Invoke(req.progress);
                }
                if (www.isNetworkError || www.isHttpError)
                {
                    Logger.Error($"Http request error! {www.error}");
                    progressChanged?.Invoke(-1f);
                    yield break;
                }
                Logger.Debug($"Success downloading \"{url}\"");
                byte[] data = www.downloadHandler.data;
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                    File.WriteAllBytes(path, data);
                    Logger.Debug("Downloaded file!");
                }
                catch (Exception)
                {
                    Logger.Error("Failed to download file!");
                    progressChanged?.Invoke(-1f);
                    yield break;
                }
            }
        }
    }
}
