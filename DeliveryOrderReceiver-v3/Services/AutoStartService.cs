using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace DeliveryOrderReceiver.Services;

/// <summary>
/// Windows 시작 시 자동 실행 등록/해제.
/// HKCU\Software\Microsoft\Windows\CurrentVersion\Run (사용자 권한, UAC 불필요).
/// </summary>
public class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DeliveryOrderReceiver-v3";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    public void Enable()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
            key.SetValue(ValueName, $"\"{exePath}\"");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"자동 시작 등록 실패: {ex.Message}", ex);
        }
    }

    public void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"자동 시작 해제 실패: {ex.Message}", ex);
        }
    }
}
