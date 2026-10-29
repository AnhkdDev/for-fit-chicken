using System.Net;
using System.Net.Http.Json;
using ForFitChicken.Application.Commerce;
using ForFitChicken.Infrastructure.Identity;
using ForFitChicken.Infrastructure.Media;
using ForFitChicken.UnitTest.Integration.Persistence;
using ForFitChicken.UnitTest.Unit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ForFitChicken.UnitTest.Integration.Http;
[Trait("Category", "SqlServerIntegration")]
public sealed class MediaUploadTests
{
    [SqlServerFact]
    public async Task OwnerAndOperatorUpload_RequiresCsrf_TransformsImage_AndKeepsDraftPrivate()
    {
        var db = new MigratedDatabaseFixture();
        var root = Path.Combine(Path.GetTempPath(), "ForFitChicken_MediaHttp_" + Guid.NewGuid().ToString("N"));
        try {
            await db.InitializeAsync();
            await using var factory = new AdminAuthApiFactory(db, configure: services => {
                services.AddSingleton(new LocalMediaStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Media:RootPath"] = root }).Build()));
            });
            using var guest = factory.Client();
            Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsync("/api/media", Form())).StatusCode);
            foreach (var role in new[] { "Owner", "Operator" }) {
                var name = Guid.NewGuid().ToString("N"); var password = "TestOnly-" + Guid.NewGuid().ToString("N") + "-aA7!";
                using (var scope = factory.Services.CreateScope()) {
                    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var user = new ApplicationUser { UserName = name, DisplayName = "Disposable HTTP media test" };
                    Assert.True((await users.CreateAsync(user, password)).Succeeded); Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
                }
                using var client = factory.Client();
                var csrf = await client.GetFromJsonAsync<ForFitChicken.API.Controllers.CsrfTokenResponse>("/api/admin/auth/csrf");
                client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.RequestToken);
                Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/admin/auth/login", new { userName = name, password })).StatusCode);
                client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
                Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/media", Form())).StatusCode);
                csrf = await client.GetFromJsonAsync<ForFitChicken.API.Controllers.CsrfTokenResponse>("/api/admin/auth/csrf");
                client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.RequestToken);
                var response = await client.PostAsync("/api/media", Form());
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var asset = (await response.Content.ReadFromJsonAsync<AssetView>())!; Assert.Equal("image/webp", asset.ContentType);
                Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync(asset.Url)).StatusCode);
                var privateImage = await client.GetAsync(asset.Url); Assert.Equal(HttpStatusCode.OK, privateImage.StatusCode);
                Assert.True(privateImage.Headers.CacheControl!.NoStore);
                if (role == "Operator") Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/media/storage")).StatusCode);
            }
        } finally {
            await db.DisposeAsync();
            if (Directory.Exists(root)) { foreach (var file in Directory.GetFiles(root)) File.Delete(file); Directory.Delete(root); }
        }
    }
    private static MultipartFormDataContent Form()
    {
        var form = new MultipartFormDataContent(); var image = new ByteArrayContent(ImageOptimizationTests.Png(4, 4));
        image.Headers.ContentType = new("image/png"); form.Add(image, "file", "test.png"); return form;
    }
}
