using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Exceptions.Database;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace QuestingBots.Server.Internal;

// Copied from https://github.com/sp-tarkov/server-csharp/blob/main/SPTarkov.Server/Helpers/DatabaseImporter.cs

public sealed class DatabaseImporter(
    ISptLogger<DatabaseImporter> logger,
    ServerLocalisationService serverLocalisationService,
    ImporterUtil importerUtil,
    JsonUtil jsonUtil
)
{
    private const string SptDataPath = "./SPT_Data/";
    private readonly Dictionary<string, string> _databaseHashes = [];

    public async Task LoadHashesAsync(CancellationToken cancellationToken = default)
    {
        var checksFilePath = Path.Combine(SptDataPath, "checks.dat");

        try
        {
            if (File.Exists(checksFilePath))
            {
                await using var fs = File.OpenRead(checksFilePath);

                using var reader = new StreamReader(fs, Encoding.ASCII);
                var base64Content = await reader.ReadToEndAsync(cancellationToken);

                var jsonBytes = Convert.FromBase64String(base64Content);

                await using var ms = new MemoryStream(jsonBytes);

                var FileHashes = await jsonUtil.DeserializeFromMemoryStreamAsync<List<FileHash>>(ms, cancellationToken) ?? [];

                foreach (var hash in FileHashes)
                {
                    _databaseHashes.Add(hash.Path, hash.Hash);
                }
            }
            else
            {
                logger.Error(serverLocalisationService.GetText("validation_error_exception", checksFilePath));
            }
        }
        catch (Exception)
        {
            logger.Error(serverLocalisationService.GetText("validation_error_exception", checksFilePath));
        }
    }

    /// <summary>
    /// Read all json files in database folder and map into a json object
    /// </summary>
    /// <param name="filePath">path to database folder</param>
    /// <param name="shouldVerifyDatabase">if the database should be verified after deserialization</param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> that can be used to cancel the database hydration operation.
    /// </param>
    /// <returns></returns>
    public async Task<DatabaseTables?> LoadDatabaseAsync(bool shouldVerifyDatabase, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.Info(serverLocalisationService.GetText("importing_database"));
            Stopwatch timer = new();
            timer.Start();

            var dataToImport = await importerUtil.LoadRecursiveAsync<DatabaseTables>(
                $"{SptDataPath}database/",
                shouldVerifyDatabase ? VerifyDatabaseAsync : null,
                cancellationToken: cancellationToken
            );

            timer.Stop();

            logger.Info(serverLocalisationService.GetText("importing_database_finish"));
            logger.Debug($"Database import took {timer.ElapsedMilliseconds}ms");

            return dataToImport;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.Warning("Database import was cancelled.");

            throw;
        }
    }

    public async Task VerifyDatabaseAsync(string fileName, CancellationToken cancellationToken)
    {
        var relativePath = fileName.StartsWith(SptDataPath, StringComparison.OrdinalIgnoreCase) ? fileName[SptDataPath.Length..] : fileName;

        using var md5 = MD5.Create();
        await using var stream = File.OpenRead(fileName);
        var hashBytes = await md5.ComputeHashAsync(stream, cancellationToken);
        var hashString = Convert.ToHexString(hashBytes);

        if (!_databaseHashes.TryGetValue(relativePath, out var expectedHash) || expectedHash != hashString)
        {
            throw new ValidationErrorException(serverLocalisationService.GetText("validation_error_file", fileName));
        }
    }

    private class FileHash
    {
        public string Path { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
    }
}
