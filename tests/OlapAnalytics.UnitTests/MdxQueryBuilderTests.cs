using Moq;
using OlapAnalytics.Application.Services;
using OlapAnalytics.Domain.Entities;
using OlapAnalytics.Domain.Interfaces;
using OlapAnalytics.Domain.ValueObjects;

namespace OlapAnalytics.UnitTests;

/// <summary>
/// Unit tests for MdxQueryBuilder — verifies correct MDX syntax generation
/// with mocked metadata from IMdxExecutor.
/// </summary>
public class MdxQueryBuilderTests
{
    private readonly Mock<IMdxExecutor> _mockExecutor;
    private readonly MdxQueryBuilder _builder;

    public MdxQueryBuilderTests()
    {
        _mockExecutor = new Mock<IMdxExecutor>();
        
        _mockExecutor.Setup(x => x.GetMeasuresAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Measure>
            {
                new Measure { Name = "Trip Cost", UniqueName = "[Measures].[Trip Cost]" },
                new Measure { Name = "Duration Days", UniqueName = "[Measures].[Duration Days]" },
                new Measure { Name = "Traveler Count", UniqueName = "[Measures].[Traveler Count]" }
            });

        _mockExecutor.Setup(x => x.GetDimensionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dimension>
            {
                new Dimension 
                { 
                    Name = "destination", 
                    UniqueName = "[Dim Destination]",
                    Hierarchies = new List<DimensionHierarchy>
                    {
                        new DimensionHierarchy
                        {
                            Name = "Country",
                            Levels = new List<DimensionLevel>
                            {
                                new DimensionLevel { Name = "Country", UniqueName = "[Dim Destination].[Country]" }
                            }
                        }
                    }
                },
                new Dimension { Name = "time", UniqueName = "[Dim Date]" },
                new Dimension { Name = "transportation", UniqueName = "[Dim Transportation]" }
            });

        _builder = new MdxQueryBuilder(_mockExecutor.Object);
    }

    [Fact]
    public async Task BuildMdxQuery_SingleMeasure_ContainsCorrectSyntax()
    {
        var mdx = await _builder.BuildMdxQueryAsync(
            "[SSAS_Travel]",
            new[] { "Trip Cost" },
            "destination");

        Assert.Contains("SELECT", mdx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ON COLUMNS", mdx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ON ROWS", mdx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[SSAS_Travel]", mdx);
        Assert.Contains("[Measures].[Trip Cost]", mdx);
        Assert.Contains("[Dim Destination]", mdx);
    }

    [Fact]
    public async Task BuildMdxQuery_MultipleMeasures_AllMeasuresIncluded()
    {
        var mdx = await _builder.BuildMdxQueryAsync(
            "[SSAS_Travel]",
            new[] { "Trip Cost", "Duration Days", "Traveler Count" },
            "destination");

        Assert.Contains("[Measures].[Trip Cost]", mdx);
        Assert.Contains("[Measures].[Duration Days]", mdx);
        Assert.Contains("[Measures].[Traveler Count]", mdx);
    }

    [Fact]
    public async Task ApplyFilters_WithDateRange_AddsWhereClause()
    {
        var baseMdx = await _builder.BuildMdxQueryAsync("[SSAS_Travel]", new[] { "Trip Cost" }, "destination");
        var filtered = await _builder.ApplyFiltersAsync(baseMdx, Array.Empty<DimensionFilter>(), new DateRange(2023) { YearColumn = "[Dim Date].[Year]" });

        Assert.Contains("WHERE", filtered, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[Dim Date].[Year].&[2023]", filtered);
    }

    [Fact]
    public async Task ApplyFilters_WithDimensionFilter_AddsMemberFilter()
    {
        var baseMdx = await _builder.BuildMdxQueryAsync("[SSAS_Travel]", new[] { "Trip Cost" }, "destination");
        var filter = new DimensionFilter
        {
            DimensionName = "destination",
            LevelName = "Country",
            MemberValues = new List<string> { "Vietnam" }
        };
        var filtered = await _builder.ApplyFiltersAsync(baseMdx, new[] { filter });

        Assert.Contains("WHERE", filtered, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Vietnam", filtered);
    }

    [Fact]
    public async Task ApplyFilters_WithMultipleMembers_UsesSetSyntax()
    {
        var baseMdx = await _builder.BuildMdxQueryAsync("[SSAS_Travel]", new[] { "Trip Cost" }, "destination");
        var filter = new DimensionFilter
        {
            DimensionName = "destination",
            MemberValues = new List<string> { "Vietnam", "Thailand", "Japan" }
        };
        var filtered = await _builder.ApplyFiltersAsync(baseMdx, new[] { filter });

        Assert.Contains("{", filtered);
        Assert.Contains("Vietnam", filtered);
        Assert.Contains("Thailand", filtered);
        Assert.Contains("Japan", filtered);
    }

    [Fact]
    public async Task BuildTopNQuery_GeneratesTopCountMdx()
    {
        var mdx = await _builder.BuildTopNQueryAsync("[SSAS_Travel]", "Trip Cost", "destination", 10);

        Assert.Contains("TOPCOUNT", mdx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("10", mdx);
        Assert.Contains("[Measures].[Trip Cost]", mdx);
    }

    [Fact]
    public async Task BuildYoYQuery_ContainsParallelPeriodOrCalculatedMember()
    {
        var mdx = await _builder.BuildYoYQueryAsync("[SSAS_Travel]", "Trip Cost", 2023, "[Dim Date].[Year]");

        Assert.Contains("MEMBER", mdx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2023", mdx);
        Assert.Contains("2022", mdx);
        Assert.Contains("YoY Growth", mdx, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildMoMQuery_ContainsParallelPeriod()
    {
        var mdx = await _builder.BuildMoMQueryAsync("[SSAS_Travel]", "Trip Cost", 2023, "[Dim Date].[Year]");

        Assert.Contains("ParallelPeriod", mdx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2023", mdx);
        Assert.Contains("MoM Growth", mdx, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplySlice_AddsWhereClause()
    {
        var baseMdx = await _builder.BuildMdxQueryAsync("[SSAS_Travel]", new[] { "Trip Cost" }, "destination");
        var sliced = await _builder.ApplySliceAsync(baseMdx, "time", "2023");

        Assert.Contains("WHERE", sliced, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2023", sliced);
    }

    [Fact]
    public async Task ApplyDice_MultipleDimensions_AllFilterApplied()
    {
        var baseMdx = await _builder.BuildMdxQueryAsync("[SSAS_Travel]", new[] { "Trip Cost" }, "destination");
        var diced = await _builder.ApplyDiceAsync(baseMdx, new Dictionary<string, List<string>>
        {
            { "time", new List<string> { "2022", "2023" } },
            { "transportation", new List<string> { "Flight" } }
        });

        Assert.Contains("WHERE", diced, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildTrendQuery_YearlyDefault_ContainsYearMembers()
    {
        var mdx = await _builder.BuildTrendQueryAsync("[SSAS_Travel]", "Trip Cost", "[Dim Date]", "yearly", null);

        Assert.Contains("[Dim Date].[Year]", mdx);
        Assert.Contains("[Measures].[Trip Cost]", mdx);
    }
}
