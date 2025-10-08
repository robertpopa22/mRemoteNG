using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Web;

namespace ExternalConnectors.OP;

public class OnePasswordCliException : Exception
{
	public OnePasswordCliException(string message) : base(message)
	{
	}
}

public class OnePasswordCli
{
	private const string OnePasswordCliExecutable = "op.exe";
	private const string UserNamePurpose = "USERNAME";
	private const string PasswordPurpose = "PASSWORD";
	private const string StringType = "STRING";
	private const string SshKeyType = "SSHKEY";
	private const string DomainLabel = "domain";

	private record VaultUrl(string Label, string Href);

	private record VaultField(string Id, string Label, string Type, string Purpose, string Value);

	private record VaultItem(VaultUrl[]? Urls, VaultField[]? Fields);

	private static readonly JsonSerializerOptions JsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public static void ReadPassword(string input, out string username, out string password, out string domain, out string privateKey)
	{
		var inputUrl = new Uri(input);
		var vault = WebUtility.UrlDecode(inputUrl.Host);
		var queryParams = HttpUtility.ParseQueryString(inputUrl.Query);
		var account = queryParams["account"];
		var item = WebUtility.UrlDecode(inputUrl.AbsolutePath.TrimStart('/'));
		ItemGet(item, vault, account, out username, out password, out domain, out privateKey);
	}

	private static void ItemGet(string item, string? vault, string? account, out string username, out string password, out string domain, out string privateKey)
	{
		var args = new List<string> { "item", "get", item };

		if (!string.IsNullOrEmpty(account))
		{
			args.Add("--account");
			args.Add(account);
		}

		if (!string.IsNullOrEmpty(vault))
		{
			args.Add("--vault");
			args.Add(vault);
		}

		args.Add("--format");
		args.Add("json");

		var exitCode = RunCommand(OnePasswordCliExecutable, args, out var output, out var error);
		if (exitCode != 0)
		{
			username = string.Empty;
			password = string.Empty;
			privateKey = string.Empty;
			domain = string.Empty;
			throw new OnePasswordCliException($"Error running op item get: {error}");
		}

		var items = JsonSerializer.Deserialize<VaultItem>(output, JsonSerializerOptions) ??
		            throw new OnePasswordCliException("1Password returned null");
		username = items.Fields?.FirstOrDefault(x => x.Purpose == UserNamePurpose)?.Value ?? string.Empty;
		password = items.Fields?.FirstOrDefault(x => x.Purpose == PasswordPurpose)?.Value ?? string.Empty;
		privateKey = items.Fields?.FirstOrDefault(x => x.Type == SshKeyType)?.Value ?? string.Empty;
		domain = items.Fields?.FirstOrDefault(x => x.Type == StringType && x.Label == DomainLabel)?.Value ?? string.Empty;
	}

	private static int RunCommand(string command, IReadOnlyCollection<string> arguments, out string output,
		out string error)
	{
		var processStartInfo = new ProcessStartInfo
		{
			FileName = command,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		foreach (var argument in arguments)
		{
			processStartInfo.ArgumentList.Add(argument);
		}

		using var process = new Process();
		process.StartInfo = processStartInfo;
		process.Start();
		output = process.StandardOutput.ReadToEnd();
		error = process.StandardError.ReadToEnd();
		process.WaitForExit();
		return process.ExitCode;
	}
}