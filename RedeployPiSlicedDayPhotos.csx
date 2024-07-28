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

// This is a dotnet-script file to help with redeploying the PiSlicedDayPhotos program to a Raspberry Pi - if you
// are only running a single pi/instance of the program this is probably not that useful. But if you are running 
// a number of cameras this might be useful to re-deploy to all of them.
//
// There are many possible ways to script/run this - I currently run it via Pointless Waymarks PowerShell runner -
// https://github.com/cmiles/PointlessWaymarksProject/tree/main/PointlessWaymarks.PowershellRunnerGui -
// to save it with my other scripts and to make it easier to store the various hosts/usernames/passwords
// with 'good enough' security.
//
// To run this script you will need dotnet-script installed - see https://github.com/dotnet-script/dotnet-script -
// take care that it is appropriate and works with your setup but 'dotnet tool install -g dotnet-script' is usually a good
// place to start. You can then run this:
//   dotnet-script [Path]\RedeployPiSlicedDayPhotos.csx -- [PiUserName] [PiPassword] [PiHostName]

Console.WriteLine("Starting Redeply Pi Sliced Day Photos - version 7/22/2024");

if(Args.Count != 3){
    Console.WriteLine("This program needs exactly 3 parameters:");
    Console.WriteLine(" username");
    Console.WriteLine(" password");
    Console.WriteLine(" host");
    return;
}

var username = Args[0];
var piRemoteProgramDirectory = $"/home/{username}/PiSlicedDayPhotos";
var password = Args[1];
var host = Args[2];

var piSlicedPhotosLocalPublishDirectory = @"M:\PiSlicedDayPhotos";

CopyExecutableTo(host, username, password, piSlicedPhotosLocalPublishDirectory, piRemoteProgramDirectory);


static void CopyExecutableTo(string machineName, string userName, string password, string copyFromDirectory, string copyToDirectory)
{
    var connectionInfo = new ConnectionInfo(machineName, userName, new Renci.SshNet.PasswordAuthenticationMethod(userName, password));

    var uploadList = new List<FileInfo> {
        new FileInfo(Path.Combine(copyFromDirectory, "PiSlicedDayPhotos")),
        new FileInfo(Path.Combine(copyFromDirectory, "README.md")),
        new FileInfo(Path.Combine(copyFromDirectory, "LICENSE"))
    };

    Console.WriteLine($"Files to Copy: {string.Join(", ", uploadList.Select(x => x.FullName))}");

    using var sshClient = new SshClient(connectionInfo);

    try
    {
        Console.WriteLine($"Connecting via SSH to {machineName}");
        sshClient.Connect();

        Console.WriteLine("Stopping pisliceddayphotos");
        sshClient.RunCommand("sudo systemctl stop pisliceddayphotos");

        Console.WriteLine($"Connecting via SFTP to {machineName}");
        using var sftpClient = new SftpClient(connectionInfo);
        sftpClient.Connect();

        var counter = 0;
        
        foreach (var loopFile in uploadList)
        {
            Console.WriteLine($"Uploading file {++counter} of {uploadList.Count()}");
            using Stream fileStream = File.Open(loopFile.FullName, FileMode.Open);
            sftpClient.UploadFile(fileStream, $"{copyToDirectory}/{loopFile.Name}");

            Console.WriteLine($"{machineName}: Copied {loopFile.Name} to {$"{copyToDirectory}/{loopFile.Name}"}");
        }
    }
    finally
    {
        Console.WriteLine("Starting pisliceddayphotos");
        sshClient.RunCommand("sudo systemctl start pisliceddayphotos");
    }
}