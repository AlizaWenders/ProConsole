using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using static System.Console;

namespace ProConsole {

public static class ProConsoleFunctions {

	[DllImport("kernel32.dll", SetLastError = true )]
	private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);
	[DllImport("kernel32.dll", SetLastError = true )]
	private static extern bool GetConsoleMode(IntPtr handle, out int mode);
	[DllImport("kernel32.dll", SetLastError = true )]
	private static extern IntPtr GetStdHandle(int handle);
	private static IntPtr handleIn = GetStdHandle(-10);
	private static IntPtr handleSB = GetStdHandle(-11);
	private	static int mode;

	[DllImport("user32.dll")]
	public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);
	[DllImport("user32.dll")]
	private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
	[DllImport("kernel32.dll", ExactSpelling = true)]
	private static extern IntPtr GetConsoleWindow();
	private const int MF_BYCOMMAND = 0x00000000;
	private const int SC_CLOSE = 0xF060;
	private const int SC_MINIMIZE = 0xF020;
	private const int SC_MAXIMIZE = 0xF030;
	private const int SC_SIZE = 0xF000;

	private static Random random;
	private static Timer timer;
	private static ProColor[,,] background;
	private static List<MetalText> metalTexts = new List<MetalText>();
	private static List<ProMenu> menus = new List<ProMenu>();

	public static ProColor currentFColor;
	public static ProColor currentBColor;
	public static ProColor cBlack = new ProColor {r = 0, g = 0, b = 0};
	public static ProColor cWhite = new ProColor {r = 255, g = 255, b = 255};
	public static ProColor cGray1 = new ProColor {r = 128, g = 128, b = 128};

	private static readonly string[,] borderElements = new string[2, 8] {
		{"─", "│", "┌", "┐", "└", "┘", "[ ", " ]"},
		{"═", "║", "╔", "╗", "╚", "╝", "[ ", " ]"}
	};

	public static void InitProConsole(bool disableEditing = true, bool disableResizing = true, bool disableCloseAndMinimize = true)
	{
		GetConsoleMode(handleSB, out mode);
		SetConsoleMode(handleSB, mode | 0x4);

		if (disableEditing) {
		GetConsoleMode(handleIn, out mode);
		SetConsoleMode(handleIn, mode & ~0x0040);
		}

		if (disableResizing || disableCloseAndMinimize) {
		IntPtr handle = GetConsoleWindow();
		IntPtr sysMenu = GetSystemMenu(handle, false);
		if (handle != IntPtr.Zero) {
		if (disableResizing) {
		DeleteMenu(sysMenu, SC_MAXIMIZE, MF_BYCOMMAND);
		DeleteMenu(sysMenu, SC_SIZE, MF_BYCOMMAND);
		}
		if (disableCloseAndMinimize) {
		DeleteMenu(sysMenu, SC_CLOSE, MF_BYCOMMAND);
		DeleteMenu(sysMenu, SC_MINIMIZE, MF_BYCOMMAND);
		}
		}
		}

		random = new Random((int) DateTime.Now.Ticks);
		timer = new Timer(25);
		timer.Elapsed += DynamicUpdate;
		timer.AutoReset = true;
		timer.Enabled = true;

		SetFColor(cWhite, true);
		SetBColor(cBlack, true);
		ClearScreen(cBlack);
	}

	public static void SetFColor(ProColor color, bool perma = true)
	{
		Write("\x1b[38;2;" + color.r + ";" + color.g + ";" + color.b + "m");
		if (perma) currentFColor = color;
	}

	public static void SetBColor(ProColor color, bool perma = true)
	{
		Write("\x1b[48;2;" + color.r + ";" + color.g + ";" + color.b + "m");
		if (perma) currentBColor = color;
	}

	public static void ResetColors()
	{
		SetFColor(currentFColor, false);
		SetBColor(currentBColor, false);
	}

	public static void ClearScreen(ProColor color)
	{
		background = new ProColor[WindowWidth, WindowHeight, 2];
		SetBColor(color);
		for (int y = 0; y < WindowHeight; y++) {
			Write(" ".Repeat(WindowWidth));
			for (int x = 0; x < WindowWidth; x++) {
				background[x, y, 0] = color;
				background[x, y, 1] = color;
			}
		}
		metalTexts.Clear();
	}

	public static void Print(string caption, int col, int row)
	{
		int cl = CursorLeft;
		int ct = CursorTop;

		SetCursorPosition(col == -1 ? (WindowWidth - caption.Length) / 2 : col, row == -1 ? WindowHeight / 2 : row);
		Write(caption);

		SetCursorPosition(cl, ct);
	}

	public static void Print(string caption, int col, int row, ProColor cForeStart, ProColor cForeEnd, bool opaque = true, float bkgColorFactor = 1f)
	{
		Print(caption, col, row, cForeStart, cForeEnd, currentBColor, currentBColor, opaque, bkgColorFactor);
	}

	public static void Print(string caption, int col, int row, ProColor cForeStart, ProColor cForeEnd, ProColor cBackStart, ProColor cBackEnd, bool opaque = true, float bkgColorFactor = 1f)
	{
		int cl = CursorLeft;
		int ct = CursorTop;

		int[,,] colors = new int[2, 2, 3] {
			{{cForeStart.r, cForeStart.g, cForeStart.b}, {cForeEnd.r, cForeEnd.g, cForeEnd.b}},
			{{cBackStart.r, cBackStart.g, cBackStart.b}, {cBackEnd.r, cBackEnd.g, cBackEnd.b}}
		};
		int[,] colorDelta = new int[2, 3];
		for (int i = 0; i < 2; i++) {
			for (int c = 0; c < 3; c++) {
				colorDelta[i, c] = colors[i, 1, c] - colors[i, 0, c];
			}
		}

		int x = col == -1 ? (WindowWidth - caption.Length) / 2 : col;
		int y = row == -1 ? WindowHeight / 2 : row;
		SetCursorPosition(x, y);
		for (int i = 0; i < caption.Length; i++) {
			float f = (float) i / caption.Length;
			SetFColor(new ProColor {r = cForeStart.r + (int) (f * colorDelta[0, 0]), g = cForeStart.g + (int) (f * colorDelta[0, 1]), b = cForeStart.b + (int) (f * colorDelta[0, 2])}, false);
			if (opaque) {
			SetBColor(new ProColor {r = cBackStart.r + (int) (f * colorDelta[1, 0]), g = cBackStart.g + (int) (f * colorDelta[1, 1]), b = cBackStart.b + (int) (f * colorDelta[1, 2])}, false);
			} else {
			SetBColor(background[x + i, y, 0] * bkgColorFactor, false);
			}
			Write(caption[i]);
		}

		ResetColors();
		SetCursorPosition(cl, ct);
	}

	public static void DrawBox(
		int x, int y, int w, int h, string title = null, string caption = null, BorderStyle borderStyle = BorderStyle.SingleDefault,
		bool useCustomColors = false, ProColor cBorder = null, ProColor cBkg = null, ProColor cTitle = null, ProColor cCaption = null, bool opaque = true, float bkgColorFactor = 1f
	) {
		if (w < 2 || h < 2) return;

		int bs = (int) borderStyle;
		if (!useCustomColors || cBorder == null) cBorder = currentFColor;
		if (!useCustomColors || cBkg == null) cBkg = currentBColor;
		if (!useCustomColors || cTitle == null) cTitle = currentFColor;
		if (!useCustomColors || cCaption == null) cCaption = currentFColor;

		Print(borderElements[bs, 2] + borderElements[bs, 0].Repeat(w - 2) + borderElements[bs, 3], x, y, cBorder, cBorder, cBkg, cBkg, opaque, bkgColorFactor);
		for (int i = 0; i < h - 2; i++) {
			if (bkgColorFactor < 1f) {
				Print(borderElements[bs, 1] + " ".Repeat(w - 2) + borderElements[bs, 1], x, y + 1 + i, cBorder, cBorder, cBkg, cBkg, opaque, bkgColorFactor);
			} else {
				Print(borderElements[bs, 1], x, y + 1 + i, cBorder, cBorder, cBkg, cBkg, opaque, bkgColorFactor);
				Print(borderElements[bs, 1], x + w - 1, y + 1 + i, cBorder, cBorder, cBkg, cBkg, opaque, bkgColorFactor);
			}
		}
		Print(borderElements[bs, 4] + borderElements[bs, 0].Repeat(w - 2) + borderElements[bs, 5], x , y + h - 1, cBorder, cBorder, cBkg, cBkg, opaque, bkgColorFactor);

		if (title != null) {
			title = borderElements[bs, 6] + title + borderElements[bs, 7];
			int cx = x + (w - title.Length) / 2;
			Print(title, cx, y, cTitle, cTitle, cBkg, cBkg, opaque, bkgColorFactor);
		}

		if (caption != null) {
			int cx = x + (w - caption.Length) / 2;
			int cy = y + h / 2;
			Print(caption, cx, cy, cCaption, cCaption, cBkg, cBkg, opaque, bkgColorFactor);
		}
	}

	public static MetalText DrawMetalText(MetalText metalText)
	{
		metalText.currentInterval = metalText.interval - 1;
		metalText.currentHighlightPos = random.Next(1, metalText.caption.Length);
		metalText.currentHighlightDir = 1;
		metalTexts.Add(metalText);
		return metalText;
	}

	public static void RemoveMetalText(MetalText metalText) {
		metalTexts.Remove(metalText);
	}

	public static void DrawImage(Image image, int col, int row, ImageResolutionScale scale = ImageResolutionScale.Double, bool ignoreError = false, float alpha = -1f, bool draw = true)
	{
		if (image == null) return;
		if (image.Width % 2 != 0 || image.Height % 2 != 0) {
			DrawBox(col, row, (3 - (int) scale) * image.Width, image.Height / (int) scale, null, "Unable to render image.", BorderStyle.DoubleDefault, true, cWhite, cBlack, cWhite, cWhite);
			return;
		}

		Bitmap img = new Bitmap(image);

		for (int i = 0; i < img.Width; i++) {
		for (int j = 0; j < img.Height; j += (int) scale) {

		try {
			SetCursorPosition(col + i * (3 - (int) scale), row + j / (int) scale);
		} catch (ArgumentOutOfRangeException) {
			if (ignoreError) continue; else return;
		}
		int cl = CursorLeft; // caching is more efficient than multiple CursorXXX calls
		int ct = CursorTop;

		Color pixel = img.GetPixel(i, j);
		ProColor bColor = new ProColor {r = pixel.R, g = pixel.G, b = pixel.B, a = pixel.A};
		if (alpha != -1f) {
			float a = bColor.a / 255f * alpha;
			float ainv = 1 - a;
			bColor = new ProColor {
				r = (int) (bColor.r * a + background[cl, ct, 0].r * ainv),
				g = (int) (bColor.g * a + background[cl, ct, 0].g * ainv),
				b = (int) (bColor.b * a + background[cl, ct, 0].b * ainv)
			};
		}
		if (draw) SetBColor(bColor, false);

		if (scale == ImageResolutionScale.Double) {
			pixel = img.GetPixel(i, j + 1);
			ProColor fColor = new ProColor {r = pixel.R, g = pixel.G, b = pixel.B, a = pixel.A};
			float a = fColor.a / 255f * alpha;
			float ainv = 1 - a;
			if (alpha != -1f) {
				fColor = new ProColor {
					r = (int) (fColor.r * a + background[cl, ct, 1].r * ainv),
					g = (int) (fColor.g * a + background[cl, ct, 1].g * ainv),
					b = (int) (fColor.b * a + background[cl, ct, 1].b * ainv)
				};
			}
			if (draw) {
			SetFColor(fColor, false);
			Write("▄");
			}
			background[cl, ct, 0] = bColor;
			background[cl, ct, 1] = fColor;
			background[cl, ct, 0].e = 2; // dual "pixel" mode
		} else {
			if (draw) Write("  ");
			background[cl, ct, 0] = bColor;
			background[cl, ct, 0].e = 0; // normal mode
			background[cl + 1, ct, 0] = bColor;
			background[cl + 1, ct, 0].e = 0;
		}

		} }

		if (draw) {
		ResetColors();
		SetCursorPosition(0, 0);
		}
	}

	public static void RedrawBackground(int x, int y, int w, int h)
	{
		for (int j = y; j < y + h; j++) {
		for (int i = x; i < x + w; i++) {

		SetCursorPosition(i, j);
		SetBColor(background[i, j, 0], false);
		if (background[i, j, 0].e == 0) {
			Write(" ");
		} else if (background[i, j, 0].e == 2) {
			SetFColor(background[i, j, 1], false);
			Write("▄");
		}

		} }

		ResetColors();
		SetCursorPosition(0, 0);
	}

	public static void CreateMenu(ProMenu menu)
	{
		menu.isEnabled = true;
		menu.isVisible = true;
		menus.Add(menu);
	}

	public static void DeleteMenu(ProMenu menu)
	{
		menu.invokeReturnFlag = ProMenu.InvokeReturnFlag.TerminateMenu;
	}

	public static void RemoveMenusStartingFrom(int id)
	{
		for (int i = menus.Count - 1; i >= id; i--) menus.RemoveAt(i);
	}

	public static void ActivateMenuSystem(int m)
	{
		foreach (ProMenu menu in menus) {
			if (menu.isVisible) menu.Draw();
		}

		bool quit = false;
		do {

		menus[m].Draw(true);
		int result = menus[m].Update(menus);
		if (result == (int) ProMenu.InvokeReturnFlag.None) {
			quit = true;
		}
		else if (result == (int) ProMenu.InvokeReturnFlag.NextMenu) {
			menus[m].Draw();
			do {
			m++;
			if (m >= menus.Count) m = 0;
			} while (!menus[m].isEnabled);
		}
		else if (result == (int) ProMenu.InvokeReturnFlag.TerminateMenu) {
			RedrawBackground(menus[m].x, menus[m].y, menus[m].w, menus[m].h);
			menus.RemoveAt(m);
			if (menus.Count == 0) quit = true;
			do {
			m++;
			if (m >= menus.Count) m = 0;
			} while (!menus[m].isEnabled);
		}
		else if (result >= 10) {
			menus[m].Draw();
			m = result - 10;
		}

		} while (!quit);
	}

	private static void DynamicUpdate(object source, ElapsedEventArgs e)
	{
		if (menus.Count > 0) return;

		// Metal texts
		foreach (MetalText mt in metalTexts) {
		if (++mt.currentInterval == mt.interval) {

		mt.currentInterval = 0;
		mt.currentHighlightPos += mt.currentHighlightDir;
		if (mt.currentHighlightPos < 2 || mt.currentHighlightPos >= mt.caption.Length - 2) mt.currentHighlightDir *= -1;
		int x = mt.col == -1 ? (WindowWidth - mt.caption.Length) / 2 : mt.col;
		int y = mt.row == -1 ? WindowHeight / 2 : mt.row;
		Print(mt.caption.Substring(0, mt.currentHighlightPos), x, y, mt.textColor, mt.highlightColor, mt.backColor, mt.backColor);
		if (mt.currentHighlightPos < mt.caption.Length - 1) Print(mt.caption.Substring(mt.currentHighlightPos + 1), x + mt.currentHighlightPos + 1, y, mt.highlightColor, mt.textColor, mt.backColor, mt.backColor);

		}
		}
		// ---
	}

	public enum BorderStyle
	{
		SingleDefault = 0,
		DoubleDefault = 1
	}

	public enum ImageResolutionScale
	{
		Single = 1,
		Double = 2
	}

	public class MetalText
	{
		public string caption;
		public int col = -1;
		public int row = -1;
		public ProColor textColor = cWhite;
		public ProColor highlightColor = cBlack;
		public ProColor backColor = cBlack;
		public int interval = 2;
		public int currentInterval;
		public int currentHighlightPos;
		public int currentHighlightDir;
	}

}

public static class StringExtensions {

	public static string Repeat(this string instr, int n)
	{
		if (n <= 0) return null;
		if (string.IsNullOrEmpty(instr) || n == 1) return instr;
		return new StringBuilder(instr.Length * n).Insert(0, instr, n).ToString();
	}

}

}