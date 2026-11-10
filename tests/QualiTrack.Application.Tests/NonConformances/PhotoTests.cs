using QualiTrack.Application.Abstractions;
using QualiTrack.Application.NonConformances;
using QualiTrack.Application.Tests.Fakes;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.Tests.NonConformances;

public sealed class PhotoTests
{
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private readonly InMemoryNcrRepository _repository = new();
    private readonly InMemoryFileStore _files = new();
    private readonly CountingUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _raiser = new(Ncrs.Inspector, Role.Inspector);

    [Fact]
    public async Task The_raiser_attaches_a_photo_which_is_stored_and_recorded_on_the_ncr()
    {
        var ncr = Stored(Ncrs.Open());

        var result = await AttachAsync(ncr.Id, _png, _raiser);

        Assert.True(result.IsSuccess);
        Assert.Same(PhotoFormat.Png, ncr.Photo);
        Assert.Equal(_png, _files.Files[$"{ncr.Id}.png"]);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task A_file_that_is_not_a_jpeg_or_png_is_refused_and_nothing_is_stored()
    {
        var ncr = Stored(Ncrs.Open());

        var result = await AttachAsync(ncr.Id, "GIF89a.."u8.ToArray(), _raiser);

        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Empty(_files.Files);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(Role.Investigator)]
    [InlineData(Role.QaManager)]
    public async Task Only_an_inspector_can_attach_a_photo(Role role)
    {
        var ncr = Stored(Ncrs.Open());

        var result = await AttachAsync(ncr.Id, _png, new FakeCurrentUser(role));

        Assert.Equal(ErrorKind.Forbidden, result.Error.Kind);
    }

    [Fact]
    public async Task Another_inspector_cannot_attach_a_photo_to_an_ncr_they_did_not_raise()
    {
        var ncr = Stored(Ncrs.Open());

        var result = await AttachAsync(ncr.Id, _png, new FakeCurrentUser(Role.Inspector));

        Assert.Equal(ErrorKind.Forbidden, result.Error.Kind);
        Assert.Empty(_files.Files);
    }

    [Fact]
    public async Task A_second_photo_is_a_conflict_and_the_first_is_kept()
    {
        var ncr = Stored(Ncrs.Open());
        await AttachAsync(ncr.Id, _png, _raiser);

        var result = await AttachAsync(ncr.Id, [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46], _raiser);

        Assert.Equal(ErrorKind.Conflict, result.Error.Kind);
        Assert.Equal([$"{ncr.Id}.png"], _files.Files.Keys);
    }

    [Fact]
    public async Task Someone_who_can_see_the_ncr_can_open_its_photo_with_its_content_type()
    {
        var ncr = Stored(Ncrs.Open());
        await AttachAsync(ncr.Id, _png, _raiser);

        var photo = (await GetAsync(ncr.Id, new FakeCurrentUser(Role.QaManager))).Value;

        Assert.Equal("image/png", photo.ContentType);
        using var copy = new MemoryStream();
        await photo.Content.CopyToAsync(copy);
        Assert.Equal(_png, copy.ToArray());
    }

    [Fact]
    public async Task The_photo_of_an_ncr_the_caller_may_not_see_is_not_found()
    {
        var ncr = Stored(Ncrs.Open());
        await AttachAsync(ncr.Id, _png, _raiser);

        var result = await GetAsync(ncr.Id, new FakeCurrentUser(Role.Inspector));

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
    }

    [Fact]
    public async Task An_ncr_without_a_photo_has_no_photo_to_open()
    {
        var ncr = Stored(Ncrs.Open());

        var result = await GetAsync(ncr.Id, _raiser);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
    }

    private NonConformance Stored(NonConformance ncr)
    {
        _repository.Add(ncr);
        return ncr;
    }

    private Task<Result> AttachAsync(Guid ncrId, byte[] content, ICurrentUser user)
    {
        var runner = new NcrCommandRunner(_repository, _unitOfWork, new FixedClock(Ncrs.RaisedAt), user);
        return new AttachPhotoHandler(runner, _files, user)
            .HandleAsync(new AttachPhotoCommand(ncrId, new MemoryStream(content)), CancellationToken.None);
    }

    private Task<Result<PhotoView>> GetAsync(Guid ncrId, ICurrentUser user) =>
        new GetNcrPhotoHandler(_repository, _files, user).HandleAsync(new GetNcrPhotoQuery(ncrId), CancellationToken.None);
}
