namespace LifeWorks.Application.Tests;

public class BlobFileStorageTests
{
    // Integration tests for BlobFileStorage.
    // Requires a real Azure Storage account or Azurite.
    // Set BLOB_TEST_CONNECTION_STRING env var to enable.
    // To run locally with Azurite: docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite

    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("BLOB_TEST_CONNECTION_STRING");

    [Fact(Skip = "Requires BLOB_TEST_CONNECTION_STRING environment variable")]
    public async Task Save_ThenOpenRead_ReturnsSameBytes()
    {
        // TODO: implement when running against real/emulated Azure Storage
        await Task.CompletedTask;
    }
}
