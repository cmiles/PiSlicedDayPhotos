#r "nuget: SSH.NET, 2024.1.0"

using Renci.SshNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Transactions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

// This is a dotnet-script file to help with gathering the photos from the PiSlicedDayPhotos program onto a central
// machine.
//
// There are many possible ways to script/run this - I currently run it via Pointless Waymarks PowerShell runner -
// https://github.com/cmiles/PointlessWaymarksProject/tree/main/PointlessWaymarks.PowershellRunnerGui -
// to save it with my other scripts, run it on a schedule and to make it easier to store the various
// hosts/usernames/passwords with 'good enough' security.
//
// To run this script you will need dotnet-script installed - see https://github.com/dotnet-script/dotnet-script -
// take care that it is appropriate and works with your setup but 'dotnet tool install -g dotnet-script' is usually a good
// place to start. You can then run this:
//   dotnet-script [PathToScript]\SftpPiSlicedDayPhotosToLocalDirectory.csx -- [PiUserName] [PiPassword] [PiHostName] [LocalDirectoryForPhotos]

Console.WriteLine("Starting Sftp PiSlicedDayPhotos to Local Directory - Version 7/22/2024");

if (Args.Count != 4)
{
    Console.WriteLine("This program needs exactly 4 parameters:");
    Console.WriteLine(" username");
    Console.WriteLine(" password");
    Console.WriteLine(" host");
    Console.WriteLine(" local directory for photos");
    return;
}

var username = Args[0];
var piProgramDirectory = $"/home/{username}/SlicedPhotos";
var password = Args[1];
var host = Args[2];
var localDirectoryForPhotos = Args[3];

CopyPiSlicedPhotosFromRemote(host, username, password, piProgramDirectory, localDirectoryForPhotos);

static void CopyPiSlicedPhotosFromRemote(string machineName, string userName, string password, string copyFromDirectory, string copyToDirectory)
{
    Console.WriteLine($"Opening SFTP Connection to {machineName}");
    
    var sourceSftpConnection = new ConnectionInfo(machineName, userName, new Renci.SshNet.PasswordAuthenticationMethod(userName, password));

    using var client = new SftpClient(sourceSftpConnection);
    client.Connect();

    Console.WriteLine($"Getting remote file list from {copyFromDirectory}");

    var sourceFileList = client.ListDirectory(copyFromDirectory);

    var sourceJpegs = sourceFileList.Where(x => Path.GetExtension(x.Name).Equals(".jpg", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name).ToList();

    Console.WriteLine($"Found {sourceJpegs.Count} .jpg files out of {sourceFileList.Count()} total source files");

    var targetDirectory = new DirectoryInfo(copyToDirectory);
    if (!targetDirectory.Exists)
    {
        Console.WriteLine($"The target directory {targetDirectory} doesn't exist - Creating...");
        targetDirectory.Create();
    }
    
    Console.WriteLine($"Listing *.jpg Files in {targetDirectory}");
    
    var targetJpegs = targetDirectory.EnumerateFiles("*.jpg").Select(x => x.Name).OrderBy(x => x).ToList();

    var toCopy = sourceJpegs.Where(x => !targetJpegs.Contains(x.Name)).OrderBy(x => x.Name).ToList();

    Console.WriteLine($"Found {toCopy.Count} new files to copy - {targetJpegs.Count} existing files in {targetDirectory.FullName}");

    foreach (var loopCopy in toCopy)
    {
        var newFileName = Path.Combine(copyToDirectory, loopCopy.Name);

        using Stream fileStream = File.Create(newFileName);
        client.DownloadFile(loopCopy.FullName, fileStream);

        Console.WriteLine($"  Copied {newFileName}");
    }
}