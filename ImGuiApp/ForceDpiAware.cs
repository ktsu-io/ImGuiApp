// Adapted From: https://github.com/Ryujinx/Ryujinx/blob/master/src/Ryujinx.Common/SystemInterop/ForceDpiAware.cs
// License: MIT

namespace ktsu.ImGuiApp;

using System.Globalization;
using System.Runtime.InteropServices;
public static partial class ForceDpiAware
{
	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool SetProcessDPIAware();

	private const string X11LibraryName = "libX11.so.6";

	[LibraryImport(X11LibraryName)]
	private static partial IntPtr XOpenDisplay([MarshalAs(UnmanagedType.LPStr)] string display);

	[LibraryImport(X11LibraryName)]
	private static partial IntPtr XGetDefault(IntPtr display, [MarshalAs(UnmanagedType.LPStr)] string program, [MarshalAs(UnmanagedType.LPStr)] string option);

	[LibraryImport(X11LibraryName)]
	private static partial int XDisplayWidth(IntPtr display, int screenNumber);

	[LibraryImport(X11LibraryName)]
	private static partial int XDisplayWidthMM(IntPtr display, int screenNumber);

	[LibraryImport(X11LibraryName)]
	private static partial int XCloseDisplay(IntPtr display);

	private const double StandardDpiScale = 96.0;
	private const double MaxScaleFactor = 10.25;

	/// <summary>
	/// Marks the application as DPI-Aware when running on the Windows operating system.
	/// </summary>
	public static void Windows()
	{
		// Make process DPI aware for proper window sizing on high-res screens.
		if (OperatingSystem.IsWindowsVersionAtLeast(6))
		{
			SetProcessDPIAware();
		}
	}

	public static double GetActualScaleFactor()
	{
		double userDpiScale = 96.0;

		try
		{
			if (OperatingSystem.IsWindows())
			{
				userDpiScale = GdiPlusHelper.GetDpiX(IntPtr.Zero);
			}
			else if (OperatingSystem.IsLinux())
			{
				string? xdgSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")?.ToLower();

				if (xdgSessionType is null or "x11")
				{
					IntPtr display = XOpenDisplay(null!);
					string? dpiString = Marshal.PtrToStringAnsi(XGetDefault(display, "Xft", "dpi"));
					if (dpiString == null || !double.TryParse(dpiString, NumberStyles.Any, CultureInfo.InvariantCulture, out userDpiScale))
					{
						userDpiScale = XDisplayWidth(display, 0) * 25.4 / XDisplayWidthMM(display, 0);
					}
					_ = XCloseDisplay(display);
				}
				else if (xdgSessionType == "wayland")
				{
					// TODO
					//Logger.Warning?.Print(LogClass.Application, "Couldn't determine monitor DPI: Wayland not yet supported");
				}
				else
				{
					//Logger.Warning?.Print(LogClass.Application, $"Couldn't determine monitor DPI: Unrecognised XDG_SESSION_TYPE: {xdgSessionType}");
				}
			}
		}
		catch (Exception)
		{
			//Logger.Warning?.Print(LogClass.Application, $"Couldn't determine monitor DPI: {e.Message}");
		}

		return userDpiScale;
	}

	public static double GetWindowScaleFactor()
	{
		double userDpiScale = GetActualScaleFactor();

		return Math.Min(userDpiScale / StandardDpiScale, MaxScaleFactor);
	}
}
