<Query Kind="Program">
  <NuGetReference>SSH.NET</NuGetReference>
  <Namespace>Renci.SshNet</Namespace>
</Query>

/// <summary>This Linqpad script can be an easy way to re-deploy the Pi Sliced Day program especially
/// if you are running it on multiple Pis. This is NOT appropriate for use installing the program (it
/// does not create the directory structures, set up a service, ...) but for re-deploying the program
/// it is suitable because it will not overwrite your settings or sunrise/sunset files.
/// </summary>
void Main()
{
	var username = "[username]";
	var password = "[password]";
	var publishDirectory = @"M:\PiSlicedDayPhotos";
	var piProgramDirectory = $"/home/{username}/PiSlicedDayPhotos";

	CopyExecutableTo("[pi 1 network name]", username, password, publishDirectory, piProgramDirectory);
	//CopyExecutableTo("[pi 2 network name]", username, password, publishDirectory, piProgramDirectory);
	//CopyExecutableTo("[pi 3 network name]", username, password, publishDirectory, piProgramDirectory);
}

public static void CopyExecutableTo(string machineName, string userName, string password, string copyFromDirectory, string copyToDirectory)
{
var connectionInfo = new ConnectionInfo(machineName, userName, new Renci.SshNet.PasswordAuthenticationMethod(userName, password));

var uploadList = new List<FileInfo> {
		new FileInfo(Path.Combine(copyFromDirectory, "PiSlicedDayPhotos")),
		new FileInfo(Path.Combine(copyFromDirectory, "README.md")),
		new FileInfo(Path.Combine(copyFromDirectory, "LICENSE"))
	};

	using var sshClient = new SshClient(connectionInfo);

	try
	{
		sshClient.Connect();

		sshClient.RunCommand("sudo systemctl stop pisliceddayphotos");

		using var sftpClient = new SftpClient(connectionInfo);
		sftpClient.Connect();

		foreach (var loopFile in uploadList)
		{
			using Stream fileStream = File.Open(loopFile.FullName, FileMode.Open);
			sftpClient.UploadFile(fileStream, $"{copyToDirectory}/{loopFile.Name}");

			Console.WriteLine($"{machineName}: Copied {loopFile.Name} to {$"{copyToDirectory}/{loopFile.Name}"}");
		}
	}
	finally
	{
		sshClient.RunCommand("sudo systemctl start pisliceddayphotos");
	}
}