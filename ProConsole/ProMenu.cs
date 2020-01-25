using System;
using System.Collections.Generic;
using static System.Console;
using static ProConsole.ProConsoleFunctions;

namespace ProConsole {

public class ProMenu {

	public int x, y;
	public int w, h;
	public string title;
	public MenuAlignment alignment = MenuAlignment.AlignCenter;
	public BorderStyle borderStyle = BorderStyle.SingleDefault;
	public ProColor[] borderColors = new ProColor[4];
	public float inactiveColorFactor = 0.5f;
	public float inactiveItemColorFactor = 0.25f;
	public float bkgColorFactor = 0.85f;
	public bool opaque = true;
	public int contentMarginTop = 2;
	public int itemSpacing = 2;
	public bool allowNoItems = false;
	public bool hasShortcut = false;
	public ConsoleKey shortcut = ConsoleKey.A;

	public List<MenuItem> items = new List<MenuItem>();
	public int currentItem = 0;
	public bool isEnabled = true;
	public bool isVisible = true;
	public bool quit;
	public InvokeReturnFlag invokeReturnFlag;

	private bool currentState;
	private int firstVisibleItem = 0;
	private int lastVisibleItem;

	// Constructor
	public ProMenu(int x, int y, int w, int h, ProColor[] borderColors)
	{
		this.x = x; this.y = y; this.w = w; this.h = h;
		this.borderColors = borderColors;
	}

	public void AddShortcut(ConsoleKey key)
	{
		hasShortcut = true;
		shortcut = key;
	}

	public int Update(List<ProMenu> menus)
	{
		if (!allowNoItems && items.Count == 0) return (int) InvokeReturnFlag.NextMenu;

		quit = false;
		invokeReturnFlag = InvokeReturnFlag.None;
		do {

		ConsoleKey key = ReadKey(true).Key;
		switch (key) {
		case ConsoleKey.UpArrow:
			if (items.Count == 0) break;
			DrawArrow(false, borderColors[1], opaque, bkgColorFactor);
			do {
			if (--currentItem < 0) {
				currentItem = items.Count - 1;
				if (itemSpacing == 1 && items.Count > h - 2 - contentMarginTop) {
					firstVisibleItem = currentItem - (h - 2 - contentMarginTop);
					Draw(currentState);
				}
			} else if (itemSpacing == 1 && currentItem < firstVisibleItem) {
				firstVisibleItem--;
				Draw(currentState);
			}
			} while (!items[currentItem].isActive);
			DrawArrow( true, borderColors[2], opaque, bkgColorFactor);
			break;
		case ConsoleKey.DownArrow:
			if (items.Count == 0) break;
			DrawArrow(false, borderColors[1], opaque, bkgColorFactor);
			do {
			if (++currentItem > items.Count - 1) {
				currentItem = 0;
				if (itemSpacing == 1) {
					firstVisibleItem = 0;
					Draw(currentState);
				}
			} else if (itemSpacing == 1 && currentItem > lastVisibleItem) {
				firstVisibleItem++;
				Draw(currentState);
			}
			} while (!items[currentItem].isActive);
			DrawArrow( true, borderColors[2], opaque, bkgColorFactor);
			break;
		case ConsoleKey.Enter:
			if (items.Count == 0) break;
			if (items[currentItem].selectAction != null) items[currentItem].selectAction.Invoke();
			if (invokeReturnFlag != InvokeReturnFlag.None) return (int) invokeReturnFlag;
			break;
		case ConsoleKey.Tab:
			return (int) InvokeReturnFlag.NextMenu;
		}

		for (int i = 0; i < menus.Count; i++) {
			if (menus[i] != this && menus[i].isEnabled && menus[i].hasShortcut && menus[i].shortcut == key) return 10 + i;
		}

		} while (!quit);

		return (int) InvokeReturnFlag.None;
	}

	public void DrawArrow(bool visible, ProColor color, bool opaque, float bkgColorFactor)
	{
		if (opaque) {
			SetFColor(color, false);
			SetCursorPosition(x + 2, y + contentMarginTop + (currentItem - firstVisibleItem) * itemSpacing);
			Write("»");
			SetCursorPosition(x + w - 3, y + contentMarginTop + (currentItem - firstVisibleItem) * itemSpacing);
			Write("«");
			ResetColors();
		} else {
			Print(visible ? "»" : " ", x + 2, y + contentMarginTop + (currentItem - firstVisibleItem) * itemSpacing, color, color, opaque, bkgColorFactor);
			Print(visible ? "«" : " ", x + w - 3, y + contentMarginTop + (currentItem - firstVisibleItem) * itemSpacing, color, color, opaque, bkgColorFactor);
		}
	}

	public void Draw(bool isActive = false)
	{
		currentState = isActive;
		float c = isActive ? 1f : (isEnabled ? inactiveColorFactor : inactiveColorFactor / 2f);
		DrawBox(x, y, w, h, title, null, borderStyle, true, borderColors[0] * c, borderColors[1] * c, borderColors[2] * c, borderColors[3] * c, opaque, bkgColorFactor);

		if (hasShortcut) {
		Print("[" + shortcut.ToString() + "]", x + 1, y, borderColors[0] * c, borderColors[0] * c, opaque, bkgColorFactor);
		}

		for (int i = firstVisibleItem; i < items.Count; i++) {

		int yPos = y + contentMarginTop + (i - firstVisibleItem) * itemSpacing;
		if (yPos > y + h - 2) break;
		lastVisibleItem = i;

		int xPos = 0;
		switch (alignment) {
		case MenuAlignment.AlignLeft:
			xPos = x + 4;
			break;
		case MenuAlignment.AlignCenter:
			xPos = x + (w - items[i].caption.Length) / 2;
			break;
		case MenuAlignment.AlignRight:
			xPos = x + (w - items[i].caption.Length) - 4;
			break;
		}

		float d = items[i].isActive ? 1f : inactiveItemColorFactor;
		Print(items[i].caption, xPos, yPos, borderColors[2] * c * d, borderColors[3] * c * d, opaque, bkgColorFactor);

		}

		if (items.Count > h - 1 - contentMarginTop) {
		Print(string.Format("[{0}-{1}/{2}]", firstVisibleItem + 1, lastVisibleItem + 1, items.Count), x + 1, y + h - 1, borderColors[0] * c, borderColors[0] * c, opaque, bkgColorFactor);
		}

		if (items.Count > 0) DrawArrow(true, borderColors[2] * c, opaque, bkgColorFactor);
	}

	public MenuItem AddMenuItem(string caption, Action selectAction = null, bool isActive = true, bool redraw = false)
	{
		MenuItem m = new MenuItem() {
			caption = caption,
			selectAction = selectAction,
			isActive = isActive
		};
		items.Add(m);
		if (redraw) Draw(currentState);
		return m;
	}

	public void RemoveMenuItem(int m)
	{
		if (currentItem >= m) if (--currentItem < 0) currentItem = 0;
		if (m >= 0 && m < items.Count) items.RemoveAt(m);
		Draw(currentState);
	}

	public void SetMenuItemActive(MenuItem m, bool isActive = true)
	{
		m.isActive = isActive;
		Draw(currentState);
	}

	public void SetMenuItemActive(int m, bool isActive = true)
	{
		if (m >= 0 && m < items.Count) items[m].isActive = isActive;
		Draw(currentState);
	}

	public class MenuItem
	{
		public string caption;
		public Action selectAction;
		public bool isActive = true;
	}

	public enum MenuAlignment
	{
		AlignLeft = 0,
		AlignCenter = 1,
		AlignRight = 2
	}

	public enum InvokeReturnFlag
	{
		TerminateMenu = -1,
		None = 0,
		NextMenu = 1
	}

}

}