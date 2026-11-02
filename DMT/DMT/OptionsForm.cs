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

namespace DMT
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Drawing;
	using System.Linq;
	using System.Windows.Forms;

	using DMT.Library.Ui;
	using DMT.Modules;

	/// <summary>
	/// The options (settings) window
	/// </summary>
	partial class OptionsForm : Form
	{
		/// <summary>
		/// Friendlier names for the module and page names
		/// </summary>
		static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>
		{
			{ "Dual Monitor Tools", "System" },
			{ "Dual Monitor Tools/Monitor Names", "Monitor names" },
			{ "Dual Monitor Tools/Monitor Order", "Monitor order" },
			{ "Cursor/General", "Shortcuts" },
			{ "Cursor/Sticky", "Sticky edges" },
			{ "Cursor/Primary Working Area", "Primary area" },
			{ "Cursor/Dead Space", "Dead space" },
			{ "Launcher/Magic Words", "Magic words" },
			{ "Launcher/HotKeys", "Shortcuts" },
			{ "Launcher/Import / Export", "Import & export" },
			{ "Snap/General", "Snapshots" },
			{ "Swap Screen", "Windows" },
			{ "Swap Screen/Active Window", "Active window" },
			{ "Swap Screen/System Defined Areas", "Numpad zones" },
			{ "Swap Screen/User Defined Areas", "Custom zones" },
			{ "Swap Screen/Other Windows", "All windows" },
			{ "Wallpaper Changer", "Wallpaper" },
			{ "Wallpaper Changer/Properties", "Appearance" },
			{ "Wallpaper Changer/Providers", "Sources" },
		};

		static readonly Dictionary<string, string> Glyphs = new Dictionary<string, string>
		{
			{ "Dual Monitor Tools/General", Theme.GlyphSettings },
			{ "Dual Monitor Tools/Monitor Names", Theme.GlyphRename },
			{ "Dual Monitor Tools/Monitor Order", Theme.GlyphSort },
			{ "Dual Monitor Tools/Monitors", Theme.GlyphMonitor },
			{ "Cursor/General", Theme.GlyphMouse },
			{ "Cursor/Sticky", Theme.GlyphPin },
			{ "Cursor/Primary Working Area", Theme.GlyphHome },
			{ "Cursor/Dead Space", Theme.GlyphFrame },
			{ "Launcher/Magic Words", Theme.GlyphCommand },
			{ "Launcher/HotKeys", Theme.GlyphKeyboard },
			{ "Launcher/General", Theme.GlyphSliders },
			{ "Launcher/Import / Export", Theme.GlyphSwap },
			{ "Snap/General", Theme.GlyphCamera },
			{ "Swap Screen/Active Window", Theme.GlyphTaskView },
			{ "Swap Screen/System Defined Areas", Theme.GlyphTiles },
			{ "Swap Screen/User Defined Areas", Theme.GlyphLayout },
			{ "Swap Screen/Other Windows", Theme.GlyphWindows },
			{ "Wallpaper Changer/General", Theme.GlyphPhoto },
			{ "Wallpaper Changer/Properties", Theme.GlyphPalette },
			{ "Wallpaper Changer/Providers", Theme.GlyphFolder },
		};

		/// <summary>
		/// The order the modules are listed in (the most used first)
		/// </summary>
		static readonly string[] GroupOrder = { "Swap Screen", "Cursor", "Launcher", "Snap", "Wallpaper Changer", "Dual Monitor Tools" };

		// remember the page the user was on, so we can return to it next time
		static string _lastPageKey;

		class PageInfo
		{
			public string Key;
			public string Group;
			public string Name;
			public NavItem Item;
			public ContainerControl Page;
			public Control Host;
		}

		IModuleService _moduleService;
		List<PageInfo> _pages = new List<PageInfo>();
		PageInfo _currentPage;

		NavPane _nav;
		Panel _navTop;
		TextBlock _appName;
		Glyph _appGlyph;
		Panel _header;
		TextBlock _groupLabel;
		TextBlock _titleLabel;
		Panel _contentHost;
		ToolTip _toolTip;
		bool _compact;

		/// <summary>
		/// Initialises a new instance of the <see cref="OptionsForm" /> class.
		/// </summary>
		/// <param name="moduleService">The service providing information on the modules</param>
		public OptionsForm(IModuleService moduleService)
		{
			_moduleService = moduleService;
			InitializeComponent();

			ComponentResourceManager resources = new ComponentResourceManager(typeof(OptionsForm));
			Icon = (Icon)resources.GetObject("$this.Icon");
			Text = "Dual Monitor Tools";
			AutoScaleMode = AutoScaleMode.Dpi;
			AutoScaleDimensions = new SizeF(96F, 96F);
			StartPosition = FormStartPosition.CenterScreen;
			KeyPreview = true;
			BackColor = Theme.Background;
			ForeColor = Theme.TextPrimary;
			DoubleBuffered = true;
			ClientSize = new Size(LogicalToDeviceUnits(1120), LogicalToDeviceUnits(760));
			MinimumSize = new Size(LogicalToDeviceUnits(520), LogicalToDeviceUnits(420));

			_toolTip = new ToolTip(components);

			_navTop = new Panel { BackColor = Theme.NavBackground };
			_appGlyph = new Glyph(Theme.GlyphMonitor, TextRole.Icon, TextTone.Accent);
			_appName = new TextBlock("Dual Monitor Tools", TextRole.Caption, TextTone.Secondary) { Wrap = false };
			_navTop.Controls.Add(_appGlyph);
			_navTop.Controls.Add(_appName);

			_nav = new NavPane();

			_header = new Panel { BackColor = Theme.Background };
			_groupLabel = new TextBlock(string.Empty, TextRole.Body, TextTone.Secondary) { Wrap = false };
			_titleLabel = new TextBlock(string.Empty, TextRole.Title) { Wrap = false };
			_header.Controls.Add(_groupLabel);
			_header.Controls.Add(_titleLabel);

			_contentHost = new Panel { BackColor = Theme.Background };

			Controls.Add(_contentHost);
			Controls.Add(_header);
			Controls.Add(_nav);
			Controls.Add(_navTop);

			Theme.Changed += Theme_Changed;
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			Theme.ApplyWindowChrome(this);
			BuildNavigation(_moduleService.GetOptionNodes());

			PageInfo first = _pages.FirstOrDefault(p => p.Key == _lastPageKey) ?? _pages.FirstOrDefault();
			if (first != null)
			{
				first.Item.Checked = true;
			}
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			Theme.Changed -= Theme_Changed;
			base.OnFormClosed(e);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		protected override void OnDpiChanged(DpiChangedEventArgs e)
		{
			base.OnDpiChanged(e);
			PerformLayout();
		}

		void Theme_Changed(object sender, EventArgs e)
		{
			if (InvokeRequired)
			{
				BeginInvoke(new EventHandler(Theme_Changed), sender, e);
				return;
			}

			BackColor = Theme.Background;
			ForeColor = Theme.TextPrimary;
			_navTop.BackColor = Theme.NavBackground;
			_header.BackColor = Theme.Background;
			_contentHost.BackColor = Theme.Background;
			Theme.ApplyWindowChrome(this);
			foreach (PageInfo page in _pages)
			{
				if (page.Host != null)
				{
					page.Host.BackColor = Theme.Background;
				}

				UiHelper.RefreshTheme(page.Page);
				if (!(page.Page is SettingsPage))
				{
					UiHelper.ApplyLegacyTheme(page.Page);
				}
			}

			UiHelper.RefreshTheme(this);
		}

		void BuildNavigation(IEnumerable<ModuleOptionNode> optionNodes)
		{
			List<ModuleOptionNode> nodes = optionNodes.ToList();
			nodes.Sort((a, b) => GroupIndex(a.Name).CompareTo(GroupIndex(b.Name)));

			_nav.SuspendLayout();
			foreach (ModuleOptionNode node in nodes)
			{
				ModuleOptionNodeBranch branch = node as ModuleOptionNodeBranch;
				if (branch == null)
				{
					// a module with just one page
					AddPage(node.Name, node.Name, node.OptionPanel);
					continue;
				}

				_nav.Controls.Add(new NavHeader(DisplayName(branch.Name)));
				foreach (ModuleOptionNode child in branch.Nodes)
				{
					AddPage(branch.Name, child.Name, child.OptionPanel);
				}
			}

			_nav.ResumeLayout();
		}

		void AddPage(string group, string name, ContainerControl page)
		{
			if (page == null)
			{
				return;
			}

			string key = group + "/" + name;
			string glyph;
			if (!Glyphs.TryGetValue(key, out glyph))
			{
				glyph = Theme.GlyphSettings;
			}

			PageInfo info = new PageInfo
			{
				Key = key,
				Group = DisplayName(group),
				Name = DisplayName(key, name),
				Page = page,
			};

			info.Item = new NavItem(info.Name, glyph, _toolTip);
			info.Item.Compact = _compact;
			info.Item.CheckedChanged += (s, e) =>
			{
				if (info.Item.Checked)
				{
					ShowPage(info);
				}
			};

			_pages.Add(info);
			_nav.Controls.Add(info.Item);
		}

		static int GroupIndex(string group)
		{
			int index = Array.IndexOf(GroupOrder, group);
			return index < 0 ? GroupOrder.Length : index;
		}

		static string DisplayName(string key, string fallback = null)
		{
			string name;
			if (DisplayNames.TryGetValue(key, out name))
			{
				return name;
			}

			return fallback ?? key;
		}

		void ShowPage(PageInfo info)
		{
			if (info == _currentPage)
			{
				return;
			}

			_lastPageKey = info.Key;
			_groupLabel.Text = info.Group;
			_titleLabel.Text = info.Name;

			if (info.Host == null)
			{
				info.Host = CreateHost(info.Page);
			}

			_contentHost.SuspendLayout();
			_contentHost.Controls.Add(info.Host);
			info.Host.Dock = DockStyle.Fill;
			if (_currentPage != null && _currentPage.Host != null)
			{
				_contentHost.Controls.Remove(_currentPage.Host);
			}

			_contentHost.ResumeLayout();
			_currentPage = info;
		}

		/// <summary>
		/// Pages built from settings cards are shown directly.
		/// Older pages (laid out in the designer) are placed in a scrolling panel and recoloured.
		/// </summary>
		Control CreateHost(ContainerControl page)
		{
			if (page is SettingsPage)
			{
				return page;
			}

			LegacyPageHost host = new LegacyPageHost(page);
			return host;
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			base.OnLayout(levent);
			if (_nav == null)
			{
				return;
			}

			int width = ClientSize.Width;
			int height = ClientSize.Height;

			bool compact = width < LogicalToDeviceUnits(880);
			if (compact != _compact)
			{
				_compact = compact;
				_nav.Compact = compact;
				_appName.Visible = !compact;
			}

			int navWidth = LogicalToDeviceUnits(compact ? 60 : 284);
			int navTopHeight = LogicalToDeviceUnits(52);

			_navTop.SetBounds(0, 0, navWidth, navTopHeight);
			Size glyphSize = _appGlyph.GetPreferredSize(Size.Empty);
			int glyphLeft = compact ? (navWidth - glyphSize.Width) / 2 : LogicalToDeviceUnits(22);
			_appGlyph.SetBounds(glyphLeft, (navTopHeight - glyphSize.Height) / 2, glyphSize.Width, glyphSize.Height);
			int nameLeft = glyphLeft + glyphSize.Width + LogicalToDeviceUnits(12);
			int nameHeight = _appName.GetHeightForWidth(navWidth);
			_appName.SetBounds(nameLeft, (navTopHeight - nameHeight) / 2, Math.Max(0, navWidth - nameLeft - LogicalToDeviceUnits(8)), nameHeight);

			_nav.SetBounds(0, navTopHeight, navWidth, height - navTopHeight);

			int headerHeight = LogicalToDeviceUnits(compact ? 76 : 96);
			_header.SetBounds(navWidth, 0, width - navWidth, headerHeight);
			int pad = LogicalToDeviceUnits(36);
			int groupHeight = _groupLabel.GetHeightForWidth(width);
			int titleHeight = _titleLabel.GetHeightForWidth(width);
			int titleTop = headerHeight - titleHeight - LogicalToDeviceUnits(14);
			_titleLabel.SetBounds(pad, titleTop, Math.Max(0, _header.Width - pad * 2), titleHeight);
			_groupLabel.SetBounds(pad, titleTop - groupHeight, Math.Max(0, _header.Width - pad * 2), groupHeight);
			_groupLabel.Visible = !compact;

			_contentHost.SetBounds(navWidth, headerHeight, width - navWidth, height - headerHeight);
		}

		/// <summary>
		/// Scrolling host for pages that were laid out in the designer
		/// </summary>
		class LegacyPageHost : Panel, IThemeAware
		{
			Control _page;
			Size _minimumSize = Size.Empty;

			public LegacyPageHost(Control page)
			{
				_page = page;
				AutoScroll = true;
				BackColor = Theme.Background;
				page.Dock = DockStyle.None;
				page.Visible = true;
				Controls.Add(page);
				UiHelper.ApplyLegacyTheme(page);
				page.BackColor = Theme.Background;
			}

			public void ApplyTheme()
			{
				BackColor = Theme.Background;
				_page.BackColor = Theme.Background;
			}

			protected override void OnLayout(LayoutEventArgs levent)
			{
				base.OnLayout(levent);

				if (_minimumSize.IsEmpty && _page.IsHandleCreated)
				{
					// the smallest size the page was designed for
					int right = 0;
					int bottom = 0;
					foreach (Control child in _page.Controls)
					{
						right = Math.Max(right, child.Right);
						bottom = Math.Max(bottom, child.Bottom);
					}

					_minimumSize = new Size(right + LogicalToDeviceUnits(8), bottom + LogicalToDeviceUnits(8));
				}

				int pad = LogicalToDeviceUnits(36);
				Size min = _minimumSize.IsEmpty ? new Size(LogicalToDeviceUnits(500), LogicalToDeviceUnits(432)) : _minimumSize;
				int width = Math.Max(min.Width, Math.Min(ClientSize.Width - pad * 2, LogicalToDeviceUnits(1000)));
				int height = Math.Max(min.Height, ClientSize.Height - LogicalToDeviceUnits(24));
				_page.SetBounds(pad + AutoScrollPosition.X, AutoScrollPosition.Y, width, height);
				AutoScrollMinSize = new Size(width + pad * 2, height + LogicalToDeviceUnits(24));
			}

			protected override void OnResize(EventArgs eventargs)
			{
				base.OnResize(eventargs);
				PerformLayout();
			}
		}
	}
}
