using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Domain.Tests;

/// <summary>A1: one optional photo, JPEG or PNG, at most 5 MB, attached by the inspector who raised the NCR.</summary>
public sealed class PhotoTests
{
    private static readonly byte[] _jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void A_jpeg_is_recognised_by_its_first_bytes()
    {
        var format = PhotoFormat.Recognise(_jpeg, length: 120_000);

        Assert.Equal(PhotoFormat.Jpeg, format);
    }

    [Fact]
    public void A_png_is_recognised_by_its_first_bytes()
    {
        var format = PhotoFormat.Recognise(_png, length: 120_000);

        Assert.Equal(PhotoFormat.Png, format);
    }

    [Theory]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x00, 0x00 })]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37 })]
    [InlineData(new byte[] { 0x3C, 0x73, 0x76, 0x67, 0x20, 0x78, 0x6D, 0x6C })]
    [InlineData(new byte[] { 0xFF, 0xD8 })]
    [InlineData(new byte[0])]
    public void Anything_but_a_jpeg_or_png_is_refused_whatever_it_is_called(byte[] header)
    {
        var exception = Assert.Throws<DomainException>(() => PhotoFormat.Recognise(header, length: 1_000));

        Assert.Equal(DomainErrorKind.InvalidInput, exception.Kind);
    }

    [Fact]
    public void A_photo_of_exactly_five_megabytes_is_allowed()
    {
        var format = PhotoFormat.Recognise(_png, length: PhotoFormat.MaxLength);

        Assert.Equal(PhotoFormat.Png, format);
        Assert.Equal(5 * 1024 * 1024, PhotoFormat.MaxLength);
    }

    [Fact]
    public void A_photo_over_five_megabytes_is_refused()
    {
        var exception = Assert.Throws<DomainException>(() => PhotoFormat.Recognise(_png, length: PhotoFormat.MaxLength + 1));

        Assert.Equal(DomainErrorKind.InvalidInput, exception.Kind);
    }

    [Fact]
    public void The_inspector_who_raised_an_open_ncr_can_attach_its_photo()
    {
        var ncr = Ncrs.Open();

        ncr.AttachPhoto(PhotoFormat.Png, Ncrs.Day(0, Ncrs.Inspector));

        Assert.Equal(PhotoFormat.Png, ncr.Photo);
    }

    [Fact]
    public void Nobody_else_can_attach_the_photo()
    {
        var ncr = Ncrs.Open();

        var exception = Assert.Throws<DomainException>(() => ncr.AttachPhoto(PhotoFormat.Png, Ncrs.Day(0, Ncrs.Manager)));

        Assert.Equal(DomainErrorKind.NotPermitted, exception.Kind);
        Assert.Null(ncr.Photo);
    }

    [Fact]
    public void An_ncr_has_at_most_one_photo()
    {
        var ncr = Ncrs.Open();
        ncr.AttachPhoto(PhotoFormat.Png, Ncrs.Day(0, Ncrs.Inspector));

        var exception = Assert.Throws<DomainException>(() => ncr.AttachPhoto(PhotoFormat.Jpeg, Ncrs.Day(0, Ncrs.Inspector)));

        Assert.Equal(DomainErrorKind.InvalidState, exception.Kind);
        Assert.Equal(PhotoFormat.Png, ncr.Photo);
    }

    [Fact]
    public void The_photo_can_only_be_attached_while_the_ncr_is_open()
    {
        var ncr = Ncrs.Investigating();

        var exception = Assert.Throws<DomainException>(() => ncr.AttachPhoto(PhotoFormat.Png, Ncrs.Day(2, Ncrs.Inspector)));

        Assert.Equal(DomainErrorKind.InvalidState, exception.Kind);
    }

    [Fact]
    public void Attaching_the_photo_is_not_a_status_change_so_the_history_is_unchanged()
    {
        var ncr = Ncrs.Open();

        ncr.AttachPhoto(PhotoFormat.Jpeg, Ncrs.Day(0, Ncrs.Inspector));

        Assert.Empty(ncr.History);
        Assert.Equal(NcrStatus.Open, ncr.Status);
    }
}
