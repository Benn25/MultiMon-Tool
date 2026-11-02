#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2015  Gerald Evans
//
// Dual Monitor Tools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DMT.Library.Ui;
using DMT.Library.Wallpaper;
using DMT.Library.WallpaperPlugin;

namespace DMT.Modules.WallpaperChanger
{
	/// <summary>
	/// Shows the current wallpaper and where each screen's image came from
	/// </summary>
	class WallpaperChangerPropertiesOptionsPanel : SettingsPage
	{
		WallpaperChangerModule _wallpaperChangerModule;
		WallpaperPreview _preview;
		TextBlock _hint;
		SettingsCard _providerCard;
		SettingsCard _sourceCard;
		SettingsCard _photographerCard;
		SettingsCard _detailsCard;
		SettingsCard _sizeCard;

		public WallpaperChangerPropertiesOptionsPanel(WallpaperChangerModule wallpaperChangerModule)
		{
			_wallpaperChangerModule = wallpaperChangerModule;

			_hint = AddText("The wallpaper that DMT made. Click a screen to see where its image came from.");
			_preview = Add(new WallpaperPreview(), 16);
			_preview.ScreenClicked += (s, e) => ShowSourceImageInfo();

			AddHeader("About this image");
			_providerCard = AddLinkCard("Source", Theme.GlyphFolder);
			_sourceCard = AddLinkCard("Image", Theme.GlyphPhoto);
			_photographerCard = AddLinkCard("Photographer", Theme.GlyphCamera);
			_detailsCard = AddLinkCard("More information", Theme.GlyphInfo);
			_sizeCard = AddCard("Original size", string.Empty, Theme.GlyphFullScreen);

			ShowNewWallpaper();
		}

		/// <summary>
		/// Not shown on this page (the module also calls this on the general page)
		/// </summary>
		public void ShowNextChange(string nextChangeMsg, Color foreColor)
		{
		}

		/// <summary>
		/// Shows the latest wallpaper (called by the module when the wallpaper changes)
		/// </summary>
		public void ShowNewWallpaper()
		{
			if (IsDisposed)
			{
				return;
			}

			Image sourceImage = _wallpaperChangerModule.Desktop.CurrentWallpaperImage;
			IWallpaperCompositor compositor = _wallpaperChangerModule.Desktop.CurrentCompositor;
			_preview.SetWallpaper(sourceImage, compositor);
			_hint.Text = sourceImage != null && compositor != null
				? "The wallpaper that DMT made. Click a screen to see where its image came from."
				: "Details will appear here once DMT has made a wallpaper.";
			ShowSourceImageInfo();
		}

		SettingsCard AddLinkCard(string title, string glyph)
		{
			FluentButton open = new FluentButton("Open", Theme.GlyphOpen, ButtonKind.Subtle);
			open.Click += (s, e) =>
			{
				string url = open.Tag as string;
				if (!string.IsNullOrEmpty(url))
				{
					try
					{
						Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
					}
					catch (Exception)
					{
						// nothing useful we can do
					}
				}
			};
			return AddCard(title, string.Empty, glyph, open);
		}

		void ShowSourceImageInfo()
		{
			ProviderImage providerImage = _wallpaperChangerModule.Desktop.GetProviderImage(_preview.SelectedScreen);
			if (providerImage == null)
			{
				ShowLink(_providerCard, null, null);
				ShowLink(_sourceCard, null, null);
				ShowLink(_photographerCard, null, null);
				ShowLink(_detailsCard, null, null);
				_sizeCard.Description = "—";
				return;
			}

			ShowLink(_providerCard, providerImage.Provider, providerImage.ProviderUrl);
			ShowLink(_sourceCard, providerImage.Source, providerImage.SourceUrl);
			ShowLink(_photographerCard, providerImage.Photographer, providerImage.PhotographerUrl);
			ShowLink(_detailsCard, providerImage.MoreInfo, providerImage.MoreInfoUrl);
			_sizeCard.Description = providerImage.Image != null ? string.Format("{0} × {1}", providerImage.Image.Width, providerImage.Image.Height) : "—";
		}

		static void ShowLink(SettingsCard card, string text, string url)
		{
			card.Description = string.IsNullOrEmpty(text) ? "—" : text;
			card.Action.Tag = url;
			card.Action.Visible = !string.IsNullOrEmpty(url);
		}

		/// <summary>
		/// Draws the wallpaper with the screens outlined; a screen can be clicked to select it
		/// </summary>
		class WallpaperPreview : ThemedControl, IHeightForWidth
		{
			Image _image;
			IWallpaperCompositor _compositor;
			Bitmap _scaled;
			Rectangle _imageRect;

			public WallpaperPreview()
			{
				SetStyle(ControlStyles.Selectable, false);
				Cursor = Cursors.Hand;
			}

			public event EventHandler ScreenClicked;

			public int SelectedScreen { get; private set; }

			public void SetWallpaper(Image image, IWallpaperCompositor compositor)
			{
				_image = image;
				_compositor = compositor;
				if (_scaled != null)
				{
					_scaled.Dispose();
					_scaled = null;
				}

				if (_compositor != null && SelectedScreen >= _compositor.AllScreens.Count)
				{
					SelectedScreen = 0;
				}

				Invalidate();
			}

			public int GetHeightForWidth(int width)
			{
				if (_image == null)
				{
					return S(120);
				}

				return Math.Min((int)((long)width * _image.Height / _image.Width), S(360));
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing && _scaled != null)
				{
					_scaled.Dispose();
				}

				base.Dispose(disposing);
			}

			protected override void OnResize(EventArgs e)
			{
				base.OnResize(e);
				if (_scaled != null)
				{
					_scaled.Dispose();
					_scaled = null;
				}
			}

			Rectangle ScreenRect(int screenIndex)
			{
				return WallpaperCompositor.CalcDestRect(_compositor.DesktopRect, _imageRect, _compositor.AllScreens[screenIndex].ScreenRect);
			}

			protected override void OnMouseClick(MouseEventArgs e)
			{
				base.OnMouseClick(e);
				if (_compositor == null)
				{
					return;
				}

				for (int i = 0; i < _compositor.AllScreens.Count; i++)
				{
					if (ScreenRect(i).Contains(e.Location))
					{
						SelectedScreen = i;
						Invalidate();
						if (ScreenClicked != null)
						{
							ScreenClicked(this, EventArgs.Empty);
						}

						break;
					}
				}
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				Graphics g = e.Graphics;
				g.Clear(ParentBackColor);

				if (_image == null || _compositor == null)
				{
					Theme.FillRounded(g, ClientRectangle, S(6), Theme.Surface, Theme.Stroke);
					TextRenderer.DrawText(g, "No wallpaper yet", Theme.GetFont(TextRole.Body, DeviceDpi), ClientRectangle, Theme.TextTertiary,
						TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
					return;
				}

				_imageRect = ImageFitter.UnderStretch(_image.Size, ClientRectangle);
				if (_scaled == null || _scaled.Size != _imageRect.Size)
				{
					if (_scaled != null)
					{
						_scaled.Dispose();
					}

					// scaling the full size wallpaper is slow, so only do it when the size changes
					_scaled = new Bitmap(Math.Max(1, _imageRect.Width), Math.Max(1, _imageRect.Height));
					using (Graphics sg = Graphics.FromImage(_scaled))
					{
						sg.InterpolationMode = InterpolationMode.HighQualityBicubic;
						sg.DrawImage(_image, 0, 0, _scaled.Width, _scaled.Height);
					}
				}

				g.SmoothingMode = SmoothingMode.AntiAlias;
				using (GraphicsPath clip = Theme.RoundedRect(_imageRect, S(6)))
				{
					g.SetClip(clip);
					g.DrawImage(_scaled, _imageRect.Location);
					g.ResetClip();
				}

				Font font = Theme.GetFont(TextRole.Subtitle, DeviceDpi);
				for (int i = 0; i < _compositor.AllScreens.Count; i++)
				{
					Rectangle screen = Rectangle.Inflate(ScreenRect(i), -S(2), -S(2));
					bool selected = i == SelectedScreen;
					using (Pen pen = new Pen(selected ? Theme.Accent : Color.FromArgb(160, Color.White), S(selected ? 3 : 1)))
					using (GraphicsPath path = Theme.RoundedRect(screen, S(5)))
					{
						g.DrawPath(pen, path);
					}

					// the screen number on a small dark badge so it's readable on any image
					string number = (i + 1).ToString();
					Size size = TextRenderer.MeasureText(number, font, Size.Empty, TextFormatFlags.NoPadding);
					Rectangle badge = new Rectangle(screen.Left + S(8), screen.Top + S(8), size.Width + S(16), size.Height + S(4));
					Theme.FillRounded(g, badge, S(4), Color.FromArgb(170, 0, 0, 0));
					TextRenderer.DrawText(g, number, font, badge, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
				}
			}
		}
	}
}
