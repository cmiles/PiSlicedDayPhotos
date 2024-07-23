<Query Kind="Program">
  <NuGetReference>SSH.NET</NuGetReference>
  <Namespace>Renci.SshNet</Namespace>
</Query>

void Main(string[] args)
{
	Console.WriteLine("Starting Sftp PiSlicedDayPhotos to Local Directory - Version 7/22/2024");

	if (args.Length != 4)
	{
		Console.WriteLine("This program needs exactly 4 parameters:");
		Console.WriteLine(" username");
		Console.WriteLine(" password");
		Console.WriteLine(" host");
		Console.WriteLine(" local directory for photos");
		return;
	}

	var username = args[0];
	var piProgramDirectory = $"/home/{username}/SlicedPhotos";
	var password = args[1];
	var host = args[2];
	var localDirectoryForPhotos = args[3];

	CopyPiSlicedPhotosFromRemote(host, username, password, piProgramDirectory, localDirectoryForPhotos);
}

public static void CopyPiSlicedPhotosFromRemote(string machineName, string userName, string password, string copyFromDirectory, string copyToDirectory)
{
	Console.WriteLine($"Opening SFTP Connection to {machineName}");
	
	var sourceSftpConnection = new ConnectionInfo(machineName, userName, new Renci.SshNet.PasswordAuthenticationMethod(userName, password));

	using var client = new SftpClient(sourceSftpConnection);
	client.Connect();

	Console.WriteLine($"Getting remote file list from {copyFromDirectory}");

	var sourceFileList = client.ListDirectory(copyFromDirectory);

	var sourceJpegs = sourceFileList.Where(x => Path.GetExtension(x.Name).Equals(".jpg", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name).ToList();

	Console.WriteLine($"Found {sourceJpegs.Count()} .jpg files out of {sourceFileList.Count()} total source files");

	var targetDirectory = new DirectoryInfo(copyToDirectory);
	if (!targetDirectory.Exists)
	{
		Console.WriteLine($"The target directory {targetDirectory} doesn't exist - Creating...");
		targetDirectory.Create();
	}
	
	Console.WriteLine($"Listing *.jpg Files in {targetDirectory}");
	
	var targetJpegs = targetDirectory.EnumerateFiles("*.jpg").Select(x => x.Name).OrderBy(x => x).ToList();

	var toCopy = sourceJpegs.Where(x => !targetJpegs.Contains(x.Name)).OrderBy(x => x.Name).ToList();

	Console.WriteLine($"Found {toCopy.Count()} new files to copy - {targetJpegs.Count()} existing files in {targetDirectory.FullName}");

	foreach (var loopCopy in toCopy)
	{
		var newFileName = Path.Combine(copyToDirectory, loopCopy.Name);

		using Stream fileStream = File.Create(newFileName);
		client.DownloadFile(loopCopy.FullName, fileStream);

		Console.WriteLine($"  Copied {newFileName}");
	}
}