using AssetLiquidation.Api.DTOs;
using AssetLiquidation.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;

namespace AssetLiquidation.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AssetController : ControllerBase
{
    private readonly LiquidateDbContext _context;

    public AssetController(LiquidateDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Cadastra um novo Ativo
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssetRequest request, ApiVersion apiVersion)
    {
        try
        {
            var exists = await _context.Assets.AnyAsync(a => a.AssetId == request.AssetId);
            if (exists)
                return Conflict (new { message = $"Alredy exist an AssetId '{request.AssetId}'." });
            var asset = new Asset(request.AssetId, request.InitialAmount);
            //estudar
            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            var response = new AssetResponse(
                asset.Id,
                asset.AssetId,
                asset.InitialAmount,
                asset.CurrentAmount,
                asset.Status.ToString(),
                asset.CreatedAt
            );

            //estudar
            return CreatedAtAction(
                nameof(GetById),
                new {version = apiVersion.ToString(), id = asset.Id},
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message});
        }
    }

    /// <summary>
    /// Consulta um Ativo pelo GUID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var asset = await _context.Assets.FindAsync(id);

        if (asset == null)
            return NotFound(new { message = "Asset not found." });

        var response = new AssetResponse(
            asset.Id,
            asset.AssetId,
            asset.InitialAmount,
            asset.CurrentAmount,
            asset.Status.ToString(),
            asset.CreatedAt
        );

        return Ok(response);
    }

    /// <summary>
    /// Consulta um Ativo pelo AssetId
    /// </summary>
    [HttpGet("contract/{assetId}")]
    public async Task<IActionResult> GetByAssetId(string assetId)
    {
        var asset = await _context.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssetId == assetId);

        if (asset == null)
            return NotFound(new { message = $"Asset {assetId} not found." });

        var response = new AssetResponse(
            asset.Id,
            asset.AssetId,
            asset.InitialAmount,
            asset.CurrentAmount,
            asset.Status.ToString(),
            asset.CreatedAt
        );

        return Ok(response);
    }

    /// <summary>
    /// Lista todos os Ativos cadastrados
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        //Estudar
        var assets = await _context.Assets
            .AsNoTracking()
            .Select(asset => new AssetResponse(
                asset.Id,
                asset.AssetId,
                asset.InitialAmount,
                asset.CurrentAmount,
                asset.Status.ToString(),
                asset.CreatedAt
            ))
            .ToListAsync();
    
        return Ok(assets);
    }
}