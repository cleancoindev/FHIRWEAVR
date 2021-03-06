﻿// Sends data not yet on server to it via HTTP POST (uses own thread).

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;

public class PushData
{

    public void Upload(string fileToUpload, string host)
    {
        Thread uploadWorker = new Thread(() => UploadCore(fileToUpload, host));
        uploadWorker.Start();
    }

    public void ManualUpload(string fileToUpload, string filePath, string host)
    {
        Thread manualUploadWorker = new Thread(() => ManualUploadCore(fileToUpload, filePath, host));
        manualUploadWorker.Start();
    }

    public bool FileExistsAtURL(string url)
    {
        bool returnValue = false;
        Thread fileWorker = new Thread(() => returnValue = FileExistsAtURLCore(url));
        fileWorker.Start();

        // Join so we can return value
        fileWorker.Join();

        return returnValue;
    }

    void UploadCore(string fileToUpload, string host)
    {
        // File check function can also be used to check if a host exists
        if (!FileExistsAtURLCore(host))
        {
            Debug.LogError("Host is offline or does not support TLS 1.0.");
        }

        // Note that HTTP allows additional / characters so don't need to check final character
        else if (!FileExistsAtURLCore(host + "/VirZOOM-device-profile.xml"))
        {
            UploadViaClient(host, DataHandler.path + "VirZOOM-device-profile.xml");
        }

        // For 'last' keyword as first parameter to push last saved file
        if (fileToUpload == "last")
        {
            var folder = new DirectoryInfo(DataHandler.path);
            var lastFile = folder.GetFiles().OrderByDescending(f => f.LastWriteTime).First();
            fileToUpload = lastFile.Name;
        }

        string fullFilePath = DataHandler.path + fileToUpload;

        if (!(File.Exists(fullFilePath)))
        {
            Debug.LogError("File not found. Please check the filename or use ManualUpload(string " +
                "fileToUpload, string filePath, string host) for non-default save locations.");
            throw new ArgumentException("File not found.", "fileToUpload");
        }

        UploadViaClient(host, fullFilePath);
    }

    void ManualUploadCore(string fileToUpload, string filePath, string host)
    {
        char lastPathChar = filePath[filePath.Length - 1];

        // Note that DOS uses / and \ interchangeably, even allowing both in a single path.
        // Note \\ is an escape on \.
        if (lastPathChar != '/' || lastPathChar != '\\')
        {
            filePath = filePath + Path.DirectorySeparatorChar;
        }

        string fullFilePath = filePath + fileToUpload;

        if (!(File.Exists(fullFilePath)))
        {
            Debug.LogError("File not found. Please check the filename and path.");
            throw new ArgumentException("File not found.", "fileToUpload");
        }

        UploadViaClient(host, fullFilePath);
    }

    void UploadViaClient(string host, string fileToUpload)
    {
        // Wrap WebClient in using block so it is only accessible in the block scope
        using (WebClient FHIRClient = new WebClient())
        {
            FHIRClient.UploadFile(host, fileToUpload);
        }
    }

    bool FileExistsAtURLCore(string url)
    {
        try
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "HEAD";

            // If HTTP status is 200, file exists
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            response.Close();
            return (response.StatusCode == HttpStatusCode.OK);
        }
        catch
        {
            Debug.Log("Could not read server status.");
            return false;
        }
    }

}
