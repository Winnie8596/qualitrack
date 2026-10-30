using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using QualiTrack.Api.Http;

namespace QualiTrack.Api.IntegrationTests.Ncrs;

/// <summary>A1 attach one optional photo, D2 show it.</summary>
[Collection(ApiCollection.Name)]
public sealed class PhotoEndpointsTests(QualiTrackApiFactory factory)
{
    private const int FiveMegabytes = 5 * 1024 * 1024;

    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];
    private static readonly byte[] _gif = "GIF89a\u0001\u0000\u0001\u0000"u8.ToArray();

    private readonly NcrApi _api = new(factory);

    [Fact]
    public async Task The_raiser_who_attaches_a_png_gets_204_and_anyone_who_may_see_the_ncr_can_open_it()
    {
        var (inspector, _) = await _api.SignedInAsync(NcrApi.Inspector);
        var id = await NcrApi.RaiseAsync(inspector);

        var response = await inspector.PostAsync($"/api/ncrs/{id}/photo", Photo(_png, "image/png"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var (manager, _) = await _api.SignedInAsync(NcrApi.QaManager);
        var detail = await manager.GetFromJsonAsync<PhotoFlag>($"/api/ncrs/{id}");
        Assert.True(detail!.HasPhoto);
        var photo = await manager.GetAsync($"/api/ncrs/{id}/photo");
        Assert.Equal(HttpStatusCode.OK, photo.StatusCode);
        Assert.Equal("image/png", photo.Content.Headers.ContentType?.MediaType);
        Assert.Equal(_png, await photo.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task A_file_that_is_not_a_jpeg_or_png_gets_400_even_when_it_claims_to_be_one()
    {
        var (inspector, _) = await _api.SignedInAsync(NcrApi.Inspector);
        var id = await NcrApi.RaiseAsync(inspector);

        var response = await inspector.PostAsync($"/api/ncrs/{id}/photo", Photo(_gif, "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False((await _api.GetAsync(id)).HasPhoto);
    }

    [Fact]
    public async Task A_photo_over_five_megabytes_gets_400()
    {
        var (inspector, _) = await _api.SignedInAsync(NcrApi.Inspector);
        var id = await NcrApi.RaiseAsync(inspector);
        var tooBig = new byte[FiveMegabytes + 1];
        _png.CopyTo(tooBig, 0);

        var response = await inspector.PostAsync($"/api/ncrs/{id}/photo", Photo(tooBig, "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_request_far_over_the_limit_is_refused_before_it_is_read()
    {
        var (inspector, _) = await _api.SignedInAsync(NcrApi.Inspector);
        var id = await NcrApi.RaiseAsync(inspector);

        var response = await inspector.PostAsync($"/api/ncrs/{id}/photo", Photo(new byte[4 * FiveMegabytes], "image/png"));

        Assert.False(response.IsSuccessStatusCode);
        Assert.False((await _api.GetAsync(id)).HasPhoto);
    }

    [Fact]
    public async Task Attaching_without_a_file_gets_400_naming_the_photo_field()
    {
        var (inspector, _) = await _api.SignedInAsync(NcrApi.Inspector);
        var id = await NcrApi.RaiseAsync(inspector);

        // What a form with no file chosen sends: a part, but no file in it
        var response = await inspector.PostAsync(
            $"/api/ncrs/{id}/photo", new MultipartFormDataContent { { new StringContent(string.Empty), "note" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Contains("photo", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Attaching_without_the_antiforgery_token_is_refused()
    {
        var (inspector, _) = await _api.SignedInAsync(NcrApi.Inspector);
        var id = await NcrApi.RaiseAsync(inspector);
        inspector.DefaultRequestHeaders.Remove(AntiforgeryProtection.HeaderName);

        var response = await inspector.PostAsync($"/api/ncrs/{id}/photo", Photo(_png, "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False((await _api.GetAsync(id)).HasPhoto);
    }

    [Fact]
    public async Task A_second_photo_gets_409()
    {
        var (inspector, _) = await _api.SignedInAsync(NcrApi.Inspector);
        var id = await NcrApi.RaiseAsync(inspector);
        await inspector.PostAsync($"/api/ncrs/{id}/photo", Photo(_png, "image/png"));

        var response = await inspector.PostAsync($"/api/ncrs/{id}/photo", Photo(_png, "image/png"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData(NcrApi.QaManager)]
    [InlineData(null)]
    public async Task Only_the_inspector_who_raised_the_ncr_can_attach_its_photo(string? email)
    {
        var id = await _api.RaiseAsync();
        var (client, _) = await _api.SignedInAsync(email ?? await _api.CreateUserAsync("Inspector"));

        var response = await client.PostAsync($"/api/ncrs/{id}/photo", Photo(_png, "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Attaching_or_opening_a_photo_without_signing_in_gets_401()
    {
        var id = await _api.RaiseAsync();
        var client = _api.Anonymous();

        var attach = await client.PostAsync($"/api/ncrs/{id}/photo", Photo(_png, "image/png"));
        var open = await client.GetAsync($"/api/ncrs/{id}/photo");

        Assert.Equal(HttpStatusCode.Unauthorized, attach.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, open.StatusCode);
    }

    [Fact]
    public async Task Attaching_to_an_ncr_that_does_not_exist_gets_404()
    {
        var (inspector, _) = await _api.SignedInAsync(NcrApi.Inspector);

        var response = await inspector.PostAsync($"/api/ncrs/{Guid.NewGuid()}/photo", Photo(_png, "image/png"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Opening_the_photo_of_an_ncr_without_one_or_that_the_caller_may_not_see_gets_404()
    {
        var (inspector, _) = await _api.SignedInAsync(NcrApi.Inspector);
        var withPhoto = await NcrApi.RaiseAsync(inspector);
        await inspector.PostAsync($"/api/ncrs/{withPhoto}/photo", Photo(_png, "image/png"));
        var withoutPhoto = await NcrApi.RaiseAsync(inspector);
        var (otherInspector, _) = await _api.SignedInAsync(await _api.CreateUserAsync("Inspector"));

        var none = await inspector.GetAsync($"/api/ncrs/{withoutPhoto}/photo");
        var hidden = await otherInspector.GetAsync($"/api/ncrs/{withPhoto}/photo");

        Assert.Equal(HttpStatusCode.NotFound, none.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    private static MultipartFormDataContent Photo(byte[] bytes, string contentType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "photo", "photo.png" } };
    }

    private sealed record PhotoFlag(bool HasPhoto);
}
