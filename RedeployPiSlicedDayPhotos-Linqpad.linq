<Query Kind="Program">
  <NuGetReference>SSH.NET</NuGetReference>
  <Namespace>Renci.SshNet</Namespace>
</Query>

void Main (string[] args)
{
	Console.WriteLine("Starting Redeply Pi Sliced Day Photos - version 7/22/2024");
	
	if(args.Length != 3){
		Console.WriteLine("This program needs exactly 3 parameters:");
		Console.WriteLine(" username");
		Console.WriteLine(" password");
		Console.WriteLine(" host");
		return;
	}

	var username = args[0];
	var piRemoteProgramDirectory = $"/home/{username}/PiSlicedDayPhotos";
	var password = args[1];
	var host = args[2];

	var piSlicedPhotosLocalPublishDirectory = @"M:\PiSlicedDayPhotos";

	CopyExecutableTo(host, username, password, piSlicedPhotosLocalPublishDirectory, piRemoteProgramDirectory);
}

public static void CopyExecutableTo(string machineName, string userName, string password, string copyFromDirectory, string copyToDirectory)
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