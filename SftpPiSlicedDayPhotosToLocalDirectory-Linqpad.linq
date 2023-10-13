<Query Kind="Program">
  <NuGetReference>SSH.NET</NuGetReference>
  <Namespace>Renci.SshNet</Namespace>
</Query>

/// <summary>This is a simple Linqpad script to transfer Pi Sliced Day Photographs from a Pi to another computer via SFTP.
/// There are an endless number of ways to do this - but running this script via the Windows Task Scheduler with lprun?.exe
/// is one simple solution.
/// </summary>
void Main()
{
	var username = "[username]";
	var password = "[password]";
	var piPhotoDirectory = $"/home/{username}/SlicedPhotos";
	var localPhotoDirectory = @"M:\GmhDataWell\SolarSepticPhotos\";

	CopyFrom("[pi 1 network name]", username, password, piPhotoDirectory, localPhotoDirectory);
	//CopyFrom("[pi 2 network name]", username, password, piPhotoDirectory, localPhotoDirectory);
	//CopyFrom("[pi 3 network name]", username, password, piPhotoDirectory, localPhotoDirectory);
}

public static void CopyFrom(string machineName, string userName, string password, string copyFromDirectory, string copyToDirectory)
{
	var sourceSftpConnection = new ConnectionInfo(machineName, userName, new Renci.SshNet.PasswordAuthenticationMethod(userName, password));

	using var client = new SftpClient(sourceSftpConnection);
	
	client.Connect();

	var sourceFileList = client.ListDirectory(copyFromDirectory);

	var sourceJpegs = sourceFileList.Where(x => Path.GetExtension(x.Name).Equals(".jpg", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name).ToList();

	Console.WriteLine($"Found {sourceJpegs.Count()} .jpg files out of {sourceFileList.Count()} total source files");

	var targetDirectory = new DirectoryInfo(copyToDirectory);
	
	if (!targetDirectory.Exists) targetDirectory.Create();

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