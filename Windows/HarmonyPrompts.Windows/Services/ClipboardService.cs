using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HarmonyPrompts.Windows.Native;

namespace HarmonyPrompts.Windows.Services;

public sealed class ClipboardService
{
    private readonly Dispatcher _dispatcher;
    private readonly Action<System.Windows.DataObject> _publish;

    public ClipboardService(Dispatcher dispatcher)
        : this(dispatcher, data => System.Windows.Clipboard.SetDataObject(data, copy: true)) { }

    // Harness-only seam to reproduce successful OLE publication followed by a failed flush.
    internal ClipboardService(Dispatcher dispatcher, Action<System.Windows.DataObject> publish)
    {
        _dispatcher = dispatcher;
        _publish = publish;
    }

    private const string MarkerFormat = "HarmonyPrompts.CopyOperation.v1";
    private uint _sequence;
    public bool IsCurrent => _sequence != 0 && Win32.GetClipboardSequenceNumber() == _sequence;

    public async Task<bool> CopyAsync(string text, CancellationToken cancellationToken)
    {
        _sequence = 0;
        // CF_UNICODETEXT is NUL-terminated: never silently send a truncated prompt.
        if (text.Contains('\0')) return false;
        var marker = Guid.NewGuid().ToByteArray();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var verified = await _dispatcher.InvokeAsync(() =>
                {
                    var data = new System.Windows.DataObject();
                    data.SetText(text, TextDataFormat.UnicodeText);
                    // OLE may retain this object if publication succeeds but flushing throws.
                    // Keep these managed-only streams alive with its data; no using/Dispose here.
                    var operation = new MemoryStream(marker, writable: false);
                    var noHistory = new MemoryStream(new byte[4], writable: false);
                    var noCloud = new MemoryStream(new byte[4], writable: false);
                    data.SetData(MarkerFormat, operation);
                    // Raw DWORD 0 (streams, not serialized managed integers).
                    data.SetData("CanIncludeInClipboardHistory", noHistory);
                    data.SetData("CanUploadToCloudClipboard", noCloud);
                    _publish(data);
                    return VerifyAndCaptureSequence(text, marker);
                }, DispatcherPriority.Normal, cancellationToken);
                if (verified) return true;
            }
            catch (COMException) { /* Clipboard busy: bounded retry below. */ }
            if (attempt < 4) await Task.Delay(TimeSpan.FromMilliseconds(40 * (attempt + 1)), cancellationToken);
        }
        return false;
    }

    private bool VerifyAndCaptureSequence(string expected, byte[] marker)
    {
        if (!Win32.OpenClipboard(0)) return false;
        try
        {
            // Reading payload + private operation marker and recording the sequence
            // under one open transaction cannot certify another writer's sequence.
            if (!Matches(13, checked((expected.Length + 1) * 2), pointer =>
                Marshal.PtrToStringUni(pointer, expected.Length) == expected && Marshal.ReadInt16(pointer, expected.Length * 2) == 0)) return false;
            var markerFormat = Win32.RegisterClipboardFormat(MarkerFormat);
            if (markerFormat == 0 || !Matches(markerFormat, marker.Length, pointer =>
            {
                var actual = new byte[marker.Length];
                Marshal.Copy(pointer, actual, 0, actual.Length);
                return actual.AsSpan().SequenceEqual(marker);
            })) return false;
            foreach (var name in new[] { "CanIncludeInClipboardHistory", "CanUploadToCloudClipboard" })
            {
                var format = Win32.RegisterClipboardFormat(name);
                if (format == 0 || !Matches(format, 4, pointer => Marshal.ReadInt32(pointer) == 0)) return false;
            }
            _sequence = Win32.GetClipboardSequenceNumber();
            return _sequence != 0;
        }
        finally { Win32.CloseClipboard(); }
    }

    private static bool Matches(uint format, int minimumBytes, Func<nint, bool> match)
    {
        var memory = Win32.GetClipboardData(format);
        if (memory == 0 || Win32.GlobalSize(memory) < (nuint)minimumBytes) return false;
        var pointer = Win32.GlobalLock(memory);
        if (pointer == 0) return false;
        try { return match(pointer); }
        finally { Win32.GlobalUnlock(memory); }
    }
}
