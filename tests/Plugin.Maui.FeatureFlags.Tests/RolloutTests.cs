namespace Plugin.Maui.FeatureFlags.Tests;

public sealed class RolloutTests
{
    [Fact]
    public void Zero_percent_excludes_everyone()
    {
        var result = Harness.Evaluate(Harness.Flag("new_checkout", flag => flag.Percentage = 0));

        Assert.False(result.Enabled);
        Assert.Equal(FeatureFlagReason.NotInRollout, result.Reason);
        Assert.InRange(result.RolloutBucket ?? -1, 0, 99);
    }

    [Fact]
    public void One_hundred_percent_includes_everyone()
    {
        var result = Harness.Evaluate(Harness.Flag("new_checkout", flag => flag.Percentage = 100));

        Assert.True(result.Enabled);
        Assert.Equal(FeatureFlagReason.Matched, result.Reason);
    }

    [Fact]
    public void Bucket_is_sticky_for_the_same_user()
    {
        var first = RolloutHasher.Bucket("new_voip_engine", "user-42");
        var second = RolloutHasher.Bucket("new_voip_engine", "user-42");

        Assert.Equal(first, second);
        Assert.InRange(first, 0, 99);
    }

    [Fact]
    public void Different_users_can_land_in_different_buckets()
    {
        var seen = new HashSet<int>();
        for (var i = 0; i < 40; i++)
        {
            seen.Add(RolloutHasher.Bucket("new_voip_engine", "user-" + i));
        }

        Assert.True(seen.Count > 1);
    }

    [Fact]
    public void User_id_is_preferred_over_device_id_for_rollout()
    {
        var withUser = Harness.Evaluate(
            Harness.Flag("rollout", flag => flag.Percentage = 100),
            new FakeContext().Context with { UserId = "user-42", DeviceId = "device-1" });
        var otherUser = Harness.Evaluate(
            Harness.Flag("rollout", flag => flag.Percentage = 100),
            new FakeContext().Context with { UserId = "user-99", DeviceId = "device-1" });

        Assert.Equal(
            RolloutHasher.Bucket("rollout", "user-42"),
            withUser.RolloutBucket);
        Assert.Equal(
            RolloutHasher.Bucket("rollout", "user-99"),
            otherUser.RolloutBucket);
    }
}
