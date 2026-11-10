using PropertyAdmin.Logic.V1.Reports.Models;

namespace PropertyAdmin.Logic.V1.Reports.GetAssetResults;

public sealed record Result(IReadOnlyList<AssetResultModel> AssetResults);
