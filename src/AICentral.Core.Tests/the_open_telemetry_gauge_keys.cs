using System.Diagnostics;
using Shouldly;
using Xunit;

namespace AICentral.Core.Tests;

public class the_open_telemetry_gauge_keys
{
    [Fact]
    public void produces_the_correct_key()
    {
        var key = ActivitySources.BuildGaugeKey("test", new TagList()
        {
            { "test", "one" },
            { "test2", "two" }
        });
        
        key.ShouldBe("aicentral.test.one.two");
    }
}