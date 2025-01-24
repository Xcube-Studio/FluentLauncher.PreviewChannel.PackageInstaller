using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace FluentLauncher.PreviewChannel.PackageInstaller.Scripts;

public class InstallScripts
{
    public const string Certification = """
        MIIDHDCCAgSgAwIBAgIQJ9btv5s1GrpAYNuif4HC8TANBgkqhkiG9w0BAQsFADAv
        MS0wKwYDVQQDEyQwNTNFRkIwRS02NzA1LTRBMTEtOTRCOS05ODBDNEM5RTAwNDcw
        HhcNMjQwNzAxMDYyNDEzWhcNMjUwNzAxMTIyNDEzWjAvMS0wKwYDVQQDEyQwNTNF
        RkIwRS02NzA1LTRBMTEtOTRCOS05ODBDNEM5RTAwNDcwggEiMA0GCSqGSIb3DQEB
        AQUAA4IBDwAwggEKAoIBAQDU/dm+LIjWovZNGdfkkFvsGkNLrDWWLWlz6MuZC4BV
        nedv3NyIYJxUt9WMo8BXdemQ7NqGRjtzthSGA1E8fB+UmC15lfMJZJ3L2nUIZG1d
        dOXP1/XW/3Xo97Qzm6Orx3AfJa3jSSRuflbUSvW0l6eEHsUTMYqa31lYiebI03H+
        eia8eVbgxzaMhmjHYEkv7gElFq5OtTIvZ4QONah/ijFeoFFTkvrFOxZtelcOqMRW
        P7E1I0dpZGswI68wmPfCdIcgTAj60JXy4N94mP7WMvdlIJvSFRM/CIvMUOP/ITYR
        YoWawI88rRplweQSNt6DnLy11Gz+rAboULUbKKiV3xlFAgMBAAGjNDAyMAwGA1Ud
        EwEB/wQCMAAwIgYDVR0lAQH/BBgwFgYIKwYBBQUHAwMGCisGAQQBgjdUAwEwDQYJ
        KoZIhvcNAQELBQADggEBADOL+F24yg9fqptdtGip4ZtlRvDfUTp3inYV2JUk3vHw
        nMjHQmGIFAEOifcXXh6E6T/MjepdvQIRMTjnVLinAPvq8Y2sKh5SQZCxED/rBukv
        Hmw1PElTMxrVo/fUOE5ASyZvcZ63EmeiFffYpjXkZBsT54e88BgOh2WP1D2Rczsx
        WhyLZufOx+nwa7KaH9shACa1va8PnPlQiNx5ynxkicXyVaAcpa0wVFwl8k+0qmLX
        RLnnzK2fbXQRfyIhKG1q1rZ6V2O8N8Xf19WiZmqVzxxm9EjVKVXMl5uG7Y/MVKOw
        ibZ+jVh3oqn2yxqQ/zCt5e//A5dUWPzIhA0f5eGaywQ=
        """;

    public static async Task InstallPackage(string packagePath, string[] dependencyPackagesPath,
        string? certificationPath = null, bool launchAfterInstalled = true, string? logFilePath = default)
    {
        TextWriter defaultWriter = Console.Out;
        StreamWriter? streamWriter = null;

        if (!string.IsNullOrEmpty(logFilePath))
        {
            try
            {
                FileInfo logFile = new(logFilePath);
                streamWriter = logFile.CreateText();

                Console.SetOut(streamWriter);
                Console.SetError(streamWriter);
            }
            catch { }
        }

        try
        {
            #region Parse Package

            string? packageName = null;

            using (var zipArchive = ZipFile.Open(packagePath, ZipArchiveMode.Read))
            {
                using var stream = zipArchive.Entries.First(entry => entry.FullName.EndsWith("AppxManifest.xml")).Open();
                using var streamReader = new StreamReader(stream);

                using var xmlReader = XmlReader.Create(streamReader);
                packageName = xmlReader.ReadToDescendant("Identity")
                    ? xmlReader.GetAttribute("Name")
                    : null;
            }

            Console.WriteLine($"Parsed Package Name: {packageName}");

            #endregion

            #region Check If Commands Exists

            static async Task CheckPowerShellCommand(string commandName)
            {
                using (var process = Process.Start(new ProcessStartInfo("powershell", $"Get-Command {commandName}"))
                    ?? throw new InvalidOperationException("couldn't start powershell process"))
                {
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine($"PowerShell Command [{commandName}]: Missing");
                        throw new NotSupportedException($"{commandName} Command does not exist");
                    }
                    else Console.WriteLine($"PowerShell Command [{commandName}]: Checked");
                };
            }

            await CheckPowerShellCommand("Add-AppxPackage");
            await CheckPowerShellCommand("Get-AppxPackage");

            #endregion

            #region Check If Package Installed

            bool isPackageInstalled = false;
            string? packageFamilyName = null;

            using (var process = Process.Start(new ProcessStartInfo("powershell", $"Get-AppxPackage -Name {packageName}")
            { RedirectStandardOutput = true }) ?? throw new InvalidOperationException("couldn't start powershell process"))
            {
                await process.WaitForExitAsync();
                string content = await process.StandardOutput.ReadToEndAsync();

                if (!string.IsNullOrEmpty(content))
                {
                    packageFamilyName = content.Split('\n').FirstOrDefault(line => line.Contains("PackageFamilyName"))?.Split(":")[1]?.Trim();

                    Console.WriteLine($"Found Package {packageFamilyName} Installed");
                    isPackageInstalled = true;
                }
            };

            #endregion

            #region Install Dependency Packages

            foreach (string path in dependencyPackagesPath)
            {
                using (var process = Process.Start(new ProcessStartInfo("powershell", $"Add-AppxPackage -Path \"{path}\" -ForceUpdateFromAnyVersion -ForceApplicationShutdown")
                { RedirectStandardOutput = true, RedirectStandardError = true }) ?? throw new InvalidOperationException("couldn't start powershell process"))
                {
                    await process.WaitForExitAsync();
                    string errors = await process.StandardError.ReadToEndAsync();

                    if (!string.IsNullOrEmpty(errors))
                    {
                        Console.Error.WriteLine($"Dependency Package [{path}] Installation Error:");
                        Console.Error.WriteLine(errors);

                        throw new InvalidOperationException($"counldn't install dependency package {path}");
                    }
                    else Console.WriteLine($"Dependency Package [{path}] Installed");
                };
            }

            #endregion

            #region Import Certification

            byte[] certificateBytes = certificationPath != null
                ? await File.ReadAllBytesAsync(certificationPath)
                : Convert.FromBase64String(Certification);

            using (var certificate = X509CertificateLoader.LoadCertificate(certificateBytes))
            {
                using X509Store store = new(StoreName.TrustedPeople, StoreLocation.LocalMachine);

                store.Open(OpenFlags.ReadWrite);
                store.Remove(certificate);
                store.Add(certificate);

                Console.WriteLine("Certification Imported");
            }

            #endregion

            #region Install/Update Package

            string forceUpdateOption = isPackageInstalled ? " -ForceUpdateFromAnyVersion" : string.Empty;
            using (var process = Process.Start(new ProcessStartInfo("powershell", $"Add-AppxPackage -Path \"{packagePath}\" -ForceApplicationShutdown" + forceUpdateOption)
            { RedirectStandardOutput = true, RedirectStandardError = true }) ?? throw new InvalidOperationException("couldn't start powershell process"))
            {
                await process.WaitForExitAsync();
                string errors = await process.StandardError.ReadToEndAsync();

                if (!string.IsNullOrEmpty(errors))
                {
                    Console.Error.WriteLine($"Application Package [{packagePath}] Installation Error:");
                    Console.Error.WriteLine(errors);

                    throw new InvalidOperationException($"counldn't install or update package {packagePath}");
                }
                else Console.WriteLine($"Application Package [{packagePath}] Installed/Updated");
            };

            #endregion

            #region Launch Application

            try
            {
                using (var process = Process.Start(new ProcessStartInfo("powershell", $"Get-AppxPackage -Name {packageName}")
                { RedirectStandardOutput = true }) ?? throw new InvalidOperationException("couldn't start powershell process"))
                {
                    await process.WaitForExitAsync();
                    string content = await process.StandardOutput.ReadToEndAsync();

                    if (string.IsNullOrEmpty(content))
                        throw new InvalidOperationException("counldn't get PackageFamilyName of installed package");

                    packageFamilyName = content.Split('\n').FirstOrDefault(line => line.Contains("PackageFamilyName"))?.Split(":")[1]?.Trim();
                };

                if (launchAfterInstalled)
                    Process.Start("explorer.exe", $"shell:AppsFolder\\{packageFamilyName}!App");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to Launch Application");
                Console.Error.WriteLine(ex);
            }

            #endregion
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            streamWriter?.Flush();
            streamWriter?.Close();

            Console.SetOut(defaultWriter);
            Console.SetError(defaultWriter);

            throw;
        }

        streamWriter?.Flush();
        streamWriter?.Close();
    }
}
