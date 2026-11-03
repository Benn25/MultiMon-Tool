#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2026
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

namespace DMT.Library.Ui
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Drawing;
	using System.Drawing.Drawing2D;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Forms;

	using DMT.Library.Utils;

	/// <summary>
	/// A scrolling grid of picture thumbnails that can be selected, removed, reordered by dragging,
	/// and added to by dropping files or folders from Explorer
	/// </summary>
	class ThumbnailGrid : ScrollableControl, IThemeAware
	{
		const string InternalDragFormat = "DMT.ThumbnailGrid.Move";

		List<string> _items = new List<string>();
		HashSet<int> _selected = new HashSet<int>();
		int _focusIndex = -1;
		int _anchorIndex = -1;
		int _hoverIndex = -1;
		int _dropIndex = -1;
		Point _mouseDownPoint;
		bool _mouseDownOnSelected;

		// thumbnails are made on a background thread
		readonly Dictionary<string, Image> _thumbnails = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
		readonly HashSet<string> _failed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		readonly HashSet<string> _queued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		readonly Stack<string> _queue = new Stack<string>();
		readonly object _queueLock = new object();
		readonly SemaphoreSlim _queueSignal = new SemaphoreSlim(0);
		readonly CancellationTokenSource _cancel = new CancellationTokenSource();

		public ThumbnailGrid()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
				| ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
			TabStop = true;
			AutoScroll = true;
			AllowDrop = true;
			BackColor = Theme.Surface;
			AccessibleRole = AccessibleRole.List;
			AccessibleName = "Pictures";
			Task.Run(() => LoadThumbnails(_cancel.Token));
		}

		/// <summary>
		/// Raised when pictures are added, removed or moved
		/// </summary>
		public event EventHandler ItemsChanged;

		public event EventHandler SelectionChanged;

		/// <summary>
		/// Gets the pictures in their current order
		/// </summary>
		public IList<string> Items
		{
			get { return _items.AsReadOnly(); }
		}

		public int SelectedCount
		{
			get { return _selected.Count; }
		}

		/// <summary>
		/// Gets or sets whether adding a dropped folder includes its subfolders
		/// </summary>
		[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
		public bool IncludeSubfolders { get; set; } = true;

		public void ApplyTheme()
		{
			BackColor = Theme.Surface;
			Invalidate();
		}

		/// <summary>
		/// Adds pictures to the end of the list, ignoring any already in it
		/// </summary>
		/// <returns>Number of pictures added</returns>
		public int AddItems(IEnumerable<string> paths, int insertAt = -1)
		{
			HashSet<string> existing = new HashSet<string>(_items, StringComparer.OrdinalIgnoreCase);
			List<string> toAdd = new List<string>();
			foreach (string path in paths)
			{
				if (!string.IsNullOrWhiteSpace(path) && existing.Add(path))
				{
					toAdd.Add(path);
				}
			}

			if (toAdd.Count == 0)
			{
				return 0;
			}

			if (insertAt < 0 || insertAt > _items.Count)
			{
				insertAt = _items.Count;
			}

			_items.InsertRange(insertAt, toAdd);
			_selected.Clear();
			for (int i = insertAt; i < insertAt + toAdd.Count; i++)
			{
				_selected.Add(i);
			}

			_focusIndex = insertAt;
			_anchorIndex = insertAt;
			ItemsHaveChanged();
			EnsureVisible(insertAt);
			return toAdd.Count;
		}

		/// <summary>
		/// Replaces all of the pictures
		/// </summary>
		public void SetItems(IEnumerable<string> paths)
		{
			_items = paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
			_selected.Clear();
			_focusIndex = _items.Count > 0 ? 0 : -1;
			_anchorIndex = _focusIndex;
			AutoScrollPosition = Point.Empty;
			ItemsHaveChanged();
		}

		public void RemoveSelected()
		{
			if (_selected.Count == 0)
			{
				return;
			}

			int first = _selected.Min();
			_items = _items.Where((item, index) => !_selected.Contains(index)).ToList();
			_selected.Clear();
			_focusIndex = Math.Min(first, _items.Count - 1);
			_anchorIndex = _focusIndex;
			if (_focusIndex >= 0)
			{
				_selected.Add(_focusIndex);
			}

			ItemsHaveChanged();
		}

		/// <summary>
		/// Removes pictures whose files no longer exist (web addresses are kept)
		/// </summary>
		/// <returns>The number removed</returns>
		public int RemoveMissing()
		{
			int before = _items.Count;
			_items = _items.Where(p => IsUrl(p) || File.Exists(p)).ToList();
			_selected.Clear();
			_focusIndex = _items.Count > 0 ? 0 : -1;
			ItemsHaveChanged();
			return before - _items.Count;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_cancel.Cancel();
				_queueSignal.Release();
				lock (_queueLock)
				{
					foreach (Image image in _thumbnails.Values)
					{
						image.Dispose();
					}

					_thumbnails.Clear();
				}
			}

			base.Dispose(disposing);
		}

		static bool IsUrl(string path)
		{
			return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
		}

		void ItemsHaveChanged()
		{
			UpdateScrollSize();
			Invalidate();
			if (ItemsChanged != null)
			{
				ItemsChanged(this, EventArgs.Empty);
			}

			RaiseSelectionChanged();
		}

		void RaiseSelectionChanged()
		{
			if (SelectionChanged != null)
			{
				SelectionChanged(this, EventArgs.Empty);
			}
		}

		#region Layout

		int Gap { get { return LogicalToDeviceUnits(8); } }
		int Pad { get { return LogicalToDeviceUnits(10); } }
		int ThumbWidth { get { return LogicalToDeviceUnits(132); } }
		int ThumbHeight { get { return LogicalToDeviceUnits(90); } }
		int TileWidth { get { return ThumbWidth + LogicalToDeviceUnits(12); } }
		int TileHeight { get { return ThumbHeight + LogicalToDeviceUnits(36); } }

		int Columns
		{
			get { return Math.Max(1, (ClientSize.Width - Pad * 2 + Gap) / (TileWidth + Gap)); }
		}

		Rectangle TileRect(int index)
		{
			int columns = Columns;
			int usedWidth = columns * TileWidth + (columns - 1) * Gap;
			int left = Pad + Math.Max(0, (ClientSize.Width - Pad * 2 - usedWidth) / 2);
			int col = index % columns;
			int row = index / columns;
			return new Rectangle(left + col * (TileWidth + Gap), Pad + row * (TileHeight + Gap) + AutoScrollPosition.Y, TileWidth, TileHeight);
		}

		int IndexAt(Point point)
		{
			for (int i = FirstVisible(); i < _items.Count && i <= LastVisible(); i++)
			{
				if (TileRect(i).Contains(point))
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Where dropped items would go for the given point
		/// </summary>
		int InsertIndexAt(Point point)
		{
			int columns = Columns;
			int row = Math.Max(0, (point.Y - AutoScrollPosition.Y - Pad) / (TileHeight + Gap));
			Rectangle first = TileRect(row * columns);
			int col = (int)Math.Round((point.X - first.Left) / (double)(TileWidth + Gap));
			col = Math.Max(0, Math.Min(columns, col));
			return Math.Min(_items.Count, row * columns + col);
		}

		int FirstVisible()
		{
			int row = Math.Max(0, (-AutoScrollPosition.Y - Pad) / (TileHeight + Gap));
			return row * Columns;
		}

		int LastVisible()
		{
			int row = (-AutoScrollPosition.Y + ClientSize.Height) / (TileHeight + Gap) + 1;
			return Math.Min(_items.Count - 1, (row + 1) * Columns);
		}

		void UpdateScrollSize()
		{
			int rows = (_items.Count + Columns - 1) / Columns;
			AutoScrollMinSize = new Size(0, Pad * 2 + rows * TileHeight + Math.Max(0, rows - 1) * Gap);
		}

		void EnsureVisible(int index)
		{
			if (index < 0 || index >= _items.Count)
			{
				return;
			}

			Rectangle tile = TileRect(index);
			int scroll = -AutoScrollPosition.Y;
			if (tile.Top < 0)
			{
				AutoScrollPosition = new Point(0, scroll + tile.Top - Pad);
			}
			else if (tile.Bottom > ClientSize.Height)
			{
				AutoScrollPosition = new Point(0, scroll + tile.Bottom - ClientSize.Height + Pad);
			}

			Invalidate();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			UpdateScrollSize();
		}

		protected override void OnScroll(ScrollEventArgs se)
		{
			base.OnScroll(se);
			Invalidate();
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);
			Invalidate();
		}

		#endregion

		#region Painting

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(BackColor);

			if (_items.Count == 0)
			{
				TextRenderer.DrawText(g, "No pictures yet.\nAdd pictures or a folder, or drag them here from Explorer.",
					Theme.GetFont(TextRole.Body, DeviceDpi), ClientRectangle, Theme.TextTertiary,
					TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
				return;
			}

			Font captionFont = Theme.GetFont(TextRole.Caption, DeviceDpi);
			Font iconFont = Theme.GetFont(TextRole.IconLarge, DeviceDpi);
			Color selectedFill = GridStyle.Blend(Theme.Surface, Theme.Accent, Theme.IsDark ? 0.30f : 0.18f);

			for (int i = FirstVisible(); i <= LastVisible() && i < _items.Count; i++)
			{
				Rectangle tile = TileRect(i);
				if (!tile.IntersectsWith(e.ClipRectangle))
				{
					continue;
				}

				bool selected = _selected.Contains(i);
				if (selected)
				{
					Theme.FillRounded(g, tile, LogicalToDeviceUnits(6), selectedFill, Theme.Accent);
				}
				else if (i == _hoverIndex)
				{
					Theme.FillRounded(g, tile, LogicalToDeviceUnits(6), Theme.SurfaceHover);
				}

				string path = _items[i];
				Rectangle thumbArea = new Rectangle(tile.Left + LogicalToDeviceUnits(6), tile.Top + LogicalToDeviceUnits(6), ThumbWidth, ThumbHeight);
				Image thumb = GetThumbnail(path);
				if (thumb != null)
				{
					// centre the thumbnail in its area, with rounded corners
					Rectangle dest = new Rectangle(
						thumbArea.Left + (thumbArea.Width - thumb.Width) / 2,
						thumbArea.Top + (thumbArea.Height - thumb.Height) / 2,
						thumb.Width, thumb.Height);
					g.SmoothingMode = SmoothingMode.AntiAlias;
					using (GraphicsPath clip = Theme.RoundedRect(dest, LogicalToDeviceUnits(4)))
					{
						g.SetClip(clip);
						g.DrawImage(thumb, dest);
						g.ResetClip();
					}
				}
				else
				{
					Theme.FillRounded(g, thumbArea, LogicalToDeviceUnits(4), Theme.ControlFill);
					string glyph;
					if (IsUrl(path))
					{
						glyph = "";	// globe
					}
					else if (IsFailed(path))
					{
						glyph = Theme.GlyphWarning;
					}
					else
					{
						glyph = Theme.GlyphPhoto;
					}

					TextRenderer.DrawText(g, glyph, iconFont, thumbArea, Theme.TextTertiary,
						TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
				}

				string caption = IsUrl(path) ? path : Path.GetFileName(path);
				Rectangle captionRect = new Rectangle(tile.Left + LogicalToDeviceUnits(4), thumbArea.Bottom + LogicalToDeviceUnits(6), tile.Width - LogicalToDeviceUnits(8), LogicalToDeviceUnits(20));
				TextRenderer.DrawText(g, caption, captionFont, captionRect, IsFailed(path) ? Theme.TextTertiary : Theme.TextPrimary,
					TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

				if (Focused && ShowFocusCues && i == _focusIndex)
				{
					Rectangle focus = Rectangle.Inflate(tile, -1, -1);
					using (Pen pen = new Pen(Theme.TextPrimary))
					using (GraphicsPath path2 = Theme.RoundedRect(focus, LogicalToDeviceUnits(6)))
					{
						g.DrawPath(pen, path2);
					}
				}
			}

			if (_dropIndex >= 0)
			{
				// insertion marker while dragging
				bool atEnd = _dropIndex >= _items.Count;
				Rectangle tile = TileRect(atEnd ? _items.Count - 1 : _dropIndex);
				int x = atEnd ? tile.Right + Gap / 2 : tile.Left - Gap / 2;
				using (Pen pen = new Pen(Theme.Accent, LogicalToDeviceUnits(3)))
				{
					g.DrawLine(pen, x, tile.Top, x, tile.Bottom);
				}
			}
		}

		bool IsFailed(string path)
		{
			lock (_queueLock)
			{
				return _failed.Contains(path);
			}
		}

		Image GetThumbnail(string path)
		{
			if (IsUrl(path))
			{
				return null;
			}

			lock (_queueLock)
			{
				Image thumb;
				if (_thumbnails.TryGetValue(path, out thumb))
				{
					return thumb;
				}

				if (!_failed.Contains(path) && _queued.Add(path))
				{
					// most recently requested first, so what's on screen loads first
					_queue.Push(path);
					_queueSignal.Release();
				}
			}

			return null;
		}

		void LoadThumbnails(CancellationToken cancel)
		{
			while (!cancel.IsCancellationRequested)
			{
				_queueSignal.Wait();
				if (cancel.IsCancellationRequested)
				{
					return;
				}

				string path;
				lock (_queueLock)
				{
					if (_queue.Count == 0)
					{
						continue;
					}

					path = _queue.Pop();
				}

				Image thumb = MakeThumbnail(path, ThumbWidth, ThumbHeight);
				lock (_queueLock)
				{
					_queued.Remove(path);
					if (thumb != null)
					{
						_thumbnails[path] = thumb;
					}
					else
					{
						_failed.Add(path);
					}
				}

				try
				{
					if (IsHandleCreated && !IsDisposed)
					{
						BeginInvoke(new Action(Invalidate));
					}
				}
				catch (Exception)
				{
					// closing down
				}
			}
		}

		static Image MakeThumbnail(string path, int maxWidth, int maxHeight)
		{
			try
			{
				using (FileStream stream = File.OpenRead(path))
				using (Image image = Image.FromStream(stream, false, false))
				{
					// photos from phones/cameras are often stored on their side
					RotateFlipType rotate;
					switch (image.GetExifOrientation())
					{
						case 3: rotate = RotateFlipType.Rotate180FlipNone; break;
						case 6: rotate = RotateFlipType.Rotate90FlipNone; break;
						case 8: rotate = RotateFlipType.Rotate270FlipNone; break;
						default: rotate = RotateFlipType.RotateNoneFlipNone; break;
					}

					bool sideways = rotate == RotateFlipType.Rotate90FlipNone || rotate == RotateFlipType.Rotate270FlipNone;
					int sourceWidth = sideways ? image.Height : image.Width;
					int sourceHeight = sideways ? image.Width : image.Height;
					double scale = Math.Min((double)maxWidth / sourceWidth, (double)maxHeight / sourceHeight);
					int width = Math.Max(1, (int)(sourceWidth * scale));
					int height = Math.Max(1, (int)(sourceHeight * scale));

					Bitmap thumb = new Bitmap(sideways ? height : width, sideways ? width : height);
					using (Graphics g = Graphics.FromImage(thumb))
					{
						g.InterpolationMode = InterpolationMode.HighQualityBicubic;
						g.DrawImage(image, 0, 0, thumb.Width, thumb.Height);
					}

					thumb.RotateFlip(rotate);
					return thumb;
				}
			}
			catch (Exception)
			{
				// missing or not a picture
				return null;
			}
		}

		#endregion

		#region Mouse and keyboard

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			int index = IndexAt(e.Location);
			if (index != _hoverIndex)
			{
				_hoverIndex = index;
				Invalidate();
			}

			// start dragging the selected pictures to move them
			if (e.Button == MouseButtons.Left && _mouseDownOnSelected && _selected.Count > 0)
			{
				Size dragSize = SystemInformation.DragSize;
				if (Math.Abs(e.X - _mouseDownPoint.X) > dragSize.Width || Math.Abs(e.Y - _mouseDownPoint.Y) > dragSize.Height)
				{
					_mouseDownOnSelected = false;
					DataObject data = new DataObject(InternalDragFormat, true);
					DoDragDrop(data, DragDropEffects.Move);
					_dropIndex = -1;
					Invalidate();
				}
			}
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			_hoverIndex = -1;
			Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Focus();
			_mouseDownPoint = e.Location;
			int index = IndexAt(e.Location);
			_mouseDownOnSelected = index >= 0 && _selected.Contains(index) && ModifierKeys == Keys.None;

			if (index < 0)
			{
				if (ModifierKeys == Keys.None)
				{
					_selected.Clear();
					Invalidate();
					RaiseSelectionChanged();
				}

				return;
			}

			if (_mouseDownOnSelected && e.Button == MouseButtons.Left)
			{
				// may be the start of a drag, so leave the selection alone until mouse up
				_focusIndex = index;
				return;
			}

			SelectWithModifiers(index);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (_mouseDownOnSelected)
			{
				// a click (not a drag) on a selected picture just selects it
				_mouseDownOnSelected = false;
				int index = IndexAt(e.Location);
				if (index >= 0)
				{
					SelectWithModifiers(index);
				}
			}
		}

		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			base.OnMouseDoubleClick(e);
			int index = IndexAt(e.Location);
			if (index >= 0)
			{
				OpenItem(index);
			}
		}

		void OpenItem(int index)
		{
			try
			{
				Process.Start(new ProcessStartInfo(_items[index]) { UseShellExecute = true });
			}
			catch (Exception)
			{
				// nothing useful we can do
			}
		}

		void SelectWithModifiers(int index)
		{
			if ((ModifierKeys & Keys.Shift) != 0 && _anchorIndex >= 0)
			{
				if ((ModifierKeys & Keys.Control) == 0)
				{
					_selected.Clear();
				}

				for (int i = Math.Min(_anchorIndex, index); i <= Math.Max(_anchorIndex, index); i++)
				{
					_selected.Add(i);
				}
			}
			else if ((ModifierKeys & Keys.Control) != 0)
			{
				if (!_selected.Remove(index))
				{
					_selected.Add(index);
				}

				_anchorIndex = index;
			}
			else
			{
				_selected.Clear();
				_selected.Add(index);
				_anchorIndex = index;
			}

			_focusIndex = index;
			EnsureVisible(index);
			Invalidate();
			RaiseSelectionChanged();
		}

		protected override bool IsInputKey(Keys keyData)
		{
			switch (keyData & Keys.KeyCode)
			{
				case Keys.Left:
				case Keys.Right:
				case Keys.Up:
				case Keys.Down:
				case Keys.Home:
				case Keys.End:
					return true;
			}

			return base.IsInputKey(keyData);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (_items.Count == 0)
			{
				return;
			}

			int columns = Columns;
			int index = Math.Max(0, _focusIndex);
			switch (e.KeyCode)
			{
				case Keys.Left: index--; break;
				case Keys.Right: index++; break;
				case Keys.Up: index -= columns; break;
				case Keys.Down: index += columns; break;
				case Keys.Home: index = 0; break;
				case Keys.End: index = _items.Count - 1; break;
				case Keys.Delete:
					RemoveSelected();
					e.Handled = true;
					return;
				case Keys.A:
					if (e.Control)
					{
						_selected = new HashSet<int>(Enumerable.Range(0, _items.Count));
						Invalidate();
						RaiseSelectionChanged();
						e.Handled = true;
					}

					return;
				case Keys.Enter:
					OpenItem(index);
					return;
				default:
					return;
			}

			e.Handled = true;
			SelectWithModifiers(Math.Max(0, Math.Min(_items.Count - 1, index)));
		}

		protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
		protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

		#endregion

		#region Drag and drop

		protected override void OnDragEnter(DragEventArgs drgevent)
		{
			base.OnDragEnter(drgevent);
			UpdateDrag(drgevent);
		}

		protected override void OnDragOver(DragEventArgs drgevent)
		{
			base.OnDragOver(drgevent);
			UpdateDrag(drgevent);

			// scroll when dragging near the top or bottom
			Point point = PointToClient(new Point(drgevent.X, drgevent.Y));
			int edge = LogicalToDeviceUnits(30);
			if (point.Y < edge)
			{
				AutoScrollPosition = new Point(0, -AutoScrollPosition.Y - LogicalToDeviceUnits(20));
			}
			else if (point.Y > ClientSize.Height - edge)
			{
				AutoScrollPosition = new Point(0, -AutoScrollPosition.Y + LogicalToDeviceUnits(20));
			}
		}

		protected override void OnDragLeave(EventArgs e)
		{
			base.OnDragLeave(e);
			_dropIndex = -1;
			Invalidate();
		}

		void UpdateDrag(DragEventArgs drgevent)
		{
			bool files = drgevent.Data.GetDataPresent(DataFormats.FileDrop);
			bool internalMove = drgevent.Data.GetDataPresent(InternalDragFormat);
			drgevent.Effect = internalMove ? DragDropEffects.Move : (files ? DragDropEffects.Copy : DragDropEffects.None);

			int dropIndex = drgevent.Effect == DragDropEffects.None ? -1 : InsertIndexAt(PointToClient(new Point(drgevent.X, drgevent.Y)));
			if (dropIndex != _dropIndex)
			{
				_dropIndex = dropIndex;
				Invalidate();
			}
		}

		protected override void OnDragDrop(DragEventArgs drgevent)
		{
			base.OnDragDrop(drgevent);
			int insertAt = _dropIndex >= 0 ? _dropIndex : _items.Count;
			_dropIndex = -1;

			if (drgevent.Data.GetDataPresent(InternalDragFormat))
			{
				MoveSelected(insertAt);
			}
			else if (drgevent.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] dropped = drgevent.Data.GetData(DataFormats.FileDrop) as string[];
				if (dropped != null)
				{
					AddItems(ExpandPaths(dropped, IncludeSubfolders), insertAt);
				}
			}

			Invalidate();
		}

		/// <summary>
		/// Turns a mix of picture files and folders into a list of pictures
		/// </summary>
		public static List<string> ExpandPaths(IEnumerable<string> paths, bool includeSubfolders)
		{
			List<string> pictures = new List<string>();
			foreach (string path in paths)
			{
				if (Directory.Exists(path))
				{
					pictures.AddRange(DirectoryScanner.GetFilenames(path, includeSubfolders, DirectoryScanner.IsImageFile));
				}
				else if (File.Exists(path) && DirectoryScanner.IsImageFile(new FileInfo(path)))
				{
					pictures.Add(path);
				}
			}

			return pictures;
		}

		void MoveSelected(int insertAt)
		{
			if (_selected.Count == 0)
			{
				return;
			}

			List<int> indexes = _selected.OrderBy(i => i).ToList();
			List<string> moving = indexes.Select(i => _items[i]).ToList();

			// the insert position moves up by the number of selected items before it
			int adjustedInsert = insertAt - indexes.Count(i => i < insertAt);
			List<string> remaining = _items.Where((item, index) => !_selected.Contains(index)).ToList();
			adjustedInsert = Math.Max(0, Math.Min(remaining.Count, adjustedInsert));
			remaining.InsertRange(adjustedInsert, moving);
			_items = remaining;

			_selected = new HashSet<int>(Enumerable.Range(adjustedInsert, moving.Count));
			_focusIndex = adjustedInsert;
			_anchorIndex = adjustedInsert;
			ItemsHaveChanged();
		}

		#endregion
	}
}
