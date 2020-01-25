namespace ProConsole {

public class ProColor {

	public int r = 0, g = 0, b = 0, a = 1;
	public int e = 0;

	public ProColor() { }

	public ProColor(int r, int g, int b)
	{
		this.r = r;
		this.g = g;
		this.b = b;
	}

	public static ProColor operator * (ProColor color, float f)
	{
		return new ProColor {
			r = (int) (color.r * f),
			g = (int) (color.g * f),
			b = (int) (color.b * f)
		};
	}

}

}