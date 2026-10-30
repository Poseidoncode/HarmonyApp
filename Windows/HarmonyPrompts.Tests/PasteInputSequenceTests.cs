using HarmonyPrompts.Core.Services;
using HarmonyPrompts.Windows.Services;

namespace HarmonyPrompts.Tests;

public sealed class PasteInputSequenceTests
{
    [Theory]
    [InlineData(0, 0, PasteInputResult.Blocked)]
    [InlineData(1, 1, PasteInputResult.Blocked)]
    [InlineData(1, 0, PasteInputResult.KeyReleaseUncertain)]
    [InlineData(2, 2, PasteInputResult.Uncertain)]
    [InlineData(2, 1, PasteInputResult.KeyReleaseUncertain)]
    [InlineData(2, 0, PasteInputResult.KeyReleaseUncertain)]
    [InlineData(3, 1, PasteInputResult.Uncertain)]
    [InlineData(3, 0, PasteInputResult.KeyReleaseUncertain)]
    [InlineData(4, 0, PasteInputResult.Sent)]
    public void PartialInsertionChecksCleanupWithoutRetryingPaste(int accepted, int released, PasteInputResult expected)
    {
        var calls = new List<PasteKey[]>();
        var result = PasteInputSequence.Send(keys =>
        {
            calls.Add(keys);
            return checked((uint)(calls.Count == 1 ? accepted : released));
        });
        Assert.Equal(expected, result);
        Assert.Equal(new[] { new PasteKey(0x11), new PasteKey(0x56), new PasteKey(0x56, true), new PasteKey(0x11, true) }, calls[0]);
        if (accepted is 0 or 4) Assert.Single(calls);
        else
        {
            Assert.Equal(2, calls.Count);
            Assert.All(calls[1], key => Assert.True(key.IsKeyUp));
            Assert.Equal(accepted == 2 ? new ushort[] { 0x56, 0x11 } : new ushort[] { 0x11 }, calls[1].Select(key => key.VirtualKey));
        }
    }
}
