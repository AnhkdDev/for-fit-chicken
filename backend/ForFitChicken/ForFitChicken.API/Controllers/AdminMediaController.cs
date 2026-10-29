using ForFitChicken.API.Authentication;
using ForFitChicken.API.RateLimiting;
using ForFitChicken.Application.Commerce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ForFitChicken.API.Controllers;
[ApiController, Route("api/admin/media"), Authorize(Policy = AdminPolicies.Owner), EnableRateLimiting(RateLimitPolicies.Admin)]
public sealed class AdminMediaController(MediaPolicyService policy, IMediaMaintenance maintenance, CatalogService catalog) : StaffController
{
    [HttpGet("storage")]
    public async Task<MediaStorageStatus> Status(CancellationToken token) => (await policy.StatusAsync(token)) with { NodeReady = maintenance.IsReady };
    [HttpPut("uploads")]
    public Task<bool> Pause(MediaPauseInput input, CancellationToken token) => policy.PauseAsync(input.Paused, Actor, token);
    [HttpPost("reconcile")]
    public Task<MediaStorageStatus> Reconcile(CancellationToken token) => maintenance.ReconcileAsync(token);
    [HttpGet("orphans")]
    public Task<MediaObjectStatus[]> Orphans(CancellationToken token) => policy.OrphansAsync(token);
    [HttpGet("assets")]
    public Task<Page<MediaAssetStatus>> Assets(bool unusedOnly = false, int page = 1, int size = 20, CancellationToken token = default) => catalog.MediaAssetsAsync(unusedOnly, page, size, token);
    [HttpDelete("assets/{id:long}")]
    public Task<MediaDeleteResult> DeleteUnused(long id, CancellationToken token) => catalog.DeleteUnusedAssetAsync(id, Actor, token);
    [HttpPost("orphans/{id:long}/delete")]
    public async Task<IActionResult> DeleteOrphan(long id, CancellationToken token)
    { await maintenance.DeleteOrphanAsync(id, token); return NoContent(); }
}
public sealed record MediaPauseInput(bool Paused);
