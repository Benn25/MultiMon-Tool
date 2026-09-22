using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace DMT.Library.GuiUtils
{
	public static class DisplayTextHelper
	{
		public static string Truncate(string text, Graphics g, Font font, int availableWidth, out Size size)
		{
			int maxWidth = availableWidth * 2;

			size = g.MeasureString(text, font, maxWidth).ToSize();
			if (size.Width <= availableWidth)
			{
				// enough room to display the text without truncation
				return text;
			}

			// ALT: use binary chop if we expect long strings exceeding available width (but we don't)
			for (int len = text.Length - 1; len > 0; len--)
			{
				string truncated = text.Substring(0, len) + "...";
				size = g.MeasureString(truncated, font, maxWidth).ToSize();
				if (size.Width <= availableWidth)
				{
					// this truncation will fit
					return truncated;
				}
			}

			// can't even get a single letter followed by "..." to fit
			// ALT: return first letter, or just "..."?
			return "";
		}
	}
}
