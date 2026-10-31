using QualiTrack.Infrastructure.Storage;

namespace QualiTrack.Api.IntegrationTests.Storage;

public sealed class LocalFileStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"qualitrack-files-{Guid.NewGuid():N}");

    [Fact]
    public async Task A_saved_file_reads_back_byte_for_byte_by_its_key()
    {
        var store = new LocalFileStore(_root);
        byte[] content = [1, 2, 3, 4, 5];

        var key = await store.SaveAsync(new MemoryStream(content), "photo.png", CancellationToken.None);
        await using var read = await store.OpenReadAsync(key, CancellationToken.None);

        using var copy = new MemoryStream();
        await read.CopyToAsync(copy);
        Assert.Equal(content, copy.ToArray());
    }

    [Fact]
    public async Task The_folder_is_created_on_first_save()
    {
        var store = new LocalFileStore(_root);

        await store.SaveAsync(new MemoryStream([1]), "photo.png", CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(_root, "photo.png")));
    }

    [Theory]
    [InlineData("../outside.png")]
    [InlineData("..\\outside.png")]
    [InlineData("nested/photo.png")]
    [InlineData("")]
    public async Task A_name_that_is_not_a_plain_file_name_is_refused_so_nothing_is_written_outside_the_folder(string name)
    {
        var store = new LocalFileStore(_root);

        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(new MemoryStream([1]), name, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => store.OpenReadAsync(name, CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
